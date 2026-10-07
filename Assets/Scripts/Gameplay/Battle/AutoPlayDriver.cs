using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PixelDefense.Core;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Autoplay bot for testing: plans the battle with the solver on a worker thread, then taps columns through
    /// <see cref="BattleView.TapColumn"/> like a careful player who waits for the board to settle. Replans whenever
    /// something else changes the board (a player tap, a booster).
    /// </summary>
    public sealed class AutoPlayDriver
    {
        private const int NodeBudget = 250000;

        /// <summary>Seconds the board must stay settled before the bot taps (reads as a quick human glance).</summary>
        private const float ThinkTime = 0.3f;

        private const float MinTapInterval = 0.45f;

        private readonly AutoPlayer _player = new AutoPlayer();
        private CancellationTokenSource _planning;
        private int _planningActions = -1;
        private float _settled;
        private float _sinceTap;

        /// <summary>Column the bot is about to tap (shown with the hint hand), or -1.</summary>
        public int PendingColumn { get; private set; } = -1;

        public bool IsPlanning => _planning != null;

        public void Reset()
        {
            _planning?.Cancel();
            _planning = null;
            _planningActions = -1;
            _player.Reset();
            _settled = 0f;
            _sinceTap = MinTapInterval;
            PendingColumn = -1;
        }

        public void Tick(BattleView view, Battle battle, float deltaTime)
        {
            PendingColumn = -1;
            if (battle.Phase != BattlePhase.Playing)
            {
                return;
            }

            if (_player.NeedsPlan(battle))
            {
                StartPlanning(battle);
                return;
            }

            _sinceTap += deltaTime;
            int column = _player.NextTap(battle);
            if (column < 0)
            {
                _settled = 0f;
                return;
            }

            PendingColumn = column;
            _settled += deltaTime;
            if (_settled < ThinkTime || _sinceTap < MinTapInterval)
            {
                return;
            }

            if (view.TapColumn(column) == DeployResult.Deployed)
            {
                _player.OnTapped();
                _sinceTap = 0f;
                _settled = 0f;
                PendingColumn = -1;
            }
        }

        private void StartPlanning(Battle battle)
        {
            if (_planning != null && _planningActions == battle.ActionCount)
            {
                return;
            }

            _planning?.Cancel();
            var cts = new CancellationTokenSource();
            _planning = cts;
            _planningActions = battle.ActionCount;
            Plan(battle.CloneForPlanning(), battle.ActionCount, cts).Forget();
        }

        private async UniTaskVoid Plan(Battle snapshot, int actions, CancellationTokenSource cts)
        {
            float started = Time.realtimeSinceStartup;
            SolveResult result;
            try
            {
                result = await UniTask.RunOnThreadPool(() => LevelSolver.SolveFrom(snapshot, NodeBudget), cancellationToken: cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cts.IsCancellationRequested || _planning != cts)
            {
                return;
            }

            _planning = null;
            _player.SetPlan(result, actions);
            float seconds = Time.realtimeSinceStartup - started;
            if (result.Solved)
            {
                Debug.Log("[Bot] Plan: " + result.Moves.Length + " taps (" + result.Nodes + " nodes, " + seconds.ToString("F2") + "s)");
            }
            else
            {
                Debug.LogWarning("[Bot] No winning plan from here (" + result.Nodes + " nodes" +
                                 (result.BudgetExceeded ? ", budget exceeded" : string.Empty) + "); following hints.");
            }
        }
    }
}
