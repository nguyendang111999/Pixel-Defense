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

        [Test]
        public void MainPack_CannonsCarryTenTwentyOrFortyAmmo()
        {
            foreach (LevelDefinition level in Pack.Levels)
            {
                for (int c = 0; c < level.ColumnCount; c++)
                {
                    for (int d = 0; d < level.ColumnLength(c); d++)
                    {
                        int ammo = level.Cannon(c, d).Ammo;
                        Assert.That(ammo == 10 || ammo == 20 || ammo == 40, Is.True, level.Id + " has a cannon with " + ammo + " ammo");
                    }
                }
            }
        }

        [Test]
        public void MainPack_UsesManyTrackShapes()
        {
            var shapes = new HashSet<string>();
            foreach (LevelDefinition level in Pack.Levels)
            {
                shapes.Add(level.Track.Replace("_cw", string.Empty));
            }
            Assert.That(shapes.Count, Is.GreaterThanOrEqualTo(5), string.Join(", ", shapes));
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
