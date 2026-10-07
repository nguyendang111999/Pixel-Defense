using System;
using System.Collections.Generic;

namespace PixelDefense.Core
{
    /// <summary>
    /// Track geometry in slice units (1 = one dragon body slice): a straight lead-in from off-screen, a spiral that
    /// winds inward, and a final curl into the central base. Arc length s = 0 at the arena entrance, negative on
    /// the lead-in, <see cref="EndS"/> where the head touches the base. Shared by the views and the simulations
    /// so path lengths in tests match the game.
    /// </summary>
    public sealed class TrackShape
    {
        public const float SampleStep = 0.25f;

        private readonly float[] _x;
        private readonly float[] _y;
        private readonly List<float[]> _walls;

        internal TrackShape(string id, int lanes, float laneWidth, float wallWidth, float baseRadius,
            float minS, float[] x, float[] y, List<float[]> walls, float arenaRadius)
        {
            Id = id;
            Lanes = lanes;
            LaneWidth = laneWidth;
            WallWidth = wallWidth;
            BaseRadius = baseRadius;
            MinS = minS;
            _x = x;
            _y = y;
            _walls = walls;
            ArenaRadius = arenaRadius;
            MaxS = minS + (x.Length - 1) * SampleStep;
        }

        public string Id { get; }
        public int Lanes { get; }
        public float LaneWidth { get; }
        public float WallWidth { get; }
        public float BaseRadius { get; }

        /// <summary>Radius that encloses every wall; used to frame the camera.</summary>
        public float ArenaRadius { get; }

        public float MinS { get; }
        public float MaxS { get; }

        /// <summary>Arc length at the base contact point.</summary>
        public float EndS => MaxS;

        /// <summary>Wall centerlines as flattened (x, y) polylines.</summary>
        public IReadOnlyList<float[]> Walls => _walls;

        /// <summary>Front-slice position at which the head (of the given length) touches the base.</summary>
        public float ContactFront(float headLength)
        {
            return EndS - headLength;
        }

        public void Sample(float s, out float x, out float y, out float tangentX, out float tangentY)
        {
            float f = (s - MinS) / SampleStep;
            int last = _x.Length - 1;
            if (f <= 0f)
            {
                // Extrapolate along the lead-in so tails parked far off-screen still have positions.
                tangentX = _x[1] - _x[0];
                tangentY = _y[1] - _y[0];
                Normalize(ref tangentX, ref tangentY);
                x = _x[0] + tangentX * f * SampleStep;
                y = _y[0] + tangentY * f * SampleStep;
                return;
            }

            if (f >= last)
            {
                tangentX = _x[last] - _x[last - 1];
                tangentY = _y[last] - _y[last - 1];
                Normalize(ref tangentX, ref tangentY);
                x = _x[last];
                y = _y[last];
                return;
            }

            int i = (int)f;
            float t = f - i;
            x = _x[i] + (_x[i + 1] - _x[i]) * t;
            y = _y[i] + (_y[i + 1] - _y[i]) * t;

            // Blend neighbouring segment directions so cubes rotate smoothly instead of snapping per sample.
            int prev = i > 0 ? i - 1 : i;
            int next = i + 2 <= last ? i + 2 : i + 1;
            float ax = _x[i + 1] - _x[prev];
            float ay = _y[i + 1] - _y[prev];
            float bx = _x[next] - _x[i];
            float by = _y[next] - _y[i];
            tangentX = ax + (bx - ax) * t;
            tangentY = ay + (by - ay) * t;
            Normalize(ref tangentX, ref tangentY);
        }

        private static void Normalize(ref float x, ref float y)
        {
            float length = (float)Math.Sqrt(x * x + y * y);
            if (length > 1e-6f)
            {
                x /= length;
                y /= length;
            }
            else
            {
                x = 1f;
                y = 0f;
            }
        }
    }

    /// <summary>Named track layouts referenced by level files (<c>track spiral</c>).</summary>
    public static class TrackPresets
    {
        public const float WallWidth = 0.9f;
        public const float LaneMargin = 0.8f;
        public const float BaseRadius = 8.4f;
        public const float LeadInLength = 220f;
        public const float VisibleLeadWall = 34f;

        private const float TwoPi = (float)(Math.PI * 2.0);
        private const float Deg = (float)(Math.PI / 180.0);

        public static bool Exists(string id)
        {
            return TryGetSpec(id, out _);
        }

        public static TrackShape Create(string id, int lanes)
        {
            if (!TryGetSpec(id, out Spec spec))
            {
                spec = DefaultSpec;
            }
            return Build(id, lanes, spec);
        }

        private struct Spec
        {
            public float Turns;
            public float EndAngleDeg;
            public int Direction;
            public float Squareness;
        }

        // Whole turns that end at the top (90°): the dragon enters along the top edge from off-screen and its
        // final curl comes down into the base facing the camera.
        private static readonly Spec DefaultSpec = new Spec { Turns = 2f, EndAngleDeg = 90f, Direction = 1, Squareness = 2f };

        private static bool TryGetSpec(string id, out Spec spec)
        {
            switch (id)
            {
                case "spiral":
                    spec = DefaultSpec;
                    return true;
                case "spiral_cw":
                    spec = new Spec { Turns = 2f, EndAngleDeg = 90f, Direction = -1, Squareness = 2f };
                    return true;
                case "spiral_long":
                    spec = new Spec { Turns = 3f, EndAngleDeg = 90f, Direction = 1, Squareness = 2f };
                    return true;
                case "square":
                    spec = new Spec { Turns = 2f, EndAngleDeg = 90f, Direction = 1, Squareness = 4.5f };
                    return true;
                case "square_cw":
                    spec = new Spec { Turns = 2f, EndAngleDeg = 90f, Direction = -1, Squareness = 4.5f };
                    return true;
                default:
                    spec = default;
                    return false;
            }
        }

        private static TrackShape Build(string id, int lanes, Spec spec)
        {
            float laneWidth = lanes + LaneMargin;
            float pitch = laneWidth + WallWidth;
            float innerRadius = BaseRadius + WallWidth + laneWidth * 0.5f;
            float outerRadius = innerRadius + pitch * spec.Turns;
            float sweep = TwoPi * spec.Turns;
            float endAngle = spec.EndAngleDeg * Deg;
            float startAngle = endAngle - spec.Direction * sweep;

            // Dense spiral samples (by angle), then the inward curl, then uniform arc-length resampling.
            var px = new List<float>(4096);
            var py = new List<float>(4096);
            int spiralSteps = (int)(spec.Turns * 720f);
            for (int i = 0; i <= spiralSteps; i++)
            {
                float phi = sweep * i / spiralSteps;
                float r = outerRadius - (outerRadius - innerRadius) * phi / sweep;
                SpiralPoint(startAngle + spec.Direction * phi, r, spec.Squareness, out float sx, out float sy);
                px.Add(sx);
                py.Add(sy);
            }

            int spiralEnd = px.Count - 1;
            AppendCurl(px, py, spec, endAngle);

            // Lead-in: straight line backwards along the entrance tangent.
            float tx = px[1] - px[0];
            float ty = py[1] - py[0];
            float tl = (float)Math.Sqrt(tx * tx + ty * ty);
            tx /= tl;
            ty /= tl;

            float[] cumulative = Cumulative(px, py);
            float spiralLength = cumulative[cumulative.Length - 1];
            float minS = -LeadInLength;
            int count = (int)Math.Floor((spiralLength - minS) / TrackShape.SampleStep) + 1;
            var x = new float[count];
            var y = new float[count];
            int segment = 0;
            for (int i = 0; i < count; i++)
            {
                float s = minS + i * TrackShape.SampleStep;
                if (s <= 0f)
                {
                    x[i] = px[0] + tx * s;
                    y[i] = py[0] + ty * s;
                    continue;
                }

                while (segment < cumulative.Length - 2 && cumulative[segment + 1] < s)
                {
                    segment++;
                }

                float a = cumulative[segment];
                float b = cumulative[segment + 1];
                float t = b > a ? (s - a) / (b - a) : 0f;
                x[i] = px[segment] + (px[segment + 1] - px[segment]) * t;
                y[i] = py[segment] + (py[segment + 1] - py[segment]) * t;
            }

            var walls = BuildWalls(px, py, spiralEnd, spec, pitch, tx, ty, out float arenaRadius);
            return new TrackShape(id, lanes, laneWidth, WallWidth, BaseRadius, minS, x, y, walls, arenaRadius);
        }

        private static void SpiralPoint(float angle, float radius, float squareness, out float x, out float y)
        {
            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);
            if (squareness <= 2.001f)
            {
                x = radius * c;
                y = radius * s;
                return;
            }

            // Superellipse: |x|^p + |y|^p = r^p gives a rounded square that still winds smoothly.
            float e = 2f / squareness;
            x = radius * Math.Sign(c) * (float)Math.Pow(Math.Abs(c), e);
            y = radius * Math.Sign(s) * (float)Math.Pow(Math.Abs(s), e);
        }

        private static void AppendCurl(List<float> px, List<float> py, Spec spec, float endAngle)
        {
            int last = px.Count - 1;
            float p0x = px[last];
            float p0y = py[last];
            float tx = p0x - px[last - 4];
            float ty = p0y - py[last - 4];
            float tl = (float)Math.Sqrt(tx * tx + ty * ty);
            tx /= tl;
            ty /= tl;

            float contactAngle = endAngle + spec.Direction * 70f * Deg;
            float contactRadius = BaseRadius + 0.15f;
            float cx = (float)Math.Cos(contactAngle) * contactRadius;
            float cy = (float)Math.Sin(contactAngle) * contactRadius;
            float nx = cx / contactRadius;
            float ny = cy / contactRadius;

            float handle = 3.2f;
            float p1x = p0x + tx * handle;
            float p1y = p0y + ty * handle;
            float p2x = cx + nx * 2.4f;
            float p2y = cy + ny * 2.4f;

            const int steps = 80;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float u = 1f - t;
                float b0 = u * u * u;
                float b1 = 3f * u * u * t;
                float b2 = 3f * u * t * t;
                float b3 = t * t * t;
                px.Add(b0 * p0x + b1 * p1x + b2 * p2x + b3 * cx);
                py.Add(b0 * p0y + b1 * p1y + b2 * p2y + b3 * cy);
            }
        }

        private static float[] Cumulative(List<float> px, List<float> py)
        {
            var cumulative = new float[px.Count];
            for (int i = 1; i < px.Count; i++)
            {
                float dx = px[i] - px[i - 1];
                float dy = py[i] - py[i - 1];
                cumulative[i] = cumulative[i - 1] + (float)Math.Sqrt(dx * dx + dy * dy);
            }
            return cumulative;
        }

        private static List<float[]> BuildWalls(List<float> px, List<float> py, int spiralEnd, Spec spec, float pitch,
            float leadTx, float leadTy, out float arenaRadius)
        {
            var walls = new List<float[]>();
            float half = pitch * 0.5f;
            float maxRadius = 0f;

            // Outer offset of the whole spiral: the outermost wall plus every separator between turns.
            var outer = new List<float>(spiralEnd * 2);
            for (int i = 0; i <= spiralEnd; i += 2)
            {
                Offset(px, py, i, half, out float wx, out float wy);
                outer.Add(wx);
                outer.Add(wy);
                maxRadius = Math.Max(maxRadius, (float)Math.Sqrt(wx * wx + wy * wy));
            }
            walls.Add(outer.ToArray());

            // Inner wall of the last turn, stopping short of the end so the curl can pass into the base.
            int perTurn = (int)(spiralEnd / spec.Turns);
            int innerStart = Math.Max(0, spiralEnd - perTurn);
            int innerEnd = spiralEnd - 26;
            var inner = new List<float>(perTurn * 2);
            for (int i = innerStart; i <= innerEnd; i += 2)
            {
                Offset(px, py, i, -half, out float wx, out float wy);
                inner.Add(wx);
                inner.Add(wy);
            }
            if (inner.Count >= 4)
            {
                walls.Add(inner.ToArray());
            }

            // Lead-in walls: straight continuations of the entrance edges.
            float nx = leadTy;
            float ny = -leadTx;
            if (nx * px[0] + ny * py[0] < 0f)
            {
                nx = -nx;
                ny = -ny;
            }

            walls.Add(StraightWall(px[0] + nx * half, py[0] + ny * half, -leadTx, -leadTy, 0f, VisibleLeadWall));
            walls.Add(StraightWall(px[0] - nx * half, py[0] - ny * half, -leadTx, -leadTy, 2.5f, VisibleLeadWall));

            arenaRadius = maxRadius + WallWidth * 0.5f;
            return walls;
        }

        private static float[] StraightWall(float ox, float oy, float dx, float dy, float from, float to)
        {
            const int steps = 24;
            var points = new float[(steps + 1) * 2];
            for (int i = 0; i <= steps; i++)
            {
                float d = from + (to - from) * i / steps;
                points[i * 2] = ox + dx * d;
                points[i * 2 + 1] = oy + dy * d;
            }
            return points;
        }

        /// <summary>Offsets sample i along its outward (away from the arena center) normal.</summary>
        private static void Offset(List<float> px, List<float> py, int i, float distance, out float x, out float y)
        {
            int a = Math.Max(0, i - 1);
            int b = Math.Min(px.Count - 1, i + 1);
            float tx = px[b] - px[a];
            float ty = py[b] - py[a];
            float length = (float)Math.Sqrt(tx * tx + ty * ty);
            tx /= length;
            ty /= length;
            float nx = ty;
            float ny = -tx;
            if (nx * px[i] + ny * py[i] < 0f)
            {
                nx = -nx;
                ny = -ny;
            }
            x = px[i] + nx * distance;
            y = py[i] + ny * distance;
        }
    }
}
