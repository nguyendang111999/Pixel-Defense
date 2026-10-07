using System;
using System.Collections.Generic;

namespace PixelDefense.Core
{
    /// <summary>
    /// Track geometry in slice units (1 = one dragon body slice): a straight lead-in from off-screen, the level's
    /// path through the arena, and a final approach into the central base (centered on the origin). Arc length
    /// s = 0 at the arena entrance, negative on the lead-in, <see cref="EndS"/> where the head touches the base.
    /// Shared by the views and the simulations so path lengths in tests match the game.
    /// </summary>
    public sealed class TrackShape
    {
        public const float SampleStep = 0.25f;

        private readonly float[] _x;
        private readonly float[] _y;
        private readonly List<float[]> _walls;

        internal TrackShape(string id, int lanes, float laneWidth, float wallWidth, float baseRadius, float minS,
            float[] x, float[] y, List<float[]> walls, float[] outline, float[] bounds)
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
            Outline = outline;
            ArenaMinX = bounds[0];
            ArenaMaxX = bounds[1];
            ArenaMinY = bounds[2];
            ArenaMaxY = bounds[3];
            MaxS = minS + (x.Length - 1) * SampleStep;
        }

        public string Id { get; }
        public int Lanes { get; }
        public float LaneWidth { get; }
        public float WallWidth { get; }
        public float BaseRadius { get; }

        /// <summary>Distance between the centerlines of two side-by-side passes (one lane plus the wall between).</summary>
        public float Pitch => LaneWidth + WallWidth;

        /// <summary>Arena bounds (walls past the entrance plus the base), used to place the cannon tray.</summary>
        public float ArenaMinX { get; }
        public float ArenaMaxX { get; }
        public float ArenaMinY { get; }
        public float ArenaMaxY { get; }

        /// <summary>Convex outline of the arena as flattened (x, y) pairs; the camera frames it.</summary>
        public float[] Outline { get; }

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

        internal static void Normalize(ref float x, ref float y)
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

    /// <summary>
    /// Named track layouts referenced by level files (<c>track snake</c>). Every layout has a mirrored
    /// <c>_cw</c> twin. Passes that run side by side are exactly one <see cref="TrackShape.Pitch"/> apart so they
    /// share a wall.
    /// </summary>
    public static class TrackPresets
    {
        public const float WallWidth = 0.9f;
        public const float LaneMargin = 0.8f;
        public const float BaseRadius = 10.5f;
        public const float LeadInLength = 220f;
        public const float VisibleLeadWall = 34f;

        /// <summary>The head stops this far outside the platform edge.</summary>
        public const float ContactGap = 0.15f;

        private const string MirrorSuffix = "_cw";
        private const float TwoPi = (float)(Math.PI * 2.0);
        private const float Pi = (float)Math.PI;
        private const float Deg = (float)(Math.PI / 180.0);

        /// <summary>Every layout id (each base layout and its mirrored twin).</summary>
        public static readonly string[] Ids =
        {
            "spiral", "spiral_cw", "square", "square_cw", "spiral_long", "spiral_long_cw", "hairpin", "hairpin_cw",
            "zigzag", "zigzag_cw", "snake", "snake_cw", "maze", "maze_cw"
        };

        public static bool Exists(string id)
        {
            return Array.IndexOf(Ids, id) >= 0;
        }

        /// <summary>Builds the layout <paramref name="id"/> for a dragon <paramref name="lanes"/> cubes wide; unknown ids fall back to a spiral.</summary>
        public static TrackShape Create(string id, int lanes)
        {
            var layout = new Layout(lanes);
            var path = new PathBuilder();
            bool mirror = id != null && id.EndsWith(MirrorSuffix, StringComparison.Ordinal);
            string shape = mirror ? id.Substring(0, id.Length - MirrorSuffix.Length) : id;

            switch (shape)
            {
                case "square":
                    BuildSpiral(path, layout, 2f, 4.5f);
                    break;
                case "spiral_long":
                    BuildSpiral(path, layout, 3f, 2f);
                    break;
                case "hairpin":
                    BuildRings(path, layout, 2);
                    break;
                case "maze":
                    BuildRings(path, layout, 3);
                    break;
                case "zigzag":
                    BuildZigzag(path, layout);
                    break;
                case "snake":
                    BuildSnake(path, layout);
                    break;
                default:
                    BuildSpiral(path, layout, 2f, 2f);
                    break;
            }

            if (mirror)
            {
                path.MirrorX();
            }
            return Finish(id, lanes, layout, path);
        }

        /// <summary>Lane geometry derived from the dragon's width.</summary>
        private readonly struct Layout
        {
            public readonly float LaneWidth;
            public readonly float Pitch;
            public readonly float Half;

            /// <summary>Centerline radius of the innermost pass around the base (its inner wall hugs the platform).</summary>
            public readonly float Ring0;

            public readonly float ContactRadius;

            public Layout(int lanes)
            {
                LaneWidth = lanes + LaneMargin;
                Pitch = LaneWidth + WallWidth;
                Half = Pitch * 0.5f;
                Ring0 = BaseRadius + WallWidth + LaneWidth * 0.5f;
                ContactRadius = BaseRadius + ContactGap;
            }

            public float Ring(int index)
            {
                return Ring0 + Pitch * index;
            }
        }

        // ---------- Layouts (built counter-clockwise; _cw twins are mirrored) ----------

        /// <summary>
        /// Spiral winding inward, ending at the top of the base (90°). Whole turns start at the top too, so the
        /// dragon slides in along the top edge from off-screen right (left for the mirrored twin).
        /// <paramref name="squareness"/> &gt; 2 rounds the spiral into a superellipse (rounded square).
        /// </summary>
        private static void BuildSpiral(PathBuilder path, Layout layout, float turns, float squareness)
        {
            float inner = layout.Ring0;
            float outer = inner + layout.Pitch * turns;
            float sweep = TwoPi * turns;
            float endAngle = 90f * Deg;
            float startAngle = endAngle - sweep;
            int steps = (int)(turns * 720f);
            for (int i = 0; i <= steps; i++)
            {
                float phi = sweep * i / steps;
                float r = outer - (outer - inner) * phi / sweep;
                SpiralPoint(startAngle + phi, r, squareness, out float x, out float y);
                path.Add(x, y);
            }

            path.AppendCurl(endAngle + 70f * Deg, layout.ContactRadius);
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

        /// <summary>
        /// Concentric rings joined by U-turns, so the dragon reverses direction on every ring: enters along the top
        /// of the outer ring, and each hairpin drops one ring inward. Two rings = "hairpin", three = "maze".
        /// </summary>
        private static void BuildRings(PathBuilder path, Layout layout, int rings)
        {
            // Hairpin angles keep each U-turn clear of the lead-in and of the ring that comes back around to it.
            float[] turnAt = rings == 2 ? new[] { 40f } : new[] { 40f, 68f };
            float lastEnd = rings == 2 ? 150f : -40f;

            int ring = rings - 1;
            float angle = 90f;
            float direction = 1f;
            path.Start(0f, layout.Ring(ring), Pi);
            for (int k = 0; k < turnAt.Length; k++)
            {
                float target = turnAt[k];
                float sweep = direction > 0f ? Wrap(target - angle) : Wrap(angle - target);
                path.Arc(layout.Ring(ring), direction * sweep * Deg);
                path.Arc(layout.Half, direction * Pi);
                angle = target;
                direction = -direction;
                ring--;
            }

            float last = direction > 0f ? Wrap(lastEnd - angle) : Wrap(angle - lastEnd);
            path.Arc(layout.Ring(ring), direction * last * Deg);
            path.AppendCurl((lastEnd + direction * 60f) * Deg, layout.ContactRadius);
        }

        /// <summary>Angle in (0, 360] degrees.</summary>
        private static float Wrap(float degrees)
        {
            float wrapped = degrees % 360f;
            return wrapped <= 0f ? wrapped + 360f : wrapped;
        }

        /// <summary>
        /// Radial switchbacks around the base like the teeth of a gear: the dragon comes in from the left edge,
        /// then loops out and back in lobe after lobe, finally diving into the base from the upper left.
        /// </summary>
        private static void BuildZigzag(PathBuilder path, Layout layout)
        {
            const int lobes = 8;
            float spacing = 40f * Deg;
            float half = layout.Half;
            float tipRadius = layout.Ring(1) + 3.5f;

            // Fillet between neighbouring lobes, as tight as allowed while its inner wall stays off the platform.
            float sinHalf = (float)Math.Sin(spacing * 0.5f);
            float fillet = Math.Max(half, (layout.Ring0 - half / sinHalf) / (1f / sinHalf - 1f));
            float rho = (half + fillet) / sinHalf;
            float legStart = rho * (float)Math.Cos(spacing * 0.5f);
            float tip = tipRadius - half;
            float leg = tip - legStart;
            float turn = Pi - spacing;
            float contact = (float)Math.Sqrt(layout.ContactRadius * layout.ContactRadius - half * half);

            // Entry: along the left axis (just below it), from the arena edge toward the base.
            path.Start(-tipRadius, -half, 0f);
            path.Line(tipRadius - legStart);
            path.Arc(fillet, -turn);
            for (int k = 1; k <= lobes; k++)
            {
                path.Line(leg);
                path.Arc(half, Pi);
                if (k < lobes)
                {
                    path.Line(leg);
                    path.Arc(fillet, -turn);
                }
                else
                {
                    path.Line(tip - contact);
                }
            }
        }

        /// <summary>
        /// Back-and-forth rows above the base, top to bottom; the last row turns straight down into the base.
        /// </summary>
        private static void BuildSnake(PathBuilder path, Layout layout)
        {
            const int rows = 5;
            const float arenaHalfWidth = 24f;
            float half = layout.Half;
            float rowHalf = arenaHalfWidth - 2f * half - WallWidth * 0.5f;
            float drop = layout.Ring0 - layout.ContactRadius;

            // Rows alternate direction; the bottom row heads toward the center and curls down.
            float heading = rows % 2 == 1 ? 0f : Pi;
            float startX = heading == 0f ? -rowHalf : rowHalf;
            path.Start(startX, layout.Ring(rows - 1), heading);
            for (int row = rows - 1; row > 0; row--)
            {
                path.Line(2f * rowHalf);
                float turn = path.Heading == 0f ? -Pi : Pi;
                path.Arc(half, turn);
                path.SnapHeading();
            }

            path.Line(rowHalf - drop);
            path.Arc(drop, path.Heading == 0f ? -Pi * 0.5f : Pi * 0.5f);
        }

        // ---------- Common finishing: lead-in, uniform resampling, walls, bounds ----------

        private static TrackShape Finish(string id, int lanes, Layout layout, PathBuilder path)
        {
            List<float> px = path.X;
            List<float> py = path.Y;

            // Lead-in: straight line backwards along the entrance tangent.
            float tx = px[Math.Min(4, px.Count - 1)] - px[0];
            float ty = py[Math.Min(4, py.Count - 1)] - py[0];
            TrackShape.Normalize(ref tx, ref ty);

            float[] cumulative = Cumulative(px, py);
            float length = cumulative[cumulative.Length - 1];
            float minS = -LeadInLength;
            int count = (int)Math.Floor((length - minS) / TrackShape.SampleStep) + 1;
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

            int entrance = (int)Math.Round(-minS / TrackShape.SampleStep);
            var walls = WallBuilder.Build(Extend(x, layout.Pitch), Extend(y, layout.Pitch), entrance, layout.Half, layout.Pitch,
                out List<float> arenaPoints);
            float[] bounds = Bounds(arenaPoints);
            float[] outline = ConvexHull(arenaPoints);
            return new TrackShape(id, lanes, layout.LaneWidth, WallWidth, BaseRadius, minS, x, y, walls, outline, bounds);
        }

        /// <summary>
        /// Continues the samples straight past the end so the side walls of the final approach run on until they
        /// meet the platform instead of stopping short where the path ends.
        /// </summary>
        private static float[] Extend(float[] values, float distance)
        {
            int extra = (int)Math.Ceiling(distance / TrackShape.SampleStep);
            var extended = new float[values.Length + extra];
            Array.Copy(values, extended, values.Length);
            float step = values[values.Length - 1] - values[values.Length - 2];
            for (int i = 0; i < extra; i++)
            {
                extended[values.Length + i] = values[values.Length - 1] + step * (i + 1);
            }
            return extended;
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

        private static float[] Bounds(List<float> points)
        {
            var bounds = new[] { float.MaxValue, float.MinValue, float.MaxValue, float.MinValue };
            for (int i = 0; i < points.Count; i += 2)
            {
                bounds[0] = Math.Min(bounds[0], points[i]);
                bounds[1] = Math.Max(bounds[1], points[i]);
                bounds[2] = Math.Min(bounds[2], points[i + 1]);
                bounds[3] = Math.Max(bounds[3], points[i + 1]);
            }
            return bounds;
        }

        /// <summary>Andrew's monotone chain over flattened (x, y) points; returns the hull counter-clockwise.</summary>
        private static float[] ConvexHull(List<float> flat)
        {
            int n = flat.Count / 2;
            var order = new int[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
            }
            Array.Sort(order, (a, b) =>
            {
                int c = flat[a * 2].CompareTo(flat[b * 2]);
                return c != 0 ? c : flat[a * 2 + 1].CompareTo(flat[b * 2 + 1]);
            });

            var hull = new int[n * 2];
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                while (k >= 2 && Cross(flat, hull[k - 2], hull[k - 1], order[i]) <= 0f)
                {
                    k--;
                }
                hull[k++] = order[i];
            }

            for (int i = n - 2, lower = k + 1; i >= 0; i--)
            {
                while (k >= lower && Cross(flat, hull[k - 2], hull[k - 1], order[i]) <= 0f)
                {
                    k--;
                }
                hull[k++] = order[i];
            }

            var result = new float[(k - 1) * 2];
            for (int i = 0; i < k - 1; i++)
            {
                result[i * 2] = flat[hull[i] * 2];
                result[i * 2 + 1] = flat[hull[i] * 2 + 1];
            }
            return result;
        }

        private static float Cross(List<float> flat, int o, int a, int b)
        {
            float ox = flat[o * 2];
            float oy = flat[o * 2 + 1];
            return (flat[a * 2] - ox) * (flat[b * 2 + 1] - oy) - (flat[a * 2 + 1] - oy) * (flat[b * 2] - ox);
        }

        /// <summary>Dense centerline built from lines and arcs, like a turtle (headings in radians, CCW positive).</summary>
        private sealed class PathBuilder
        {
            private const float Step = 0.1f;

            public readonly List<float> X = new List<float>(4096);
            public readonly List<float> Y = new List<float>(4096);

            public float Heading { get; private set; }

            private float LastX => X[X.Count - 1];
            private float LastY => Y[Y.Count - 1];

            public void Start(float x, float y, float heading)
            {
                X.Add(x);
                Y.Add(y);
                Heading = heading;
            }

            public void Add(float x, float y)
            {
                X.Add(x);
                Y.Add(y);
            }

            public void Line(float length)
            {
                if (length <= 0f)
                {
                    return;
                }

                float ox = LastX;
                float oy = LastY;
                float dx = (float)Math.Cos(Heading);
                float dy = (float)Math.Sin(Heading);
                int steps = Math.Max(1, (int)Math.Ceiling(length / Step));
                for (int i = 1; i <= steps; i++)
                {
                    float d = length * i / steps;
                    Add(ox + dx * d, oy + dy * d);
                }
            }

            /// <summary>Turns by <paramref name="angle"/> (positive = left) along a circle of <paramref name="radius"/>.</summary>
            public void Arc(float radius, float angle)
            {
                float side = angle > 0f ? 1f : -1f;
                float cx = LastX - (float)Math.Sin(Heading) * radius * side;
                float cy = LastY + (float)Math.Cos(Heading) * radius * side;
                float start = (float)Math.Atan2(LastY - cy, LastX - cx);
                int steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(angle) * radius / Step));
                for (int i = 1; i <= steps; i++)
                {
                    float a = start + angle * i / steps;
                    Add(cx + radius * (float)Math.Cos(a), cy + radius * (float)Math.Sin(a));
                }
                Heading += angle;
            }

            /// <summary>Removes float drift so exact heading comparisons (0 or π) work after half turns.</summary>
            public void SnapHeading()
            {
                float turns = Heading / Pi;
                float rounded = (float)Math.Round(turns);
                if (Math.Abs(turns - rounded) < 1e-3f)
                {
                    Heading = Math.Abs(rounded % 2f) < 0.5f ? 0f : Pi;
                }
            }

            /// <summary>
            /// Cubic curve from the current end into the base edge at <paramref name="contactAngle"/>, arriving
            /// head-on toward the base center.
            /// </summary>
            public void AppendCurl(float contactAngle, float contactRadius)
            {
                int last = X.Count - 1;
                float p0x = X[last];
                float p0y = Y[last];
                float tx = p0x - X[Math.Max(0, last - 4)];
                float ty = p0y - Y[Math.Max(0, last - 4)];
                TrackShape.Normalize(ref tx, ref ty);

                float nx = (float)Math.Cos(contactAngle);
                float ny = (float)Math.Sin(contactAngle);
                float cx = nx * contactRadius;
                float cy = ny * contactRadius;

                const float startHandle = 3.2f;
                const float endHandle = 4f;
                float p1x = p0x + tx * startHandle;
                float p1y = p0y + ty * startHandle;
                float p2x = cx + nx * endHandle;
                float p2y = cy + ny * endHandle;

                const int steps = 80;
                for (int i = 1; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    float u = 1f - t;
                    float b0 = u * u * u;
                    float b1 = 3f * u * u * t;
                    float b2 = 3f * u * t * t;
                    float b3 = t * t * t;
                    Add(b0 * p0x + b1 * p1x + b2 * p2x + b3 * cx, b0 * p0y + b1 * p1y + b2 * p2y + b3 * cy);
                }
                Heading = (float)Math.Atan2(-ny, -nx);
            }

            public void MirrorX()
            {
                for (int i = 0; i < X.Count; i++)
                {
                    X[i] = -X[i];
                }
                Heading = Pi - Heading;
            }
        }

        /// <summary>
        /// Walls for any layout: both edges of the path (one pitch apart), dropping points on the platform, inside
        /// another lane (tight inner curves, the approach cutting through a ring) or on top of a wall already laid
        /// by an earlier pass, so side-by-side passes share one wall.
        /// </summary>
        private static class WallBuilder
        {
            private const int Stride = 2;
            private const float InsideLaneTolerance = 0.3f;
            private const float DuplicateRadius = 0.8f;
            private const float MinSpacing = 0.25f;
            private const float GridCell = 2.5f;
            private const int MinRunPoints = 3;
            private const int RecentPoints = 4;

            public static List<float[]> Build(float[] x, float[] y, int entrance, float half, float pitch, out List<float> arenaPoints)
            {
                int first = Math.Max(1, entrance - (int)(VisibleLeadWall / TrackShape.SampleStep));
                int last = x.Length - 2;
                var centers = new PointGrid(GridCell);
                int centerFrom = Math.Max(0, first - (int)(pitch * 2f / TrackShape.SampleStep));
                for (int i = centerFrom; i < x.Length; i += Stride)
                {
                    centers.Add(x[i], y[i], -1, i);
                }

                var accepted = new PointGrid(GridCell);
                var walls = new List<float[]>();
                arenaPoints = new List<float>();
                var runs = new[] { new Run(), new Run() };
                int runId = 0;
                float minRadius = BaseRadius + 0.2f;
                float minClearance = half - InsideLaneTolerance;

                for (int i = first; i <= last; i += Stride)
                {
                    float tx = x[i + 1] - x[i - 1];
                    float ty = y[i + 1] - y[i - 1];
                    TrackShape.Normalize(ref tx, ref ty);

                    for (int side = 0; side < 2; side++)
                    {
                        Run run = runs[side];
                        float sign = side == 0 ? 1f : -1f;
                        float wx = x[i] - ty * half * sign;
                        float wy = y[i] + tx * half * sign;

                        // Only this run's last few points may sit nearby; anything else means the wall already exists.
                        int recentFrom = run.Points.Count > 0 ? i - RecentPoints * Stride : int.MaxValue;
                        bool valid = wx * wx + wy * wy >= minRadius * minRadius &&
                                     centers.NearestDistance(wx, wy, half * 2f) >= minClearance &&
                                     !accepted.HasBlocking(wx, wy, DuplicateRadius, run.Id, recentFrom);

                        if (!valid)
                        {
                            Flush(run, walls);
                            continue;
                        }

                        if (run.Points.Count == 0)
                        {
                            run.Id = runId++;
                        }
                        else
                        {
                            float dx = wx - run.LastX;
                            float dy = wy - run.LastY;
                            if (dx * dx + dy * dy < MinSpacing * MinSpacing)
                            {
                                continue;
                            }
                        }

                        run.Points.Add(wx);
                        run.Points.Add(wy);
                        run.LastIndex = i;
                        accepted.Add(wx, wy, run.Id, i);
                        if (i >= entrance)
                        {
                            AddThick(arenaPoints, wx, wy, WallWidth * 0.5f);
                        }
                    }
                }

                Flush(runs[0], walls);
                Flush(runs[1], walls);

                const int baseSteps = 24;
                for (int k = 0; k < baseSteps; k++)
                {
                    float a = TwoPi * k / baseSteps;
                    arenaPoints.Add((float)Math.Cos(a) * BaseRadius);
                    arenaPoints.Add((float)Math.Sin(a) * BaseRadius);
                }
                return walls;
            }

            private static void AddThick(List<float> points, float x, float y, float radius)
            {
                points.Add(x - radius);
                points.Add(y);
                points.Add(x + radius);
                points.Add(y);
                points.Add(x);
                points.Add(y - radius);
                points.Add(x);
                points.Add(y + radius);
            }

            private static void Flush(Run run, List<float[]> walls)
            {
                if (run.Points.Count >= MinRunPoints * 2)
                {
                    walls.Add(run.Points.ToArray());
                }
                run.Points.Clear();
            }

            private sealed class Run
            {
                public readonly List<float> Points = new List<float>(256);
                public int Id;
                public int LastIndex;

                public float LastX => Points[Points.Count - 2];
                public float LastY => Points[Points.Count - 1];
            }
        }

        /// <summary>Uniform grid of tagged points for radius queries.</summary>
        private sealed class PointGrid
        {
            private readonly float _cell;
            private readonly Dictionary<long, List<Entry>> _cells = new Dictionary<long, List<Entry>>();

            public PointGrid(float cell)
            {
                _cell = cell;
            }

            private struct Entry
            {
                public float X;
                public float Y;
                public int Owner;
                public int Index;
            }

            public void Add(float x, float y, int owner, int index)
            {
                long key = Key(Cell(x), Cell(y));
                if (!_cells.TryGetValue(key, out List<Entry> list))
                {
                    list = new List<Entry>();
                    _cells[key] = list;
                }
                list.Add(new Entry { X = x, Y = y, Owner = owner, Index = index });
            }

            /// <summary>Distance to the nearest point, capped at <paramref name="limit"/>.</summary>
            public float NearestDistance(float x, float y, float limit)
            {
                float best = limit * limit;
                int reach = (int)Math.Ceiling(limit / _cell);
                int cx = Cell(x);
                int cy = Cell(y);
                for (int gx = cx - reach; gx <= cx + reach; gx++)
                {
                    for (int gy = cy - reach; gy <= cy + reach; gy++)
                    {
                        if (!_cells.TryGetValue(Key(gx, gy), out List<Entry> list))
                        {
                            continue;
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            float dx = list[i].X - x;
                            float dy = list[i].Y - y;
                            best = Math.Min(best, dx * dx + dy * dy);
                        }
                    }
                }
                return (float)Math.Sqrt(best);
            }

            /// <summary>
            /// True if a point within <paramref name="radius"/> belongs to another owner, or to <paramref name="owner"/>
            /// but was added before <paramref name="recentFrom"/>.
            /// </summary>
            public bool HasBlocking(float x, float y, float radius, int owner, int recentFrom)
            {
                float limit = radius * radius;
                int cx = Cell(x);
                int cy = Cell(y);
                for (int gx = cx - 1; gx <= cx + 1; gx++)
                {
                    for (int gy = cy - 1; gy <= cy + 1; gy++)
                    {
                        if (!_cells.TryGetValue(Key(gx, gy), out List<Entry> list))
                        {
                            continue;
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            Entry e = list[i];
                            float dx = e.X - x;
                            float dy = e.Y - y;
                            if (dx * dx + dy * dy <= limit && (e.Owner != owner || e.Index < recentFrom))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            private int Cell(float value)
            {
                return (int)Math.Floor(value / _cell);
            }

            private static long Key(int x, int y)
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }
    }
}
