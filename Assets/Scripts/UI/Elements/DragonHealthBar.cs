using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    /// <summary>
    /// The dragon's remaining body as colored segments, head first (left). Shows the color order the player
    /// must feed in, and shrinks from the left as the neck is destroyed.
    /// </summary>
    [UxmlElement]
    public partial class DragonHealthBar : VisualElement
    {
        private static readonly Color Track = new Color(0.08f, 0.09f, 0.16f, 0.75f);
        private static readonly Color Ink = new Color(0.12f, 0.13f, 0.2f);

        private readonly List<Color> _colors = new List<Color>();
        private readonly List<float> _weights = new List<float>();
        private float _total = 1f;
        private float _shake;

        public DragonHealthBar()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerate;
        }

        /// <param name="total">Scale count at full health; remaining segments are drawn relative to it.</param>
        public void SetSegments(IReadOnlyList<Color> colors, IReadOnlyList<int> counts, int total)
        {
            _colors.Clear();
            _weights.Clear();
            for (int i = 0; i < colors.Count; i++)
            {
                _colors.Add(colors[i]);
                _weights.Add(counts[i]);
            }
            _total = Mathf.Max(1, total);
            MarkDirtyRepaint();
        }

        public void Shake()
        {
            _shake = 1f;
            MarkDirtyRepaint();
        }

        public void Tick(float deltaTime)
        {
            if (_shake > 0f)
            {
                _shake = Mathf.Max(0f, _shake - deltaTime * 4f);
                MarkDirtyRepaint();
            }
        }

        private void OnGenerate(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width < 4f || rect.height < 4f)
            {
                return;
            }

            Painter2D p = context.painter2D;
            float h = rect.height;
            float radius = h * 0.5f;
            float jitter = _shake > 0f ? Mathf.Sin(_shake * 40f) * _shake * 4f : 0f;
            float x0 = rect.x + jitter;
            float y0 = rect.y;

            Fill(p, x0, y0, rect.width, h, radius, Track);
            p.strokeColor = Ink;
            p.lineWidth = 4f;
            Trace(p, x0, y0, rect.width, h, radius);
            p.Stroke();

            float inset = 6f;
            float innerW = rect.width - inset * 2f;
            float innerH = h - inset * 2f;
            float remaining = 0f;
            for (int i = 0; i < _weights.Count; i++)
            {
                remaining += _weights[i];
            }

            // Right-aligned: the bar empties from the head (left) like the dragon does.
            float used = innerW * Mathf.Clamp01(remaining / _total);
            float x = x0 + inset + innerW - used;
            float gap = 3f;
            for (int i = 0; i < _weights.Count; i++)
            {
                float w = innerW * (_weights[i] / _total);
                if (w <= 0.5f)
                {
                    continue;
                }

                float segment = Mathf.Max(1f, w - gap);
                float r = Mathf.Min(innerH * 0.5f, segment * 0.5f);
                Color color = _colors[i];
                Fill(p, x, y0 + inset, segment, innerH, r, color);
                Color shine = Color.Lerp(color, Color.white, 0.45f);
                shine.a = 0.8f;
                float shineH = innerH * 0.32f;
                Fill(p, x + r * 0.5f, y0 + inset + innerH * 0.14f, Mathf.Max(1f, segment - r), shineH, Mathf.Min(shineH * 0.5f, segment * 0.5f), shine);
                x += w;
            }
        }

        private static void Fill(Painter2D p, float x, float y, float w, float h, float r, Color color)
        {
            p.fillColor = color;
            Trace(p, x, y, w, h, r);
            p.Fill();
        }

        private static void Trace(Painter2D p, float x, float y, float w, float h, float r)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
            p.BeginPath();
            p.MoveTo(new Vector2(x + r, y));
            p.LineTo(new Vector2(x + w - r, y));
            p.ArcTo(new Vector2(x + w, y), new Vector2(x + w, y + r), r);
            p.LineTo(new Vector2(x + w, y + h - r));
            p.ArcTo(new Vector2(x + w, y + h), new Vector2(x + w - r, y + h), r);
            p.LineTo(new Vector2(x + r, y + h));
            p.ArcTo(new Vector2(x, y + h), new Vector2(x, y + h - r), r);
            p.LineTo(new Vector2(x, y + r));
            p.ArcTo(new Vector2(x, y), new Vector2(x + r, y), r);
            p.ClosePath();
        }
    }
}
