using System;

namespace PixelDefense.Core
{
    [Flags]
    public enum DestroyOutcome
    {
        None = 0,
        SliceCleared = 1,
        ColorCleared = 2,
        AllCleared = 4
    }

    /// <summary>
    /// The dragon's scales. Slice 0 is the neck (just behind the head); higher indices run toward the tail.
    /// Alive slices are packed behind the head and anchored at the tail, so clearing any slice pulls the head
    /// back by one slice. Only the front-most <see cref="WindowSize"/> alive slices are exposed to cannons.
    /// </summary>
    public sealed class DragonBody
    {
        private readonly byte[] _colors;
        private readonly bool[] _alive;
        private readonly bool[] _reserved;
        private readonly int[] _sliceAlive;
        private readonly int[] _colorAlive;
        private readonly int[] _laneOrder;
        private readonly int[] _window;
        private int _windowCount;
        private int _frontSlice;

        public DragonBody(byte[] colors, int width, int windowSize, float frontPosition)
        {
            _colors = colors;
            Width = width;
            WindowSize = windowSize;
            SliceCount = colors.Length / width;
            TotalCubes = colors.Length;
            _alive = new bool[colors.Length];
            _reserved = new bool[colors.Length];
            _sliceAlive = new int[SliceCount];
            _colorAlive = new int[ScaleColors.Count];
            _window = new int[windowSize];
            _laneOrder = BuildLaneOrder(width);

            for (int i = 0; i < colors.Length; i++)
            {
                _alive[i] = true;
                _colorAlive[colors[i]]++;
            }

            for (int s = 0; s < SliceCount; s++)
            {
                _sliceAlive[s] = width;
            }

            AliveCubes = TotalCubes;
            AliveSlices = SliceCount;
            TailPosition = frontPosition - (SliceCount - 1);
            RebuildWindow();
        }

        private DragonBody(DragonBody source)
        {
            _colors = source._colors;
            _laneOrder = source._laneOrder;
            Width = source.Width;
            WindowSize = source.WindowSize;
            SliceCount = source.SliceCount;
            TotalCubes = source.TotalCubes;
            _alive = (bool[])source._alive.Clone();
            _reserved = (bool[])source._reserved.Clone();
            _sliceAlive = (int[])source._sliceAlive.Clone();
            _colorAlive = (int[])source._colorAlive.Clone();
            _window = (int[])source._window.Clone();
            _windowCount = source._windowCount;
            _frontSlice = source._frontSlice;
            AliveCubes = source.AliveCubes;
            AliveSlices = source.AliveSlices;
            TailPosition = source.TailPosition;
            StateHash = source.StateHash;
        }

        public int Width { get; }
        public int SliceCount { get; }
        public int WindowSize { get; }
        public int TotalCubes { get; }
        public int AliveCubes { get; private set; }
        public int AliveSlices { get; private set; }

        /// <summary>Path position (slice units) of the tail-most alive slice.</summary>
        public float TailPosition { get; internal set; }

        /// <summary>Path position of the front-most alive slice; the head sits just ahead of it.</summary>
        public float FrontPosition => TailPosition + (AliveSlices > 0 ? AliveSlices - 1 : 0);

        /// <summary>Index of the front-most alive slice, or <see cref="SliceCount"/> when the body is gone.</summary>
        public int FrontSlice => _frontSlice;

        /// <summary>Zobrist hash of which scales are alive; used by the solver to recognise repeated states.</summary>
        public ulong StateHash { get; private set; }

        public int WindowCount => _windowCount;

        public float AliveFraction => TotalCubes > 0 ? (float)AliveCubes / TotalCubes : 0f;

        public DragonBody Clone()
        {
            return new DragonBody(this);
        }

        public int WindowSlice(int index)
        {
            return _window[index];
        }

        public byte ColorAt(int slice, int lane)
        {
            return _colors[slice * Width + lane];
        }

        public bool IsAlive(int slice, int lane)
        {
            return _alive[slice * Width + lane];
        }

        public bool IsReserved(int slice, int lane)
        {
            return _reserved[slice * Width + lane];
        }

        public bool IsSliceAlive(int slice)
        {
            return _sliceAlive[slice] > 0;
        }

        public int ColorRemaining(byte color)
        {
            return _colorAlive[color];
        }

        public bool IsExposed(int slice)
        {
            for (int w = 0; w < _windowCount; w++)
            {
                if (_window[w] == slice)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Front-most free scale of <paramref name="color"/> inside the exposed window, center lanes first.</summary>
        public bool TryFindTarget(byte color, out int slice, out int lane)
        {
            for (int w = 0; w < _windowCount; w++)
            {
                int s = _window[w];
                int start = s * Width;
                for (int k = 0; k < Width; k++)
                {
                    int l = _laneOrder[k];
                    int i = start + l;
                    if (_alive[i] && !_reserved[i] && _colors[i] == color)
                    {
                        slice = s;
                        lane = l;
                        return true;
                    }
                }
            }

            slice = -1;
            lane = -1;
            return false;
        }

        public bool HasTarget(byte color)
        {
            return TryFindTarget(color, out _, out _);
        }

        /// <summary>Writes each slice's path position; cleared slices get NaN.</summary>
        public void GetSlicePositions(float[] positions)
        {
            float position = TailPosition;
            for (int s = SliceCount - 1; s >= 0; s--)
            {
                if (_sliceAlive[s] > 0)
                {
                    positions[s] = position;
                    position += 1f;
                }
                else
                {
                    positions[s] = float.NaN;
                }
            }
        }

        internal void Reserve(int slice, int lane)
        {
            _reserved[slice * Width + lane] = true;
        }

        internal DestroyOutcome Destroy(int slice, int lane)
        {
            int i = slice * Width + lane;
            if (!_alive[i])
            {
                return DestroyOutcome.None;
            }

            _alive[i] = false;
            _reserved[i] = false;
            StateHash ^= CubeKey(i);
            AliveCubes--;
            byte color = _colors[i];
            _colorAlive[color]--;

            var outcome = DestroyOutcome.None;
            if (_colorAlive[color] == 0)
            {
                outcome |= DestroyOutcome.ColorCleared;
            }

            if (--_sliceAlive[slice] == 0)
            {
                AliveSlices--;
                outcome |= DestroyOutcome.SliceCleared;
                RebuildWindow();
            }

            if (AliveCubes == 0)
            {
                outcome |= DestroyOutcome.AllCleared;
            }

            return outcome;
        }

        private void RebuildWindow()
        {
            _windowCount = 0;
            _frontSlice = SliceCount;
            for (int s = 0; s < SliceCount && _windowCount < WindowSize; s++)
            {
                if (_sliceAlive[s] > 0)
                {
                    if (_windowCount == 0)
                    {
                        _frontSlice = s;
                    }
                    _window[_windowCount++] = s;
                }
            }
        }

        private static int[] BuildLaneOrder(int width)
        {
            var order = new int[width];
            int mid = width / 2;
            order[0] = mid;
            int n = 1;
            for (int d = 1; n < width; d++)
            {
                if (mid - d >= 0)
                {
                    order[n++] = mid - d;
                }
                if (n < width && mid + d < width)
                {
                    order[n++] = mid + d;
                }
            }
            return order;
        }

        internal static ulong CubeKey(int index)
        {
            // SplitMix64: deterministic, well-distributed keys without a stored random table.
            ulong z = (ulong)index * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
