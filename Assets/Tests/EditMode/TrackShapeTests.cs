using System;
using NUnit.Framework;
using PixelDefense.Core;

namespace PixelDefense.Tests.EditMode
{
    public class TrackShapeTests
    {
        [TestCase("spiral")]
        [TestCase("spiral_cw")]
        [TestCase("spiral_long")]
        [TestCase("square")]
        [TestCase("square_cw")]
        public void Preset_EndsAtBaseEdgeAndSamplesContinuously(string id)
        {
            TrackShape track = TrackPresets.Create(id, 3);

            track.Sample(track.EndS, out float ex, out float ey, out _, out _);
            float endRadius = (float)Math.Sqrt(ex * ex + ey * ey);
            Assert.That(endRadius, Is.EqualTo(TrackPresets.BaseRadius).Within(0.5f));
            Assert.That(track.EndS, Is.GreaterThan(100f));
            Assert.That(track.Walls.Count, Is.GreaterThanOrEqualTo(3));

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

        [Test]
        public void Sample_BeforeLeadIn_ExtrapolatesStraight()
        {
            TrackShape track = TrackPresets.Create("spiral", 3);

            track.Sample(track.MinS - 10f, out float x, out float y, out _, out _);
            track.Sample(track.MinS, out float x0, out float y0, out _, out _);

            float distance = (float)Math.Sqrt((x - x0) * (x - x0) + (y - y0) * (y - y0));
            Assert.That(distance, Is.EqualTo(10f).Within(0.01f));
        }
    }
}
