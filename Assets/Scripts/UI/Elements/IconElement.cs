using UnityEngine;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    public enum IconKind
    {
        None,
        Gear,
        Close,
        Snowflake,
        Bomb,
        Plus,
        Coin,
        Hand,
        Play,
        Retry,
        Home,
        Sound,
        Music,
        Vibrate,
        Dragon,
        Star
    }

    /// <summary>Resolution-independent game icons drawn with Painter2D (no texture imports needed).</summary>
    [UxmlElement]
    public partial class IconElement : VisualElement
    {
        private IconKind _kind;
        private Color _tint = Color.white;

        [UxmlAttribute]
        public IconKind Kind
        {
            get => _kind;
            set
            {
                _kind = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public Color Tint
        {
            get => _tint;
            set
            {
                _tint = value;
                MarkDirtyRepaint();
            }
        }

        public IconElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerate;
        }

        private void OnGenerate(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width < 1f || rect.height < 1f || _kind == IconKind.None)
            {
                return;
            }

            float size = Mathf.Min(rect.width, rect.height);
            var origin = new Vector2(rect.x + (rect.width - size) * 0.5f, rect.y + (rect.height - size) * 0.5f);
            IconPainter.Draw(context.painter2D, _kind, origin, size / 100f, _tint);
        }
    }
}
