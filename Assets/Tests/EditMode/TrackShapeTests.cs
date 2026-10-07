using System;
using System.Collections.Generic;
using NUnit.Framework;
using PixelDefense.Core;

namespace PixelDefense.Tests.EditMode
{
    public class TrackShapeTests
    {
        private const int Lanes = 3;

        // Arena width (slice units) above which the camera would have to zoom out and shrink the base.
        private const float MaxArenaWidth = 52f;

        private static IEnumerable<string> Presets()
        {
            return TrackPresets.Ids;
        }

        [TestCaseSource(nameof(Presets))]
        public void Preset_EndsAtBaseEdgeHeadingInward(string id)
        {
            TrackShape track = TrackPresets.Create(id, Lanes);

            track.Sample(track.EndS, out float x, out float y, out float tx, out float ty);
            float radius = (float)Math.Sqrt(x * x + y * y);
            float inward = -(x * tx + y * ty) / radius;

            Assert.That(radius, Is.EqualTo(TrackPresets.BaseRadius + TrackPresets.ContactGap).Within(0.3f));
            Assert.That(inward, Is.GreaterThan(0.85f), "The head should arrive facing the base.");
            Assert.That(track.EndS, Is.GreaterThan(150f));
        }

        [TestCaseSource(nameof(Presets))]
        public void Preset_SamplesContinuouslyAtUniformSpeed(string id)
        {
            TrackShape track = TrackPresets.Create(id, Lanes);

            track.Sample(track.MinS, out float px, out float py, out _, out _);
            for (float s = track.MinS + 0.5f; s <= track.EndS; s += 0.5f)
            {
                track.Sample(s, out float x, out float y, out float tx, out float ty);
                float step = (float)Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                Assert.That(step, Is.EqualTo(0.5f).Within(0.08f), "Uniform arc length at s=" + s);
                Assert.That(tx * tx + ty * ty, Is.EqualTo(1f).Within(1e-3f));
                px = x;
                py = y;
            }
        }

        [TestCaseSource(nameof(Presets))]
        public void Preset_PassesNeverCrowdEachOther(string id)
        {
            TrackShape track = TrackPresets.Create(id, Lanes);
            var points = new List<float[]>();
            for (float s = -TrackPresets.VisibleLeadWall; s <= track.EndS; s += 0.5f)
            {
                track.Sample(s, out float x, out float y, out _, out _);
                points.Add(new[] { x, y, s });
            }

            // Points far apart along the path must be at least one lane plus a wall apart.
            float closest = float.MaxValue;
            float atA = 0f;
            float atB = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (points[j][2] - points[i][2] < 8.5f)
                    {
                        continue;
                    }

                    float dx = points[i][0] - points[j][0];
                    float dy = points[i][1] - points[j][1];
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance < closest)
                    {
                        closest = distance;
                        atA = points[i][2];
                        atB = points[j][2];
                    }
                }
            }

            Assert.That(closest, Is.GreaterThanOrEqualTo(track.Pitch - 0.1f), "Passes at s=" + atA + " and s=" + atB + " overlap");
        }

        [TestCaseSource(nameof(Presets))]
        public void Preset_KeepsOffThePlatformUntilTheFinalApproach(string id)
        {
            TrackShape track = TrackPresets.Create(id, Lanes);
            float minRadius = TrackPresets.BaseRadius + track.LaneWidth * 0.5f;

            for (float s = track.MinS; s <= track.EndS - 12f; s += 0.5f)
            {
                track.Sample(s, out float x, out float y, out _, out _);
                Assert.That((float)Math.Sqrt(x * x + y * y), Is.GreaterThanOrEqualTo(minRadius), "Lane crosses the base at s=" + s);
            }
        }

        [TestCaseSource(nameof(Presets))]
        public void Preset_HasWallsAndFitsTheScreen(string id)
        {
            TrackShape track = TrackPresets.Create(id, Lanes);

            Assert.That(track.Walls.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(track.Outline.Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(track.ArenaMinX, Is.LessThan(-TrackPresets.BaseRadius));
            Assert.That(track.ArenaMaxX, Is.GreaterThan(TrackPresets.BaseRadius));
            Assert.That(track.ArenaMinY, Is.LessThanOrEqualTo(-TrackPresets.BaseRadius + 0.01f));
            if (id != "spiral_long" && id != "spiral_long_cw")
            {
                Assert.That(track.ArenaMaxX - track.ArenaMinX, Is.LessThanOrEqualTo(MaxArenaWidth));
            }

            foreach (float[] wall in track.Walls)
            {
                Assert.That(wall.Length % 2, Is.EqualTo(0));
                for (int i = 2; i < wall.Length; i += 2)
                {
                    float dx = wall[i] - wall[i - 2];
                    float dy = wall[i + 1] - wall[i - 1];
                    Assert.That(dx * dx + dy * dy, Is.GreaterThan(1e-4f), "Wall polylines need distinct points for their tangents.");
                }
            }
        }

        [Test]
        public void MirroredPreset_IsReflectionOfItsTwin()
        {
            TrackShape snake = TrackPresets.Create("snake", Lanes);
            TrackShape mirrored = TrackPresets.Create("snake_cw", Lanes);

            Assert.That(mirrored.EndS, Is.EqualTo(snake.EndS).Within(0.01f));
            for (float s = 0f; s <= snake.EndS; s += 7f)
            {
                snake.Sample(s, out float x, out float y, out _, out _);
                mirrored.Sample(s, out float mx, out float my, out _, out _);
                Assert.That(mx, Is.EqualTo(-x).Within(0.01f));
                Assert.That(my, Is.EqualTo(y).Within(0.01f));
            }
        }

        [Test]
        public void UnknownPreset_FallsBackToSpiral()
        {
            Assert.That(TrackPresets.Exists("no_such_track"), Is.False);
            TrackShape fallback = TrackPresets.Create("no_such_track", Lanes);
            TrackShape spiral = TrackPresets.Create("spiral", Lanes);

            Assert.That(fallback.EndS, Is.EqualTo(spiral.EndS).Within(0.01f));
        }

        [Test]
        public void Sample_BeforeLeadIn_ExtrapolatesStraight()
        {
            TrackShape track = TrackPresets.Create("spiral", Lanes);

            track.Sample(track.MinS - 10f, out float x, out float y, out _, out _);
            track.Sample(track.MinS, out float x0, out float y0, out _, out _);

            float distance = (float)Math.Sqrt((x - x0) * (x - x0) + (y - y0) * (y - y0));
            Assert.That(distance, Is.EqualTo(10f).Within(0.01f));
        }
    }
}
