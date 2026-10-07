using System.Globalization;
using System.Text;
using PixelDefense.App;
using PixelDefense.Core;
using UnityEditor;
using UnityEngine;

namespace PixelDefense.EditorTools
{
    /// <summary>
    /// Bot tools (menu <b>Pixel Defense ▸ Bot</b>): a headless playtest of every level in the pack, and the autoplay
    /// toggle for Play mode (also B in the Game view, or the pause menu).
    /// </summary>
    public static class BotReport
    {
        private const int SolverBudget = 250000;
        private const float ExpertReaction = 0.5f;
        private const float CarefulReaction = 1.2f;

        [MenuItem("Pixel Defense/Bot/Simulate All Levels", priority = 70)]
        public static void SimulateAllMenu()
        {
            Debug.Log(Simulate());
        }

        [MenuItem("Pixel Defense/Bot/Toggle Autoplay (Play Mode)", priority = 71)]
        public static void ToggleAutoplay()
        {
            var flow = Object.FindAnyObjectByType<GameFlow>();
            if (flow != null)
            {
                flow.DebugToggleBot();
            }
        }

        [MenuItem("Pixel Defense/Bot/Toggle Autoplay (Play Mode)", true)]
        private static bool CanToggleAutoplay()
        {
            return EditorApplication.isPlaying;
        }

        /// <summary>
        /// For each level: finds a winning tap order, then replays it against the moving dragon with an expert
        /// (0.5 s per tap) and a careful (1.2 s) bot that both wait for the board to settle. Margin = closest
        /// approach as a share of the starting distance.
        /// </summary>
        public static string Simulate()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetPaths.GameConfig);
            LevelPack pack = LevelParser.Parse(config.LevelPack.text, config.LevelPack.name);
            BattleSettings settings = config.Battle.Clone();
            var report = new StringBuilder();
            report.AppendLine("Bot playtest of " + pack.Levels.Count + " levels (margin = closest approach / starting distance):");
            int failures = 0;
            foreach (LevelDefinition level in pack.Levels)
            {
                TrackShape track = TrackPresets.Create(level.Track, level.Width);
                float startDistance = track.ContactFront(settings.HeadLength) - level.Start;
                SolveResult solve = LevelSolver.Solve(level, settings, SolverBudget);
                report.Append(level.Id).Append(' ').Append(level.Name).Append(" [").Append(level.Track).Append("] ");
                if (!solve.Solved)
                {
                    failures++;
                    report.AppendLine("NO SOLUTION (" + solve.Nodes + " nodes" + (solve.BudgetExceeded ? ", budget exceeded" : string.Empty) + ")");
                    continue;
                }

                BotResult expert = BotPlayer.Play(level, settings, track, solve.Moves, ExpertReaction, waitForSettle: true);
                BotResult careful = BotPlayer.Play(level, settings, track, solve.Moves, CarefulReaction, waitForSettle: true);
                if (!careful.Won)
                {
                    failures++;
                }

                report.Append("taps=").Append(solve.Moves.Length)
                    .Append(" | expert ").Append(Describe(expert, startDistance))
                    .Append(" | careful ").Append(Describe(careful, startDistance))
                    .Append(" | greedy ").AppendLine(solve.GreedyWins ? "wins" : "loses");
            }

            report.Insert(0, failures == 0 ? "[Bot] All levels beatable. " : "[Bot] " + failures + " level(s) NOT beatable by the careful bot. ");
            return report.ToString();
        }

        private static string Describe(BotResult result, float startDistance)
        {
            string time = result.Duration.ToString("F0", CultureInfo.InvariantCulture) + "s";
            if (!result.Won)
            {
                return "LOST at " + time;
            }

            float margin = startDistance > 0f ? Mathf.Max(0f, result.MinDistance) / startDistance : 0f;
            return "won " + time + " margin " + margin.ToString("P0", CultureInfo.InvariantCulture);
        }
    }
}
