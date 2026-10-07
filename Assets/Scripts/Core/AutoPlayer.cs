using System;

namespace PixelDefense.Core
{
    /// <summary>
    /// Autoplay policy for testing levels. Follows a solver plan and only taps once the board has settled, which is
    /// the solver's own model, so a found plan always clears the dragon if it is fast enough. Any change it did not
    /// make (a player tap, a booster) invalidates the plan; without a plan it follows the in-game hint.
    /// </summary>
    public sealed class AutoPlayer
    {
        private int[] _columns = Array.Empty<int>();
        private int[] _cannons = Array.Empty<int>();
        private int _next;
        private int _expectedActions = -1;

        /// <summary>The last plan found no winning order from that board (or ran out of search budget).</summary>
        public bool PlanFailed { get; private set; }

        /// <summary>Planned taps not made yet.</summary>
        public int PlannedRemaining => _columns.Length - _next;

        public void Reset()
        {
            _columns = Array.Empty<int>();
            _cannons = Array.Empty<int>();
            _next = 0;
            _expectedActions = -1;
            PlanFailed = false;
        }

        /// <summary>True when the board was changed by something other than this player since the plan was made.</summary>
        public bool NeedsPlan(Battle battle)
        {
            return battle.Phase == BattlePhase.Playing && battle.ActionCount != _expectedActions;
        }

        /// <param name="result">Solve result from <see cref="LevelSolver.SolveFrom"/>.</param>
        /// <param name="actionCount"><see cref="Battle.ActionCount"/> of the battle the planning copy was taken from.</param>
        public void SetPlan(SolveResult result, int actionCount)
        {
            _columns = result.Solved ? result.Moves : Array.Empty<int>();
            _cannons = result.Solved ? result.Cannons : Array.Empty<int>();
            _next = 0;
            _expectedActions = actionCount;
            PlanFailed = !result.Solved;
        }

        /// <summary>Column to tap now; -1 while waiting for a plan, a free slot or the board to settle.</summary>
        public int NextTap(Battle battle)
        {
            if (battle.Phase != BattlePhase.Playing || NeedsPlan(battle) || !battle.HasFreeSlot || !battle.IsQuiescent)
            {
                return -1;
            }

            if (_next < _columns.Length)
            {
                int column = _columns[_next];
                if (battle.FrontCannon(column) == _cannons[_next])
                {
                    return column;
                }

                // The board no longer matches the plan; ask for a new one.
                _expectedActions = -1;
                return -1;
            }

            int hint = BattleHints.BestColumn(battle);
            return hint >= 0 ? hint : BattleHints.FirstNonEmptyColumn(battle);
        }

        /// <summary>Call after the column returned by <see cref="NextTap"/> was deployed.</summary>
        public void OnTapped()
        {
            _next++;
            _expectedActions++;
        }
    }
}
