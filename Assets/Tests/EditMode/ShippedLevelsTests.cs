using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PixelDefense.Core;
using UnityEngine;

namespace PixelDefense.Tests.EditMode
{
    /// <summary>Every level in the shipped pack parses, is provably solvable, and a careful bot beats the dragon.</summary>
    public class ShippedLevelsTests
    {
        private const int SolverBudget = 200000;
        private const float BotReaction = 0.9f;

        private static LevelPack _pack;

        private static LevelPack Pack
        {
            get
            {
                if (_pack == null)
                {
                    string path = Path.Combine(Application.dataPath, "Levels", "main.txt");
                    _pack = LevelParser.Parse(File.ReadAllText(path), "main");
                }
                return _pack;
            }
        }

        private static IEnumerable<string> LevelIds()
        {
            foreach (LevelDefinition level in Pack.Levels)
            {
                yield return level.Id;
            }
        }

        [Test]
        public void MainPack_ParsesWithoutErrors()
        {
            Assert.That(Pack.HasErrors, Is.False, string.Join("\n", Pack.Issues));
            Assert.That(Pack.Levels.Count, Is.GreaterThanOrEqualTo(20));
        }

        [Test]
        public void MainPack_TracksAndSkinsExist()
        {
            foreach (LevelDefinition level in Pack.Levels)
            {
                Assert.That(TrackPresets.Exists(level.Track), Is.True, level.Id + " track " + level.Track);
            }
        }

        [TestCaseSource(nameof(LevelIds))]
        public void Level_IsSolvableAndBeatableByCarefulPlayer(string id)
        {
            LevelDefinition level = Pack.Levels[Pack.IndexOf(id)];
            var settings = new BattleSettings();

            SolveResult solution = LevelSolver.Solve(level, settings, SolverBudget);
            Assert.That(solution.Solved, Is.True, id + " has no solution (budget exceeded: " + solution.BudgetExceeded + ")");

            TrackShape track = TrackPresets.Create(level.Track, level.Width);
            BotResult bot = BotPlayer.Play(level, settings, track, solution.Moves, BotReaction, waitForSettle: true);
            Assert.That(bot.Won, Is.True, id + " bot lost; closest approach " + bot.MinDistance);
            Assert.That(bot.MinDistance, Is.GreaterThan(0f), id + " bot survived only by touching the base");
        }
    }
}
