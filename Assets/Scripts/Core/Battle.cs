using System;
using System.Collections.Generic;

namespace PixelDefense.Core
{
    /// <summary>
    /// Deterministic battle simulation. Cannons fire on fixed per-slot time grids and every event (landing, shot,
    /// impact, slot freed) is processed at its exact scheduled time, so outcomes don't depend on frame rate;
    /// only the dragon's continuous crawl is integrated in small steps.
    /// </summary>
    public sealed class Battle
    {
        private const float Epsilon = 1e-5f;
        private const int MaxEventsPerTick = 100000;

        // Keeps a misconfigured start from beginning the level already touching the base.
        private const float StartMargin = 0.05f;

        // A contact point no dragon can reach: planning copies only care about deploy order, never about time.
        private const float UnreachableContact = float.MaxValue / 4f;

        // Offsets cannon keys away from the dragon's cube keys in the state hash.
        private const int DeployedKeyOffset = 1 << 24;

        private readonly LevelDefinition _level;
        private readonly BattleSettings _settings;
        private readonly CannonSpec[] _cannons;
        private readonly int[] _cannonColumn;
        private readonly int[] _cannonDepth;
        private readonly int[][] _columnIds;
        private readonly int[] _columnHead;
        private readonly int[] _columnRemaining;
        private readonly bool[] _deployed;
        private readonly SlotState[] _slots;
        private readonly Queue<Shot> _shots;
        private readonly int[] _bombSlices;
        private int _slotCount;
        private int _nextShotId;
        private float _frozenUntil;
        private float _jamSince;
        private ulong _deployedHash;

        public Battle(LevelDefinition level, BattleSettings settings, float contactFront)
        {
            _level = level;
            _settings = settings;
            ContactFront = contactFront;
            Dragon = new DragonBody(level.CopyBody(), level.Width, level.Window, Math.Min(level.Start, contactFront - StartMargin));

            _columnIds = new int[level.ColumnCount][];
            _columnHead = new int[level.ColumnCount];
            _columnRemaining = new int[level.ColumnCount];
            _cannons = new CannonSpec[level.CannonCount];
            _cannonColumn = new int[_cannons.Length];
            _cannonDepth = new int[_cannons.Length];
            _deployed = new bool[_cannons.Length];
            int id = 0;
            for (int c = 0; c < level.ColumnCount; c++)
            {
                _columnIds[c] = new int[level.ColumnLength(c)];
                _columnRemaining[c] = _columnIds[c].Length;
                for (int d = 0; d < _columnIds[c].Length; d++)
                {
                    _cannons[id] = level.Cannon(c, d);
                    _cannonColumn[id] = c;
                    _cannonDepth[id] = d;
                    _columnIds[c][d] = id++;
                }
            }

            _slots = new SlotState[Math.Max(settings.MaxSlots, level.Slots)];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new SlotState();
            }
            _slotCount = level.Slots;
            _shots = new Queue<Shot>(64);
            _bombSlices = new int[Math.Max(1, settings.BombSlices)];
            MovementEnabled = true;
        }

        private Battle(Battle source, bool planning)
        {
            _level = source._level;
            _settings = source._settings;
            _cannons = source._cannons;
            _cannonColumn = source._cannonColumn;
            _cannonDepth = source._cannonDepth;
            _columnIds = source._columnIds;
            _columnHead = (int[])source._columnHead.Clone();
            _columnRemaining = (int[])source._columnRemaining.Clone();
            _deployed = (bool[])source._deployed.Clone();
            _deployedHash = source._deployedHash;
            _slots = new SlotState[source._slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = source._slots[i].Clone();
            }
            _slotCount = source._slotCount;
            _shots = new Queue<Shot>(source._shots);
            _bombSlices = new int[source._bombSlices.Length];
            _nextShotId = source._nextShotId;
            _frozenUntil = source._frozenUntil;
            _jamSince = source._jamSince;
            Dragon = source.Dragon.Clone();
            Phase = source.Phase;
            Time = source.Time;
            IsEnraged = source.IsEnraged;
            IsJammed = source.IsJammed;
            IsFrozen = source.IsFrozen;
            DeployedCount = source.DeployedCount;
            ActionCount = source.ActionCount;

            if (planning)
            {
                ContactFront = UnreachableContact;
                MovementEnabled = false;
            }
            else
            {
                ContactFront = source.ContactFront;
                InContact = source.InContact;
                ContactTimer = source.ContactTimer;
                MovementEnabled = source.MovementEnabled;
            }
        }

        public event Action<int, int, int> CannonDeployed;
        public event Action<int, int> CannonRevealed;
        public event Action<int> CannonLanded;
        public event Action<Shot> ShotFired;
        public event Action<int, int, byte, int> CubeDestroyed;
        public event Action<int> SliceCleared;
        public event Action<byte> ColorCleared;
        public event Action<int> CannonEmptied;
        public event Action<int> CannonDismissed;
        public event Action<int> SlotFreed;
        public event Action<int> SlotAdded;
        public event Action<bool> ContactChanged;
        public event Action<bool> JamChanged;
        public event Action<bool> FreezeChanged;
        public event Action Enraged;
        public event Action Won;
        public event Action Lost;

        public LevelDefinition Level => _level;
        public BattleSettings Settings => _settings;
        public DragonBody Dragon { get; }
        public BattlePhase Phase { get; private set; }
        public float Time { get; private set; }

        /// <summary>Front-slice position at which the head touches the base.</summary>
        public float ContactFront { get; }

        public bool InContact { get; private set; }
        public float ContactTimer { get; private set; }
        public float ContactProgress => ContactTimer / _settings.ContactLoseTime;
        public bool IsEnraged { get; private set; }
        public bool IsJammed { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsRushing => IsJammed && Time - _jamSince >= _settings.JamGraceTime - Epsilon;

        /// <summary>When false the dragon never moves (solver mode).</summary>
        public bool MovementEnabled { get; set; }

        /// <summary>Cannons that have left their columns so far.</summary>
        public int DeployedCount { get; private set; }

        /// <summary>Counts every successful player action (deploys and boosters); planners use it to spot outside changes.</summary>
        public int ActionCount { get; private set; }

        public float DistanceToBase => ContactFront - Dragon.FrontPosition;
        public int SlotCount => _slotCount;
        public int ColumnCount => _columnIds.Length;
        public int CannonCount => _cannons.Length;
        public int ShotsInFlight => _shots.Count;

        public SlotState GetSlot(int index)
        {
            return _slots[index];
        }

        public CannonSpec GetCannon(int cannonId)
        {
            return _cannons[cannonId];
        }

        public int CannonColumn(int cannonId)
        {
            return _cannonColumn[cannonId];
        }

        public int CannonDepth(int cannonId)
        {
            return _cannonDepth[cannonId];
        }

        public int ColumnHead(int column)
        {
            return _columnHead[column];
        }

        public int ColumnLength(int column)
        {
            return _columnIds[column].Length;
        }

        public int ColumnRemaining(int column)
        {
            return _columnRemaining[column];
        }

        /// <summary>True once the cannon has left its column (tapped from the front or picked by the booster).</summary>
        public bool IsDeployed(int cannonId)
        {
            return _deployed[cannonId];
        }

        public int CannonIdAt(int column, int depth)
        {
            return _columnIds[column][depth];
        }

        /// <summary>Front cannon id of a column, or -1 when the column is empty.</summary>
        public int FrontCannon(int column)
        {
            int head = _columnHead[column];
            return head < _columnIds[column].Length ? _columnIds[column][head] : -1;
        }

        public bool IsRevealed(int cannonId)
        {
            return !_cannons[cannonId].Hidden || _deployed[cannonId] || _cannonDepth[cannonId] <= _columnHead[_cannonColumn[cannonId]];
        }

        public bool HasFreeSlot => FirstEmptySlot() >= 0;

        public float CurrentSpeed
        {
            get
            {
                if (!MovementEnabled || IsFrozen)
                {
                    return 0f;
                }

                float speed = _level.CrawlSpeed;
                float distance = DistanceToBase;
                if (distance < _settings.DangerZone)
                {
                    float t = Math.Max(0f, distance) / _settings.DangerZone;
                    speed *= _settings.DangerSpeedFactor + (1f - _settings.DangerSpeedFactor) * t;
                }

                if (IsEnraged)
                {
                    speed *= _settings.EnrageSpeedFactor;
                }

                if (IsRushing)
                {
                    speed *= _settings.JamSpeedFactor;
                }

                return speed;
            }
        }

        /// <summary>No shots in flight, no cannon mid-jump or leaving, and no cannon able to fire.</summary>
        public bool IsQuiescent
        {
            get
            {
                if (_shots.Count > 0)
                {
                    return false;
                }

                for (int i = 0; i < _slotCount; i++)
                {
                    SlotState slot = _slots[i];
                    if (slot.Phase == SlotPhase.Landing || slot.Phase == SlotPhase.Leaving)
                    {
                        return false;
                    }

                    if (slot.Phase == SlotPhase.Active &&
                        (Dragon.ColorRemaining(slot.Color) == 0 || Dragon.HasTarget(slot.Color)))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public Battle Clone()
        {
            return new Battle(this, false);
        }

        /// <summary>
        /// Copy for planning: the dragon stays put and can never touch the base, so only the deploy order matters.
        /// Event subscribers are not copied.
        /// </summary>
        public Battle CloneForPlanning()
        {
            return new Battle(this, true);
        }

        /// <summary>Solver identity of the current state: scales alive, cannons deployed and slot contents.</summary>
        public ulong StateHash
        {
            get
            {
                ulong hash = Dragon.StateHash ^ 0xA0761D6478BD642FUL;
                hash = Mix(hash, _deployedHash);

                for (int i = 0; i < _slotCount; i++)
                {
                    SlotState slot = _slots[i];
                    ulong value = slot.Phase == SlotPhase.Empty
                        ? 0UL
                        : ((ulong)slot.Phase << 40) | ((ulong)slot.Color << 32) | (uint)slot.Ammo;
                    hash = Mix(hash, value ^ ((ulong)i << 48));
                }
                return hash;
            }
        }

        /// <summary>Taps a column: its front cannon jumps to the first free slot.</summary>
        public DeployResult Deploy(int column)
        {
            if (Phase != BattlePhase.Playing)
            {
                return DeployResult.NotPlaying;
            }

            if (column < 0 || column >= _columnIds.Length)
            {
                return DeployResult.InvalidColumn;
            }

            int cannonId = FrontCannon(column);
            return cannonId < 0 ? DeployResult.EmptyColumn : DeployCannon(cannonId);
        }

        /// <summary>
        /// Sends any cannon still waiting in a column to the first free slot, even from behind others (the pick
        /// booster). Cannons behind it move up; a mystery cannon picked this way shows its color as it jumps.
        /// </summary>
        public DeployResult DeployCannon(int cannonId)
        {
            if (Phase != BattlePhase.Playing)
            {
                return DeployResult.NotPlaying;
            }

            if (cannonId < 0 || cannonId >= _cannons.Length || _deployed[cannonId])
            {
                return DeployResult.Unavailable;
            }

            int slotIndex = FirstEmptySlot();
            if (slotIndex < 0)
            {
                return DeployResult.SlotsFull;
            }

            int column = _cannonColumn[cannonId];
            bool wasRevealed = IsRevealed(cannonId);
            _deployed[cannonId] = true;
            _deployedHash ^= DragonBody.CubeKey(DeployedKeyOffset + cannonId);
            _columnRemaining[column]--;
            DeployedCount++;
            ActionCount++;

            int[] ids = _columnIds[column];
            int oldHead = _columnHead[column];
            int head = oldHead;
            while (head < ids.Length && _deployed[ids[head]])
            {
                head++;
            }
            _columnHead[column] = head;

            CannonSpec spec = _cannons[cannonId];
            SlotState slot = _slots[slotIndex];
            slot.Clear();
            slot.Phase = SlotPhase.Landing;
            slot.CannonId = cannonId;
            slot.Color = spec.Color;
            slot.Ammo = spec.Ammo;
            slot.MaxAmmo = spec.Ammo;
            slot.ReadyAt = Time + _settings.DeployDuration;

            CannonDeployed?.Invoke(column, slotIndex, cannonId);

            if (!wasRevealed)
            {
                CannonRevealed?.Invoke(column, cannonId);
            }

            if (head != oldHead && head < ids.Length && _cannons[ids[head]].Hidden)
            {
                CannonRevealed?.Invoke(column, ids[head]);
            }

            UpdateJam();
            return DeployResult.Deployed;
        }

        public void Tick(float deltaTime)
        {
            if (Phase != BattlePhase.Playing || !(deltaTime > 0f))
            {
                return;
            }

            float end = Time + deltaTime;
            for (int guard = 0; guard < MaxEventsPerTick && Phase == BattlePhase.Playing; guard++)
            {
                float next = NextEventTime();
                AdvanceDragon(next < end ? next : end);
                if (Phase != BattlePhase.Playing || next > end)
                {
                    break;
                }
                ProcessDueEvents();
            }
        }

        /// <summary>Advances until nothing more will happen without player input (or the battle ends).</summary>
        public bool RunUntilQuiescent(float maxSeconds)
        {
            float limit = Time + maxSeconds;
            while (Phase == BattlePhase.Playing && Time < limit)
            {
                if (IsQuiescent)
                {
                    return true;
                }
                Tick(0.25f);
            }
            return Phase == BattlePhase.Playing && IsQuiescent;
        }

        public bool UseFreeze()
        {
            if (Phase != BattlePhase.Playing)
            {
                return false;
            }

            _frozenUntil = Time + _settings.FreezeDuration;
            ActionCount++;
            if (!IsFrozen)
            {
                IsFrozen = true;
                FreezeChanged?.Invoke(true);
            }
            return true;
        }

        /// <summary>Destroys every free scale in the front slices regardless of color; returns how many.</summary>
        public int UseBomb()
        {
            if (Phase != BattlePhase.Playing)
            {
                return 0;
            }

            ActionCount++;
            int count = 0;
            for (int w = 0; w < Dragon.WindowCount && count < _bombSlices.Length; w++)
            {
                _bombSlices[count++] = Dragon.WindowSlice(w);
            }

            // The window only spans `Window` slices; extend past it for a wider bomb.
            for (int s = count > 0 ? _bombSlices[count - 1] + 1 : 0; s < Dragon.SliceCount && count < _bombSlices.Length; s++)
            {
                if (Dragon.IsSliceAlive(s))
                {
                    _bombSlices[count++] = s;
                }
            }

            int destroyed = 0;
            for (int k = 0; k < count && Phase == BattlePhase.Playing; k++)
            {
                int slice = _bombSlices[k];
                for (int lane = 0; lane < Dragon.Width && Phase == BattlePhase.Playing; lane++)
                {
                    if (!Dragon.IsAlive(slice, lane) || Dragon.IsReserved(slice, lane))
                    {
                        continue;
                    }

                    byte color = Dragon.ColorAt(slice, lane);
                    DestroyOutcome outcome = Dragon.Destroy(slice, lane);
                    destroyed++;
                    CubeDestroyed?.Invoke(slice, lane, color, -1);
                    HandleOutcome(outcome, slice, color);
                }
            }

            UpdateJam();
            return destroyed;
        }

        public bool AddSlot()
        {
            if (Phase != BattlePhase.Playing || _slotCount >= _slots.Length)
            {
                return false;
            }

            _slots[_slotCount].Clear();
            _slotCount++;
            ActionCount++;
            SlotAdded?.Invoke(_slotCount - 1);
            UpdateJam();
            return true;
        }

        private int FirstEmptySlot()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].Phase == SlotPhase.Empty)
                {
                    return i;
                }
            }
            return -1;
        }

        private float NextEventTime()
        {
            float next = float.PositiveInfinity;
            if (_shots.Count > 0)
            {
                next = _shots.Peek().ImpactAt;
            }

            for (int i = 0; i < _slotCount; i++)
            {
                SlotState slot = _slots[i];
                switch (slot.Phase)
                {
                    case SlotPhase.Landing:
                        next = Math.Min(next, slot.ReadyAt);
                        break;
                    case SlotPhase.Active:
                        next = Math.Min(next, slot.NextFireAt);
                        break;
                    case SlotPhase.Leaving:
                        next = Math.Min(next, slot.FreeAt);
                        break;
                }
            }

            if (IsFrozen)
            {
                next = Math.Min(next, _frozenUntil);
            }

            if (IsJammed && !IsRushing)
            {
                next = Math.Min(next, _jamSince + _settings.JamGraceTime);
            }

            return next;
        }

        private void AdvanceDragon(float target)
        {
            while (Time < target && Phase == BattlePhase.Playing)
            {
                float step = Math.Min(target - Time, _settings.MaxStep);
                Dragon.TailPosition += CurrentSpeed * step;

                float maxTail = ContactFront - Math.Max(0, Dragon.AliveSlices - 1);
                bool touching = Dragon.AliveSlices > 0 && Dragon.TailPosition >= maxTail - Epsilon;
                if (touching)
                {
                    Dragon.TailPosition = maxTail;
                    if (!InContact)
                    {
                        InContact = true;
                        ContactTimer = 0f;
                        ContactChanged?.Invoke(true);
                    }

                    // A frozen dragon can't hurt the base, so its contact clock pauses.
                    if (!IsFrozen)
                    {
                        float remaining = _settings.ContactLoseTime - ContactTimer;
                        if (step >= remaining)
                        {
                            Time += remaining;
                            ContactTimer = _settings.ContactLoseTime;
                            Phase = BattlePhase.Lost;
                            Lost?.Invoke();
                            return;
                        }
                        ContactTimer += step;
                    }
                }
                else
                {
                    ReleaseContact();
                }

                Time = step >= target - Time ? target : Time + step;
            }
        }

        private void ProcessDueEvents()
        {
            bool any = true;
            while (any && Phase == BattlePhase.Playing)
            {
                any = false;
                float due = Time + Epsilon;

                while (_shots.Count > 0 && _shots.Peek().ImpactAt <= due && Phase == BattlePhase.Playing)
                {
                    Impact(_shots.Dequeue());
                    any = true;
                }

                if (IsFrozen && _frozenUntil <= due)
                {
                    IsFrozen = false;
                    FreezeChanged?.Invoke(false);
                    any = true;
                }

                for (int i = 0; i < _slotCount && Phase == BattlePhase.Playing; i++)
                {
                    SlotState slot = _slots[i];
                    if (slot.Phase == SlotPhase.Landing && slot.ReadyAt <= due)
                    {
                        Land(i);
                        any = true;
                    }
                    else if (slot.Phase == SlotPhase.Leaving && slot.FreeAt <= due)
                    {
                        slot.Clear();
                        SlotFreed?.Invoke(i);
                        any = true;
                    }
                }

                for (int i = 0; i < _slotCount && Phase == BattlePhase.Playing; i++)
                {
                    SlotState slot = _slots[i];
                    if (slot.Phase == SlotPhase.Active && slot.NextFireAt <= due)
                    {
                        Fire(i);
                        any = true;
                    }
                }
            }

            UpdateJam();
        }

        private void Land(int index)
        {
            SlotState slot = _slots[index];
            slot.Phase = SlotPhase.Active;
            float interval = _settings.FireInterval;
            float phase = interval * index / _slots.Length;
            double steps = Math.Ceiling((slot.ReadyAt - phase) / interval - 1e-4);
            slot.NextFireAt = phase + (float)Math.Max(0.0, steps) * interval;
            CannonLanded?.Invoke(index);

            if (Dragon.ColorRemaining(slot.Color) == 0)
            {
                Leave(index, true);
            }
        }

        private void Fire(int index)
        {
            SlotState slot = _slots[index];
            if (Dragon.ColorRemaining(slot.Color) == 0)
            {
                Leave(index, true);
                return;
            }

            float fireTime = slot.NextFireAt;
            slot.NextFireAt += _settings.FireInterval;

            if (!Dragon.TryFindTarget(slot.Color, out int slice, out int lane))
            {
                return;
            }

            Dragon.Reserve(slice, lane);
            slot.Ammo--;
            slot.LastTargetSlice = slice;
            slot.LastTargetLane = lane;
            var shot = new Shot(_nextShotId++, index, slot.CannonId, slice, lane, slot.Color, fireTime, fireTime + _settings.ShotFlightTime);
            _shots.Enqueue(shot);
            ShotFired?.Invoke(shot);

            if (slot.Ammo <= 0)
            {
                Leave(index, false);
            }
        }

        private void Leave(int index, bool dismissed)
        {
            SlotState slot = _slots[index];
            slot.Phase = SlotPhase.Leaving;
            slot.Dismissed = dismissed;
            slot.FreeAt = Time + _settings.LeaveDelay;
            if (dismissed)
            {
                CannonDismissed?.Invoke(index);
            }
            else
            {
                CannonEmptied?.Invoke(index);
            }
        }

        private void Impact(Shot shot)
        {
            if (!Dragon.IsAlive(shot.Slice, shot.Lane))
            {
                return;
            }

            DestroyOutcome outcome = Dragon.Destroy(shot.Slice, shot.Lane);
            CubeDestroyed?.Invoke(shot.Slice, shot.Lane, shot.Color, shot.Id);
            HandleOutcome(outcome, shot.Slice, shot.Color);
        }

        private void HandleOutcome(DestroyOutcome outcome, int slice, byte color)
        {
            if ((outcome & DestroyOutcome.SliceCleared) != 0)
            {
                // Bodies stay packed: the tail closes the part of the gap the head doesn't recoil into.
                Dragon.TailPosition += 1f - _settings.RecoilFraction;
                SliceCleared?.Invoke(slice);
                if (InContact && Dragon.FrontPosition < ContactFront - Epsilon)
                {
                    ReleaseContact();
                }
            }

            if ((outcome & DestroyOutcome.ColorCleared) != 0)
            {
                ColorCleared?.Invoke(color);
                for (int i = 0; i < _slotCount; i++)
                {
                    if (_slots[i].Phase == SlotPhase.Active && _slots[i].Color == color)
                    {
                        Leave(i, true);
                    }
                }
            }

            if (!IsEnraged && Dragon.AliveCubes > 0 && Dragon.AliveFraction <= _settings.EnrageFraction)
            {
                IsEnraged = true;
                Enraged?.Invoke();
            }

            if ((outcome & DestroyOutcome.AllCleared) != 0)
            {
                Phase = BattlePhase.Won;
                ReleaseContact();
                Won?.Invoke();
            }
        }

        private void ReleaseContact()
        {
            if (!InContact)
            {
                return;
            }

            InContact = false;
            ContactTimer = 0f;
            ContactChanged?.Invoke(false);
        }

        private void UpdateJam()
        {
            bool jammed = Phase == BattlePhase.Playing && ComputeJam();
            if (jammed == IsJammed)
            {
                return;
            }

            IsJammed = jammed;
            _jamSince = Time;
            JamChanged?.Invoke(jammed);
        }

        private bool ComputeJam()
        {
            if (_shots.Count > 0 || _slotCount == 0)
            {
                return false;
            }

            for (int i = 0; i < _slotCount; i++)
            {
                SlotState slot = _slots[i];
                if (slot.Phase != SlotPhase.Active)
                {
                    return false;
                }

                if (Dragon.ColorRemaining(slot.Color) == 0 || Dragon.HasTarget(slot.Color))
                {
                    return false;
                }
            }
            return true;
        }

        private static ulong Mix(ulong hash, ulong value)
        {
            hash ^= value + 0x9E3779B97F4A7C15UL + (hash << 6) + (hash >> 2);
            return hash * 0xFF51AFD7ED558CCDUL;
        }
    }
}
