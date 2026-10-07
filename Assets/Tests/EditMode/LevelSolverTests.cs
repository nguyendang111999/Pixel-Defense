using NUnit.Framework;
using PixelDefense.Core;

namespace PixelDefense.Tests.EditMode
{
    public class LevelSolverTests
    {
        private const int Budget = 20000;

        [Test]
        public void Solve_StraightforwardLevel_FindsSolutionAndGreedyWins()
        {
            LevelDefinition level = TestLevels.Make("width 1\nslots 1\nbody R*2 G*2\ncol R2 G2");

            SolveResult result = LevelSolver.Solve(level, TestLevels.Settings(), Budget);

            Assert.That(result.Solved, Is.True);
            Assert.That(result.Moves, Is.EqualTo(new[] { 0, 0 }));
            Assert.That(result.GreedyWins, Is.True);
        }

        [Test]
        public void Solve_FrontCannonCanNeverFire_IsUnsolvable()
        {
            LevelDefinition level = TestLevels.Make("width 1\nwindow 1\nslots 1\nbody R*1 G*1\ncol G1 R1");

            SolveResult result = LevelSolver.Solve(level, TestLevels.Settings(), Budget);

            Assert.That(result.Solved, Is.False);
            Assert.That(result.BudgetExceeded, Is.False);
        }

        [Test]
        public void Solve_RequiresParkingInASlot_FindsOnlyWinningOrder()
        {
            LevelDefinition level = TestLevels.Make("width 1\nwindow 1\nslots 2\nbody R*1 G*1 B*1\ncol G1\ncol B1 R1");

            SolveResult result = LevelSolver.Solve(level, TestLevels.Settings(), Budget);

            Assert.That(result.Solved, Is.True);
            Assert.That(result.Moves, Is.EqualTo(new[] { 1, 1, 0 }));
        }

        [Test]
        public void SolveFrom_MidBattle_PlansTheRemainingTaps()
        {
            LevelDefinition level = TestLevels.Make("width 1\nwindow 1\nslots 2\nbody R*1 G*1 B*1\ncol G1\ncol B1 R1");
            var battle = new Battle(level, TestLevels.Settings(), 1000f) { MovementEnabled = false };
            battle.Deploy(1);

            SolveResult result = LevelSolver.SolveFrom(battle, Budget);

            Assert.That(result.Solved, Is.True);
            Assert.That(result.Moves, Is.EqualTo(new[] { 1, 0 }));
            Assert.That(result.Cannons, Is.EqualTo(new[] { battle.CannonIdAt(1, 1), battle.CannonIdAt(0, 0) }));
            Assert.That(battle.DeployedCount, Is.EqualTo(1), "Planning must not touch the live battle.");
            Assert.That(battle.Time, Is.EqualTo(0f));
        }

        [Test]
        public void SolveFrom_HopelessBoard_ReportsUnsolved()
        {
            LevelDefinition level = TestLevels.Make("width 1\nwindow 1\nslots 1\nbody R*1 G*1\ncol G1\ncol R1");
            var battle = new Battle(level, TestLevels.Settings(), 1000f) { MovementEnabled = false };
            battle.Deploy(0);

            SolveResult result = LevelSolver.SolveFrom(battle, Budget);

            Assert.That(result.Solved, Is.False);
            Assert.That(result.BudgetExceeded, Is.False);
        }

        [Test]
        public void Bot_PlayingSolution_WinsAgainstSlowDragon()
        {
            LevelDefinition level = TestLevels.Make("width 3\nspeed 0.8\nbody R*6 G*6\ncol R18 G18");
            SolveResult result = LevelSolver.Solve(level, TestLevels.Settings(), Budget);
            TrackShape track = TrackPresets.Create(level.Track, level.Width);

            BotResult bot = BotPlayer.Play(level, TestLevels.Settings(), track, result.Moves, 0.6f, waitForSettle: true);

            Assert.That(bot.Won, Is.True);
            Assert.That(bot.MinDistance, Is.GreaterThan(0f));
        }
    }
}
