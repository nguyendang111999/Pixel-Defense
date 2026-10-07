using NUnit.Framework;
using PixelDefense.Core;

namespace PixelDefense.Tests.EditMode
{
    public class AutoPlayerTests
    {
        private const int Budget = 20000;
        private const float Step = 1f / 60f;

        [Test]
        public void AutoPlayer_PlanningFromLiveBattle_BeatsMovingDragon()
        {
            LevelDefinition level = TestLevels.Make("width 3\nwindow 2\nspeed 0.8\nbody R*4 G*4 B*4\ncol R6 G6 B6\ncol R6 G6 B6");
            BattleSettings settings = TestLevels.Settings();
            TrackShape track = TrackPresets.Create(level.Track, level.Width);
            var battle = new Battle(level, settings, track.ContactFront(settings.HeadLength));
            var player = new AutoPlayer();

            for (int frame = 0; frame < 60 * 600 && battle.Phase == BattlePhase.Playing; frame++)
            {
                if (player.NeedsPlan(battle))
                {
                    player.SetPlan(LevelSolver.SolveFrom(battle, Budget), battle.ActionCount);
                }

                int column = player.NextTap(battle);
                if (column >= 0 && battle.Deploy(column) == DeployResult.Deployed)
                {
                    player.OnTapped();
                }
                battle.Tick(Step);
            }

            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Won));
            Assert.That(player.PlanFailed, Is.False);
        }

        [Test]
        public void NextTap_WaitsUntilBoardSettles()
        {
            LevelDefinition level = TestLevels.Make("width 1\nslots 2\nbody R*2 G*2\ncol R2 G2");
            var battle = new Battle(level, TestLevels.Settings(), 1000f) { MovementEnabled = false };
            var player = new AutoPlayer();
            player.SetPlan(LevelSolver.SolveFrom(battle, Budget), battle.ActionCount);

            Assert.That(player.NextTap(battle), Is.EqualTo(0));
            battle.Deploy(0);
            player.OnTapped();

            Assert.That(player.NextTap(battle), Is.EqualTo(-1), "Red is still landing and firing.");
            battle.RunUntilQuiescent(10f);
            Assert.That(player.NextTap(battle), Is.EqualTo(0));
        }

        [Test]
        public void NeedsPlan_AfterSomeoneElseActs_IsTrue()
        {
            LevelDefinition level = TestLevels.Make("width 1\nslots 3\nbody R*2 G*2\ncol R2\ncol G2");
            var battle = new Battle(level, TestLevels.Settings(), 1000f) { MovementEnabled = false };
            var player = new AutoPlayer();
            Assert.That(player.NeedsPlan(battle), Is.True);

            player.SetPlan(LevelSolver.SolveFrom(battle, Budget), battle.ActionCount);
            Assert.That(player.NeedsPlan(battle), Is.False);

            battle.Deploy(1);
            Assert.That(player.NeedsPlan(battle), Is.True);
            Assert.That(player.NextTap(battle), Is.EqualTo(-1));
        }

        [Test]
        public void NextTap_WithoutWinningPlan_FollowsHint()
        {
            LevelDefinition level = TestLevels.Make("width 1\nwindow 1\nslots 2\nbody R*1 G*1\ncol G1\ncol R1");
            var battle = new Battle(level, TestLevels.Settings(), 1000f) { MovementEnabled = false };
            var player = new AutoPlayer();
            player.SetPlan(new SolveResult(), battle.ActionCount);

            Assert.That(player.PlanFailed, Is.True);
            Assert.That(player.NextTap(battle), Is.EqualTo(1), "Red is the exposed neck color.");
        }
    }
}
