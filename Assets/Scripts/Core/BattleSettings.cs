using System;

namespace PixelDefense.Core
{
    /// <summary>
    /// Global battle tunables shared by all levels (per-level values live in <see cref="LevelDefinition"/>).
    /// Serializable so a ScriptableObject can own and edit an instance directly.
    /// </summary>
    [Serializable]
    public sealed class BattleSettings
    {
        /// <summary>Seconds between shots of one cannon.</summary>
        public float FireInterval = 0.12f;

        /// <summary>Seconds a projectile flies before its scale is destroyed.</summary>
        public float ShotFlightTime = 0.26f;

        /// <summary>Seconds a tapped cannon spends jumping from its column to a base slot.</summary>
        public float DeployDuration = 0.42f;

        /// <summary>Seconds an empty or dismissed cannon occupies its slot while it leaves.</summary>
        public float LeaveDelay = 0.32f;

        /// <summary>Seconds of continuous head contact with the base that lose the level.</summary>
        public float ContactLoseTime = 2f;

        /// <summary>Front slice to snout tip in slices; the head touches the base this far ahead of the front slice.</summary>
        public float HeadLength = 7.37f;

        /// <summary>
        /// How far (in slices) the head is knocked back when a slice is cleared; the rest of the gap closes from
        /// the tail. 1 = full knock-back (the body keeps a constant speed and only the head is pushed back),
        /// 0 = the tail always catches up.
        /// </summary>
        public float RecoilFraction = 1f;

        /// <summary>Slices before the base over which the dragon slows to a menacing crawl.</summary>
        public float DangerZone = 10f;

        public float DangerSpeedFactor = 0.55f;

        /// <summary>Remaining-scales fraction at which the dragon enrages and speeds up.</summary>
        public float EnrageFraction = 0.2f;

        public float EnrageSpeedFactor = 1.35f;

        /// <summary>Seconds a hopeless jam is shown before the dragon rushes the base.</summary>
        public float JamGraceTime = 1.6f;

        public float JamSpeedFactor = 5f;

        public float FreezeDuration = 5f;

        /// <summary>Front slices destroyed by the bomb booster.</summary>
        public int BombSlices = 4;

        public int MaxSlots = 6;

        /// <summary>Longest dragon integration step; events are processed at their exact times regardless.</summary>
        public float MaxStep = 1f / 60f;

        public BattleSettings Clone()
        {
            return (BattleSettings)MemberwiseClone();
        }
    }
}
