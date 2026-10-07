namespace PixelDefense.Core
{
    public enum BattlePhase
    {
        Playing,
        Won,
        Lost
    }

    public enum DeployResult
    {
        Deployed,
        NotPlaying,
        InvalidColumn,
        EmptyColumn,
        SlotsFull,

        /// <summary>The cannon id is invalid or that cannon already left its column.</summary>
        Unavailable
    }

    public enum SlotPhase
    {
        Empty,
        Landing,
        Active,
        Leaving
    }

    /// <summary>A base slot and the cannon (if any) sitting on it.</summary>
    public sealed class SlotState
    {
        public SlotPhase Phase { get; internal set; }
        public int CannonId { get; internal set; } = -1;
        public byte Color { get; internal set; }
        public int Ammo { get; internal set; }
        public int MaxAmmo { get; internal set; }

        /// <summary>True when the cannon left with ammo because its color no longer exists on the dragon.</summary>
        public bool Dismissed { get; internal set; }

        public int LastTargetSlice { get; internal set; } = -1;
        public int LastTargetLane { get; internal set; } = -1;

        internal float ReadyAt;
        internal float NextFireAt;
        internal float FreeAt;

        public bool IsOccupied => Phase != SlotPhase.Empty;

        internal void Clear()
        {
            Phase = SlotPhase.Empty;
            CannonId = -1;
            Ammo = 0;
            MaxAmmo = 0;
            Dismissed = false;
            LastTargetSlice = -1;
            LastTargetLane = -1;
        }

        internal SlotState Clone()
        {
            return (SlotState)MemberwiseClone();
        }
    }

    /// <summary>A projectile in flight; its target scale is reserved until <see cref="ImpactAt"/>.</summary>
    public readonly struct Shot
    {
        public readonly int Id;
        public readonly int Slot;
        public readonly int CannonId;
        public readonly int Slice;
        public readonly int Lane;
        public readonly byte Color;
        public readonly float FiredAt;
        public readonly float ImpactAt;

        public Shot(int id, int slot, int cannonId, int slice, int lane, byte color, float firedAt, float impactAt)
        {
            Id = id;
            Slot = slot;
            CannonId = cannonId;
            Slice = slice;
            Lane = lane;
            Color = color;
            FiredAt = firedAt;
            ImpactAt = impactAt;
        }
    }
}
