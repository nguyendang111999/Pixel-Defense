namespace PixelDefense.Core
{
    public struct BotResult
    {
        public bool Won;
        public float Duration;

        /// <summary>Closest the dragon's front got to the contact point, in slices (negative = touched).</summary>
        public float MinDistance;
    }

    /// <summary>Replays a move list against the moving dragon to measure how much time pressure a level has.</summary>
    public static class BotPlayer
    {
        /// <param name="waitForSettle">Careful player: only taps once the board has settled.</param>
        public static BotResult Play(LevelDefinition level, BattleSettings settings, TrackShape track, int[] moves,
            float reactionTime, bool waitForSettle, float maxSeconds = 600f, float step = 1f / 60f)
        {
            var battle = new Battle(level, settings, track.ContactFront(settings.HeadLength));
            int next = 0;
            float lastTap = -reactionTime;
            float minDistance = battle.DistanceToBase;

            while (battle.Phase == BattlePhase.Playing && battle.Time < maxSeconds)
            {
                bool ready = battle.Time - lastTap >= reactionTime && battle.HasFreeSlot;
                if (next < moves.Length && ready && (!waitForSettle || battle.IsQuiescent))
                {
                    battle.Deploy(moves[next++]);
                    lastTap = battle.Time;
                }

                battle.Tick(step);
                float distance = battle.InContact ? -battle.ContactTimer : battle.DistanceToBase;
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return new BotResult
            {
                Won = battle.Phase == BattlePhase.Won,
                Duration = battle.Time,
                MinDistance = minDistance
            };
        }
    }
}
