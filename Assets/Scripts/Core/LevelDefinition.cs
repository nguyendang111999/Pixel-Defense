using System.Collections.Generic;

namespace PixelDefense.Core
{
    /// <summary>
    /// Immutable description of one level: the dragon's scale layout (head to tail), the cannon columns
    /// (front to back) and per-level tuning. Built by <see cref="LevelParser"/>.
    /// </summary>
    public sealed class LevelDefinition
    {
        public const int DefaultWidth = 3;
        public const int DefaultWindow = 3;
        public const int DefaultSlots = 5;
        public const float DefaultSpeed = 1f;
        public const float DefaultStart = 20f;
        public const string DefaultTrack = "spiral";
        public const string DefaultSkin = "ember";

        private readonly byte[] _body;
        private readonly CannonSpec[][] _columns;

        public LevelDefinition(
            string id, string name, string track, string skin, float crawlSpeed, int width, int window,
            int slots, float start, string tutorial, byte[] body, CannonSpec[][] columns)
        {
            Id = id;
            Name = name;
            Track = track;
            Skin = skin;
            CrawlSpeed = crawlSpeed;
            Width = width;
            Window = window;
            Slots = slots;
            Start = start;
            Tutorial = tutorial;
            _body = body;
            _columns = columns;
        }

        public string Id { get; }
        public string Name { get; }
        public string Track { get; }
        public string Skin { get; }

        /// <summary>Dragon crawl speed in slices per second.</summary>
        public float CrawlSpeed { get; }

        /// <summary>Cubes per body slice (lanes across the track).</summary>
        public int Width { get; }

        /// <summary>How many front-most alive slices cannons can hit.</summary>
        public int Window { get; }

        public int Slots { get; }

        /// <summary>Where the front slice starts, in slices past the arena entrance.</summary>
        public float Start { get; }

        public string Tutorial { get; }

        public int SliceCount => _body.Length / Width;
        public int CubeCount => _body.Length;
        public int ColumnCount => _columns.Length;

        public byte BodyColor(int slice, int lane)
        {
            return _body[slice * Width + lane];
        }

        public int ColumnLength(int column)
        {
            return _columns[column].Length;
        }

        public CannonSpec Cannon(int column, int depth)
        {
            return _columns[column][depth];
        }

        public int CannonCount
        {
            get
            {
                int count = 0;
                for (int c = 0; c < _columns.Length; c++)
                {
                    count += _columns[c].Length;
                }
                return count;
            }
        }

        /// <summary>Copies the body colors (slice-major, head first) into a new array.</summary>
        public byte[] CopyBody()
        {
            var copy = new byte[_body.Length];
            System.Array.Copy(_body, copy, _body.Length);
            return copy;
        }

        public void CountCubesPerColor(int[] counts)
        {
            System.Array.Clear(counts, 0, counts.Length);
            for (int i = 0; i < _body.Length; i++)
            {
                counts[_body[i]]++;
            }
        }

        public void CountAmmoPerColor(int[] counts)
        {
            System.Array.Clear(counts, 0, counts.Length);
            for (int c = 0; c < _columns.Length; c++)
            {
                CannonSpec[] column = _columns[c];
                for (int d = 0; d < column.Length; d++)
                {
                    counts[column[d].Color] += column[d].Ammo;
                }
            }
        }

        /// <summary>Distinct colors in head-to-tail order of first appearance; drives the HUD health bar.</summary>
        public List<byte> ColorRuns()
        {
            var runs = new List<byte>();
            for (int s = 0; s < SliceCount; s++)
            {
                byte dominant = BodyColor(s, Width / 2);
                if (runs.Count == 0 || runs[runs.Count - 1] != dominant)
                {
                    runs.Add(dominant);
                }
            }
            return runs;
        }
    }
}
