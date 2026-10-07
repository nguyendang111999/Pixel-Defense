using UnityEngine;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    /// <summary>
    /// Chunky cartoon icons in a 100x100 design space. Every icon is drawn twice: first all shapes stroked in
    /// ink (the outline), then all shapes filled, so outlines between touching parts disappear.
    /// </summary>
    internal static class IconPainter
    {
        private static readonly Color Ink = new Color(0.12f, 0.13f, 0.2f);
        private static readonly Color Gold = new Color(1f, 0.76f, 0.12f);
        private static readonly Color GoldLight = new Color(1f, 0.88f, 0.4f);

        private const float Outline = 9f;

        public static void Draw(Painter2D painter, IconKind kind, Vector2 origin, float scale, Color tint)
        {
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            for (int pass = 0; pass < 2; pass++)
            {
                var c = new Canvas(painter, origin, scale, pass);
                switch (kind)
                {
                    case IconKind.Gear: Gear(c, tint); break;
                    case IconKind.Close: Close(c, tint); break;
                    case IconKind.Snowflake: Snowflake(c, tint); break;
                    case IconKind.Bomb: Bomb(c); break;
                    case IconKind.Plus: Plus(c, tint); break;
                    case IconKind.Coin: Coin(c); break;
                    case IconKind.Hand: Hand(c, tint); break;
                    case IconKind.Play: Play(c, tint); break;
                    case IconKind.Retry: Retry(c, tint); break;
                    case IconKind.Home: Home(c, tint); break;
                    case IconKind.Sound: Sound(c, tint); break;
                    case IconKind.Music: Music(c, tint); break;
                    case IconKind.Vibrate: Vibrate(c, tint); break;
                    case IconKind.Dragon: Dragon(c, tint); break;
                    case IconKind.Star: Star(c, Gold); break;
                    case IconKind.Pick: Pick(c, tint); break;
                    case IconKind.Bot: Bot(c, tint); break;
                }
            }
        }

        private static void Gear(Canvas c, Color tint)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                c.Begin();
                c.RotatedRect(50f, 50f, a, -8f, 24f, 8f, 45f);
                c.Fill(tint);
            }
            c.Circle(50f, 50f, 31f, tint);
            c.Circle(50f, 50f, 12f, Ink);
        }

        private static void Close(Canvas c, Color tint)
        {
            c.Line(28f, 28f, 72f, 72f, 15f, tint);
            c.Line(72f, 28f, 28f, 72f, 15f, tint);
        }

        private static void Plus(Canvas c, Color tint)
        {
            c.Line(50f, 22f, 50f, 78f, 17f, tint);
            c.Line(22f, 50f, 78f, 50f, 17f, tint);
        }

        private static void Snowflake(Canvas c, Color tint)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI / 3f + Mathf.PI / 2f;
                float dx = Mathf.Cos(a);
                float dy = Mathf.Sin(a);
                c.Line(50f - dx * 38f, 50f - dy * 38f, 50f + dx * 38f, 50f + dy * 38f, 9f, tint);
                for (int end = -1; end <= 1; end += 2)
                {
                    float bx = 50f + dx * 24f * end;
                    float by = 50f + dy * 24f * end;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float ba = a + side * 0.75f;
                        float tx = Mathf.Cos(ba) * 12f * end;
                        float ty = Mathf.Sin(ba) * 12f * end;
                        c.Line(bx, by, bx + tx, by + ty, 7f, tint);
                    }
                }
            }
        }

        private static void Bomb(Canvas c)
        {
            var body = new Color(0.2f, 0.21f, 0.32f);
            c.Begin();
            c.Bezier(66f, 28f, 74f, 12f, 84f, 20f, 88f, 10f);
            c.StrokeOnly(7f, new Color(0.85f, 0.7f, 0.45f));
            c.Begin();
            c.RotatedRect(66f, 30f, Mathf.PI * 0.25f, -9f, -9f, 9f, 9f);
            c.Fill(new Color(0.55f, 0.57f, 0.68f));
            c.Circle(46f, 58f, 31f, body);
            c.Circle(36f, 47f, 8f, new Color(1f, 1f, 1f, 0.55f));
            Star(c, new Color(1f, 0.82f, 0.25f), 88f, 11f, 11f, 5f);
        }

        private static void Coin(Canvas c)
        {
            c.Circle(50f, 50f, 40f, Gold);
            c.Circle(50f, 50f, 29f, GoldLight);
            if (c.Pass == 1)
            {
                Star(c, Gold, 50f, 52f, 19f, 8.5f);
            }
        }

        private static void Hand(Canvas c, Color tint)
        {
            c.RoundRect(41f, 6f, 18f, 54f, 9f, tint);
            c.RoundRect(28f, 44f, 48f, 42f, 15f, tint);
            c.Begin();
            c.RotatedRect(28f, 60f, -0.6f, -7f, -16f, 7f, 10f);
            c.Fill(tint);
            c.RoundRect(33f, 82f, 38f, 13f, 4f, new Color(0.3f, 0.55f, 1f));
            if (c.Pass == 1)
            {
                c.Line(45f, 50f, 45f, 60f, 3f, Ink, false);
                c.Line(56f, 50f, 56f, 60f, 3f, Ink, false);
                c.Line(66f, 52f, 66f, 60f, 3f, Ink, false);
            }
        }

        private static void Play(Canvas c, Color tint)
        {
            c.Begin();
            c.Move(34f, 20f);
            c.To(82f, 50f);
            c.To(34f, 80f);
            c.Close();
            c.Fill(tint);
        }

        private static void Retry(Canvas c, Color tint)
        {
            c.Begin();
            c.ArcPath(50f, 52f, 27f, -50f, 220f);
            c.StrokeOnly(13f, tint);
            float a = -50f * Mathf.Deg2Rad;
            float ex = 50f + Mathf.Cos(a) * 27f;
            float ey = 52f + Mathf.Sin(a) * 27f;
            c.Begin();
            c.Move(ex - 15f, ey - 12f);
            c.To(ex + 14f, ey - 6f);
            c.To(ex + 2f, ey + 18f);
            c.Close();
            c.Fill(tint);
        }

        private static void Home(Canvas c, Color tint)
        {
            c.Begin();
            c.Move(50f, 14f);
            c.To(90f, 50f);
            c.To(10f, 50f);
            c.Close();
            c.Fill(tint);
            c.Begin();
            c.Rect(22f, 44f, 56f, 42f);
            c.Fill(tint);
            c.Begin();
            c.Rect(42f, 60f, 16f, 26f);
            c.Fill(Ink);
        }

        private static void Sound(Canvas c, Color tint)
        {
            c.Begin();
            c.Move(14f, 38f);
            c.To(32f, 38f);
            c.To(52f, 20f);
            c.To(52f, 80f);
            c.To(32f, 62f);
            c.To(14f, 62f);
            c.Close();
            c.Fill(tint);
            c.Begin();
            c.ArcPath(54f, 50f, 17f, -45f, 45f);
            c.StrokeOnly(8f, tint);
            c.Begin();
            c.ArcPath(54f, 50f, 31f, -50f, 50f);
            c.StrokeOnly(8f, tint);
        }

        private static void Music(Canvas c, Color tint)
        {
            c.Line(42f, 72f, 42f, 24f, 8f, tint);
            c.Line(78f, 64f, 78f, 16f, 8f, tint);
            c.Begin();
            c.Move(42f, 22f);
            c.To(78f, 12f);
            c.To(78f, 28f);
            c.To(42f, 38f);
            c.Close();
            c.Fill(tint);
            c.Circle(32f, 74f, 12f, tint);
            c.Circle(68f, 66f, 12f, tint);
        }

        private static void Vibrate(Canvas c, Color tint)
        {
            c.RoundRect(32f, 14f, 36f, 72f, 8f, tint);
            if (c.Pass == 1)
            {
                c.RoundRect(38f, 22f, 24f, 50f, 3f, new Color(0.35f, 0.4f, 0.6f));
            }
            c.Line(20f, 36f, 20f, 64f, 7f, tint);
            c.Line(80f, 36f, 80f, 64f, 7f, tint);
            c.Line(9f, 42f, 9f, 58f, 7f, tint);
            c.Line(91f, 42f, 91f, 58f, 7f, tint);
        }

        private static void Dragon(Canvas c, Color tint)
        {
            var horn = new Color(1f, 0.95f, 0.85f);
            c.Begin();
            c.Move(24f, 38f);
            c.To(14f, 8f);
            c.To(42f, 30f);
            c.Close();
            c.Fill(horn);
            c.Begin();
            c.Move(76f, 38f);
            c.To(86f, 8f);
            c.To(58f, 30f);
            c.Close();
            c.Fill(horn);
            c.RoundRect(16f, 26f, 68f, 62f, 22f, tint);
            if (c.Pass == 1)
            {
                c.Circle(36f, 52f, 10f, Color.white);
                c.Circle(64f, 52f, 10f, Color.white);
                c.Circle(38f, 54f, 5f, Ink);
                c.Circle(62f, 54f, 5f, Ink);
                c.Line(26f, 38f, 44f, 44f, 5f, Ink, false);
                c.Line(74f, 38f, 56f, 44f, 5f, Ink, false);
                c.Circle(44f, 74f, 3.5f, Ink);
                c.Circle(56f, 74f, 3.5f, Ink);
            }
        }

        /// <summary>Arcade claw lifting a cannon block: "take any one".</summary>
        private static void Pick(Canvas c, Color tint)
        {
            c.Line(50f, 6f, 50f, 28f, 8f, tint);
            c.RoundRect(37f, 60f, 26f, 26f, 7f, new Color(0.95f, 0.33f, 0.27f));
            c.RoundRect(31f, 24f, 38f, 18f, 8f, tint);
            c.Begin();
            c.Bezier(38f, 38f, 20f, 50f, 20f, 72f, 34f, 88f);
            c.StrokeOnly(9f, tint);
            c.Begin();
            c.Bezier(62f, 38f, 80f, 50f, 80f, 72f, 66f, 88f);
            c.StrokeOnly(9f, tint);
        }

        /// <summary>Little robot head for the autoplay bot.</summary>
        private static void Bot(Canvas c, Color tint)
        {
            c.Line(50f, 10f, 50f, 26f, 6f, tint);
            c.Circle(50f, 10f, 7f, tint);
            c.RoundRect(18f, 26f, 64f, 52f, 16f, tint);
            c.RoundRect(10f, 42f, 10f, 20f, 4f, tint);
            c.RoundRect(80f, 42f, 10f, 20f, 4f, tint);
            if (c.Pass == 1)
            {
                c.Circle(37f, 50f, 8f, Ink);
                c.Circle(63f, 50f, 8f, Ink);
                c.Line(38f, 66f, 62f, 66f, 5f, Ink, false);
            }
        }

        private static void Star(Canvas c, Color color)
        {
            Star(c, color, 50f, 52f, 44f, 20f);
        }

        private static void Star(Canvas c, Color color, float cx, float cy, float outer, float inner)
        {
            c.Begin();
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                float x = cx + Mathf.Cos(a) * r;
                float y = cy + Mathf.Sin(a) * r;
                if (i == 0)
                {
                    c.Move(x, y);
                }
                else
                {
                    c.To(x, y);
                }
            }
            c.Close();
            c.Fill(color);
        }

        /// <summary>Design-space drawing helpers bound to one pass (0 = ink outline, 1 = color).</summary>
        private readonly struct Canvas
        {
            private readonly Painter2D _p;
            private readonly Vector2 _o;
            private readonly float _s;
            public readonly int Pass;

            public Canvas(Painter2D painter, Vector2 origin, float scale, int pass)
            {
                _p = painter;
                _o = origin;
                _s = scale;
                Pass = pass;
            }

            private Vector2 P(float x, float y)
            {
                return _o + new Vector2(x, y) * _s;
            }

            public void Begin()
            {
                _p.BeginPath();
            }

            public void Move(float x, float y)
            {
                _p.MoveTo(P(x, y));
            }

            public void To(float x, float y)
            {
                _p.LineTo(P(x, y));
            }

            public void Close()
            {
                _p.ClosePath();
            }

            public void Bezier(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
            {
                _p.MoveTo(P(x0, y0));
                _p.BezierCurveTo(P(x1, y1), P(x2, y2), P(x3, y3));
            }

            public void ArcPath(float cx, float cy, float r, float fromDeg, float toDeg)
            {
                _p.Arc(P(cx, cy), r * _s, Angle.Degrees(fromDeg), Angle.Degrees(toDeg), ArcDirection.Clockwise);
            }

            public void Rect(float x, float y, float w, float h)
            {
                Move(x, y);
                To(x + w, y);
                To(x + w, y + h);
                To(x, y + h);
                Close();
            }

            /// <summary>Rectangle in a rotated frame: t across, r along the angle's direction.</summary>
            public void RotatedRect(float cx, float cy, float angle, float t0, float r0, float t1, float r1)
            {
                float ca = Mathf.Cos(angle);
                float sa = Mathf.Sin(angle);
                Vector2 Corner(float t, float r) => new Vector2(cx + ca * r - sa * t, cy + sa * r + ca * t);
                Vector2 a = Corner(t0, r0);
                Vector2 b = Corner(t0, r1);
                Vector2 d = Corner(t1, r1);
                Vector2 e = Corner(t1, r0);
                Move(a.x, a.y);
                To(b.x, b.y);
                To(d.x, d.y);
                To(e.x, e.y);
                Close();
            }

            public void Circle(float cx, float cy, float r, Color color)
            {
                Begin();
                _p.Arc(P(cx, cy), r * _s, Angle.Degrees(0f), Angle.Degrees(360f), ArcDirection.Clockwise);
                Close();
                Fill(color);
            }

            public void RoundRect(float x, float y, float w, float h, float r, Color color)
            {
                Begin();
                Move(x + r, y);
                To(x + w - r, y);
                _p.ArcTo(P(x + w, y), P(x + w, y + r), r * _s);
                To(x + w, y + h - r);
                _p.ArcTo(P(x + w, y + h), P(x + w - r, y + h), r * _s);
                To(x + r, y + h);
                _p.ArcTo(P(x, y + h), P(x, y + h - r), r * _s);
                To(x, y + r);
                _p.ArcTo(P(x, y), P(x + r, y), r * _s);
                Close();
                Fill(color);
            }

            public void Line(float x0, float y0, float x1, float y1, float width, Color color, bool outlined = true)
            {
                if (Pass == 0 && !outlined)
                {
                    return;
                }

                Begin();
                Move(x0, y0);
                To(x1, y1);
                StrokeOnly(width, color);
            }

            /// <summary>Strokes the current path: ink and wider in pass 0, the color in pass 1.</summary>
            public void StrokeOnly(float width, Color color)
            {
                _p.strokeColor = Pass == 0 ? Ink : color;
                _p.lineWidth = (Pass == 0 ? width + Outline : width) * _s;
                _p.Stroke();
            }

            /// <summary>Pass 0 strokes the current path in ink, pass 1 fills it.</summary>
            public void Fill(Color color)
            {
                if (Pass == 0)
                {
                    _p.strokeColor = Ink;
                    _p.lineWidth = Outline * _s;
                    _p.Stroke();
                }
                else
                {
                    _p.fillColor = color;
                    _p.Fill();
                }
            }
        }
    }
}
