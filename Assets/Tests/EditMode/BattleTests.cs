using System.Collections.Generic;
using NUnit.Framework;
using PixelDefense.Core;

namespace PixelDefense.Tests.EditMode
{
    public class BattleTests
    {
        private const float FarAway = 1000f;

        [Test]
        public void Deploy_FrontCannon_LandsInLeftmostSlotAndActivates()
        {
            Battle battle = Create("width 1\nslots 2\nbody R*3\ncol R3", FarAway, moving: false);
            int landed = -1;
            battle.CannonLanded += slot => landed = slot;

            Assert.That(battle.Deploy(0), Is.EqualTo(DeployResult.Deployed));
            Assert.That(battle.GetSlot(0).Phase, Is.EqualTo(SlotPhase.Landing));

            battle.Tick(0.5f);

            Assert.That(landed, Is.EqualTo(0));
            Assert.That(battle.GetSlot(0).Phase, Is.EqualTo(SlotPhase.Active));
            Assert.That(battle.ColumnRemaining(0), Is.EqualTo(0));
        }

        [Test]
        public void Deploy_WhenSlotsFull_ReturnsSlotsFull()
        {
            Battle battle = Create("width 1\nslots 1\nbody R*2\ncol R1 R1", FarAway, moving: false);

            Assert.That(battle.Deploy(0), Is.EqualTo(DeployResult.Deployed));
            Assert.That(battle.Deploy(0), Is.EqualTo(DeployResult.SlotsFull));
        }

        [Test]
        public void Cannon_OnlyHitsExposedScalesOfItsColor()
        {
            Battle battle = Create("width 1\nwindow 1\nbody G*1 R*2\ncol R2\ncol G1", FarAway, moving: false);

            battle.Deploy(0);
            battle.RunUntilQuiescent(10f);
            Assert.That(battle.GetSlot(0).Ammo, Is.EqualTo(2), "Red must wait while the green neck covers it.");

            battle.Deploy(1);
            battle.RunUntilQuiescent(10f);
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Won));
        }

        [Test]
        public void Cannon_OutOfAmmo_LeavesAndFreesSlot()
        {
            Battle battle = Create("width 1\nslots 1\nbody R*2\ncol R1 R1", FarAway, moving: false);
            int emptied = -1;
            int freed = -1;
            battle.CannonEmptied += slot => emptied = slot;
            battle.SlotFreed += slot => freed = slot;

            battle.Deploy(0);
            battle.RunUntilQuiescent(10f);

            Assert.That(emptied, Is.EqualTo(0));
            Assert.That(freed, Is.EqualTo(0));
            Assert.That(battle.GetSlot(0).Phase, Is.EqualTo(SlotPhase.Empty));
            Assert.That(battle.Deploy(0), Is.EqualTo(DeployResult.Deployed));
        }

        [Test]
        public void SliceCleared_FullRecoil_PullsHeadBackOneSlice()
        {
            BattleSettings settings = TestLevels.Settings();
            settings.RecoilFraction = 1f;
            var battle = new Battle(TestLevels.Make("width 1\nstart 10\nbody R*3\ncol R3"), settings, FarAway) { MovementEnabled = false };
            var fronts = new List<float>();
            battle.SliceCleared += _ => fronts.Add(battle.Dragon.FrontPosition);

            Assert.That(battle.Dragon.FrontPosition, Is.EqualTo(10f));
            battle.Deploy(0);
            battle.RunUntilQuiescent(10f);

            Assert.That(fronts.Count, Is.EqualTo(3));
            Assert.That(fronts[0], Is.EqualTo(9f).Within(1e-4f));
            Assert.That(fronts[1], Is.EqualTo(8f).Within(1e-4f));
        }

        [Test]
        public void SliceCleared_PartialRecoil_SplitsGapBetweenHeadAndTail()
        {
            BattleSettings settings = TestLevels.Settings();
            settings.RecoilFraction = 0.25f;
            var battle = new Battle(TestLevels.Make("width 1\nstart 10\nbody R*3\ncol R3"), settings, FarAway) { MovementEnabled = false };
            float tailBefore = battle.Dragon.TailPosition;
            float frontAfterFirst = float.NaN;
            battle.SliceCleared += _ =>
            {
                if (float.IsNaN(frontAfterFirst))
                {
                    frontAfterFirst = battle.Dragon.FrontPosition;
                }
            };

            battle.Deploy(0);
            battle.RunUntilQuiescent(10f);

            Assert.That(frontAfterFirst, Is.EqualTo(9.75f).Within(1e-4f));
            Assert.That(battle.Dragon.TailPosition, Is.EqualTo(tailBefore + 3f * 0.75f).Within(1e-4f));
        }

        [Test]
        public void Contact_TwoContinuousSeconds_LosesLevel()
        {
            Battle battle = Create("width 1\nstart 9.9\nspeed 5\nbody R*2\ncol R2", 10f, moving: true);
            bool lost = false;
            battle.Lost += () => lost = true;

            for (int i = 0; i < 600 && battle.Phase == BattlePhase.Playing; i++)
            {
                battle.Tick(1f / 60f);
            }

            Assert.That(lost, Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Lost));
            Assert.That(battle.Time, Is.InRange(2.0f, 2.1f));
        }

        [Test]
        public void Contact_SliceClearedDuringContact_ResetsTimerAndSurvives()
        {
            Battle battle = Create("width 1\nstart 9.95\nspeed 1\nbody R*3\ncol R3", 10f, moving: true);
            var transitions = new List<bool>();
            battle.ContactChanged += touching => transitions.Add(touching);

            battle.Deploy(0);
            for (int i = 0; i < 600 && battle.Phase == BattlePhase.Playing; i++)
            {
                battle.Tick(1f / 60f);
            }

            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Won));
            Assert.That(transitions.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(transitions[0], Is.True);
            Assert.That(transitions[1], Is.False);
        }

        [Test]
        public void AllSlotsBlocked_FlagsJamThenRushes()
        {
            Battle battle = Create("width 1\nwindow 1\nslots 1\nbody R*1 G*1\ncol G1\ncol R1", FarAway, moving: true);
            bool jamRaised = false;
            battle.JamChanged += jammed => jamRaised |= jammed;

            battle.Deploy(0);
            battle.Tick(1f);
            Assert.That(battle.IsJammed, Is.True);
            Assert.That(jamRaised, Is.True);
            Assert.That(battle.IsRushing, Is.False);

            battle.Tick(1.5f);
            Assert.That(battle.IsRushing, Is.True);
        }

        [Test]
        public void Outcome_IsIndependentOfFrameRate()
        {
            const string text = "width 3\nwindow 2\nbody R*4 GRG*2 G*2 B*3 R*1\ncol R10 G10\ncol B9 R7";
            int[] columns = { 0, 1, 1, 0 };
            float[] times = { 0f, 1f / 6f, 2f / 6f, 3f / 6f };

            List<int> slow = Record(text, columns, times, 1f / 30f, out float slowEnd);
            List<int> fast = Record(text, columns, times, 1f / 144f, out float fastEnd);

            Assert.That(slow.Count, Is.EqualTo(36), "Every cube should be destroyed.");
            Assert.That(fast, Is.EqualTo(slow));
            Assert.That(fastEnd, Is.EqualTo(slowEnd).Within(1e-3f));
        }

        [Test]
        public void Freeze_StopsDragonForDuration()
        {
            Battle battle = Create("width 1\nstart 5\nspeed 2\nbody R*2\ncol R2", FarAway, moving: true);

            Assert.That(battle.UseFreeze(), Is.True);
            battle.Tick(1f);
            Assert.That(battle.Dragon.FrontPosition, Is.EqualTo(5f).Within(1e-4f));

            battle.Tick(5f);
            Assert.That(battle.IsFrozen, Is.False);
            Assert.That(battle.Dragon.FrontPosition, Is.GreaterThan(5f));
        }

        [Test]
        public void Bomb_DestroysFrontSlicesRegardlessOfColor()
        {
            Battle battle = Create("width 1\nbody R*5 G*1\ncol R5 G1", FarAway, moving: false);

            int destroyed = battle.UseBomb();

            Assert.That(destroyed, Is.EqualTo(4));
            Assert.That(battle.Dragon.AliveCubes, Is.EqualTo(2));
            Assert.That(battle.Dragon.FrontSlice, Is.EqualTo(4));
        }

        [Test]
        public void Bomb_ClearingEverything_WinsLevel()
        {
            Battle battle = Create("width 1\nbody R*2 G*1\ncol R2 G1", FarAway, moving: false);

            battle.UseBomb();

            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Won));
        }

        [Test]
        public void Enrage_TriggersAtTwentyPercentRemaining()
        {
            Battle battle = Create("width 1\nbody R*10\ncol R10", FarAway, moving: false);
            int aliveAtEnrage = -1;
            battle.Enraged += () => aliveAtEnrage = battle.Dragon.AliveCubes;

            battle.Deploy(0);
            battle.RunUntilQuiescent(10f);

            Assert.That(aliveAtEnrage, Is.EqualTo(2));
        }

        [Test]
        public void ColorCleared_DismissesCannonWithLeftoverAmmo()
        {
            Battle battle = Create("width 1\nbody R*4 G*1\ncol R4 G1", FarAway, moving: false);
            int dismissed = -1;
            battle.CannonDismissed += slot => dismissed = slot;

            battle.UseBomb();
            battle.Deploy(0);
            battle.Tick(1f);

            Assert.That(dismissed, Is.EqualTo(0), "Red cannon has nothing left to shoot and must leave.");
        }

        [Test]
        public void DeployCannon_FromMiddleOfColumn_KeepsFrontAndSkipsTheGap()
        {
            Battle battle = Create("width 1\nslots 3\nbody R*1 G*1 B*1\ncol R1 G1 B1", FarAway, moving: false);
            int green = battle.CannonIdAt(0, 1);

            Assert.That(battle.DeployCannon(green), Is.EqualTo(DeployResult.Deployed));

            Assert.That(battle.GetSlot(0).CannonId, Is.EqualTo(green));
            Assert.That(battle.FrontCannon(0), Is.EqualTo(battle.CannonIdAt(0, 0)));
            Assert.That(battle.ColumnRemaining(0), Is.EqualTo(2));
            Assert.That(battle.Deploy(0), Is.EqualTo(DeployResult.Deployed));
            Assert.That(battle.FrontCannon(0), Is.EqualTo(battle.CannonIdAt(0, 2)), "The picked cannon's place is skipped.");
            Assert.That(battle.DeployedCount, Is.EqualTo(2));
        }

        [Test]
        public void DeployCannon_AlreadyDeployedOrInvalid_ReturnsUnavailable()
        {
            Battle battle = Create("width 1\nslots 2\nbody R*2\ncol R1 R1", FarAway, moving: false);
            int front = battle.FrontCannon(0);
            battle.Deploy(0);

            Assert.That(battle.DeployCannon(front), Is.EqualTo(DeployResult.Unavailable));
            Assert.That(battle.DeployCannon(-1), Is.EqualTo(DeployResult.Unavailable));
            Assert.That(battle.DeployCannon(battle.CannonCount), Is.EqualTo(DeployResult.Unavailable));
        }

        [Test]
        public void DeployCannon_MysteryCannonPicked_IsRevealed()
        {
            Battle battle = Create("width 1\nslots 2\nbody R*1 G*1 B*1\ncol R1 ?G1 B1", FarAway, moving: false);
            int mystery = battle.CannonIdAt(0, 1);
            var revealed = new List<int>();
            battle.CannonRevealed += (column, id) => revealed.Add(id);
            Assert.That(battle.IsRevealed(mystery), Is.False);

            battle.DeployCannon(mystery);

            Assert.That(revealed, Is.EqualTo(new[] { mystery }));
            Assert.That(battle.IsRevealed(mystery), Is.True);
        }

        [Test]
        public void Deploy_FrontAfterSecondWasPicked_RevealsMysteryThird()
        {
            Battle battle = Create("width 1\nslots 3\nbody R*1 G*1 B*1\ncol R1 G1 ?B1", FarAway, moving: false);
            int mystery = battle.CannonIdAt(0, 2);
            var revealed = new List<int>();
            battle.CannonRevealed += (column, id) => revealed.Add(id);

            battle.DeployCannon(battle.CannonIdAt(0, 1));
            Assert.That(revealed, Is.Empty);

            battle.Deploy(0);
            Assert.That(revealed, Is.EqualTo(new[] { mystery }));
            Assert.That(battle.FrontCannon(0), Is.EqualTo(mystery));
        }

        [Test]
        public void ActionCount_CountsSuccessfulDeploysAndBoosters()
        {
            Battle battle = Create("width 1\nslots 1\nbody R*3\ncol R1 R1 R1", FarAway, moving: false);

            battle.Deploy(0);
            battle.Deploy(0);
            Assert.That(battle.ActionCount, Is.EqualTo(1), "A deploy into full slots changes nothing.");

            battle.UseFreeze();
            battle.AddSlot();
            Assert.That(battle.ActionCount, Is.EqualTo(3));
        }

        [Test]
        public void StateHash_DependsOnWhichCannonWasPicked()
        {
            Battle second = Create("width 1\nslots 2\nbody R*3\ncol R1 R1 R1", FarAway, moving: false);
            Battle third = Create("width 1\nslots 2\nbody R*3\ncol R1 R1 R1", FarAway, moving: false);

            second.DeployCannon(second.CannonIdAt(0, 1));
            third.DeployCannon(third.CannonIdAt(0, 2));

            Assert.That(second.StateHash, Is.Not.EqualTo(third.StateHash));
        }

        [Test]
        public void CloneForPlanning_IgnoresContactAndLeavesOriginalUntouched()
        {
            Battle battle = Create("width 1\nstart 9.9\nspeed 5\nbody R*2\ncol R2", 10f, moving: true);
            battle.Tick(0.5f);
            Assert.That(battle.InContact, Is.True);

            Battle plan = battle.CloneForPlanning();
            plan.Tick(10f);

            Assert.That(plan.Phase, Is.EqualTo(BattlePhase.Playing), "Planning copies can never lose on time.");
            Assert.That(plan.InContact, Is.False);
            Assert.That(battle.Time, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(battle.InContact, Is.True);
        }

        private static Battle Create(string text, float contactFront, bool moving)
        {
            LevelDefinition level = TestLevels.Make(text);
            return new Battle(level, TestLevels.Settings(), contactFront) { MovementEnabled = moving };
        }

        private static List<int> Record(string text, int[] columns, float[] times, float step, out float endTime)
        {
            Battle battle = Create(text, FarAway, moving: true);
            var events = new List<int>();
            battle.CubeDestroyed += (slice, lane, color, shot) => events.Add(slice * 100 + lane * 10000 + shot * 1000000);

            int next = 0;
            for (int i = 0; i < 100000 && battle.Phase == BattlePhase.Playing; i++)
            {
                while (next < columns.Length && battle.Time >= times[next] - 1e-4f)
                {
                    Assert.That(battle.Deploy(columns[next]), Is.EqualTo(DeployResult.Deployed));
                    next++;
                }
                battle.Tick(step);
            }

            endTime = battle.Time;
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Won));
            return events;
        }
    }
}
