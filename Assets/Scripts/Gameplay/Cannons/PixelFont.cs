using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Runtime-generated chunky pixel digits (white glyphs with a dark outline) and an allocation-free mesh writer
    /// for short numbers. Glyphs: 0-9, '?' and 'x'.
    /// </summary>
    public static class PixelFont
    {
        public const int CellWidth = 12;
        public const int CellHeight = 16;
        public const int Advance = 10;
        private const int GlyphCount = 12;
        private const int AtlasWidth = 256;
        private const int AtlasHeight = 16;

        private static readonly string[] Glyphs =
        {
            "01110100011001110101110011000101110", // 0
            "00100011000010000100001000010001110", // 1
            "01110100010000100010001000100011111", // 2
            "11110000010000101110000010000111110", // 3
            "00010001100101010010111110001000010", // 4
            "11111100001111000001000011000101110", // 5
            "00110010001000011110100011000101110", // 6
            "11111000010001000100010000100001000", // 7
            "01110100011000101110100011000101110", // 8
            "01110100011000101111000010001001100", // 9
            "01110100010000100010001000000000100", // ?
            "00000000001000101010001000101010001", // x
        };

        /// <param name="readable">Keep a CPU copy (needed to encode the atlas to PNG in the Editor).</param>
        public static Texture2D CreateAtlas(bool readable = false)
        {
            var texture = new Texture2D(AtlasWidth, AtlasHeight, TextureFormat.RGBA32, false, false)
            {
                name = "PixelDigitsAtlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[AtlasWidth * AtlasHeight];
            var fill = new bool[AtlasWidth * AtlasHeight];
            for (int g = 0; g < GlyphCount; g++)
            {
                string bits = Glyphs[g];
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if (bits[row * 5 + col] != '1')
                        {
                            continue;
                        }

                        // Each glyph pixel becomes 2x2 texels; rows are stored top-down, textures bottom-up.
                        int baseX = g * CellWidth + 1 + col * 2;
                        int baseY = 1 + (6 - row) * 2;
                        for (int dx = 0; dx < 2; dx++)
                        {
                            for (int dy = 0; dy < 2; dy++)
                            {
                                fill[(baseY + dy) * AtlasWidth + baseX + dx] = true;
                            }
                        }
                    }
                }
            }

            var outline = new Color32(30, 30, 52, 255);
            var glyph = new Color32(255, 255, 255, 255);
            for (int y = 0; y < AtlasHeight; y++)
            {
                for (int x = 0; x < AtlasWidth; x++)
                {
                    int i = y * AtlasWidth + x;
                    if (fill[i])
                    {
                        pixels[i] = glyph;
                        continue;
                    }

                    bool edge = false;
                    for (int dy = -1; dy <= 1 && !edge; dy++)
                    {
                        for (int dx = -1; dx <= 1 && !edge; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            edge = nx >= 0 && ny >= 0 && nx < AtlasWidth && ny < AtlasHeight && fill[ny * AtlasWidth + nx];
                        }
                    }
                    pixels[i] = edge ? outline : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, !readable);
            return texture;
        }

        /// <summary>Reusable mesh for up to 4 glyphs, centered at the origin in the XY plane.</summary>
        public sealed class Label
        {
            private const int MaxGlyphs = 4;
            private readonly Vector3[] _vertices = new Vector3[MaxGlyphs * 4];
            private readonly Vector2[] _uvs = new Vector2[MaxGlyphs * 4];
            private readonly int[] _indices = new int[MaxGlyphs * 6];
            private readonly char[] _chars = new char[MaxGlyphs];

            public Label()
            {
                Mesh = new Mesh { name = "PixelLabel" };
                Mesh.MarkDynamic();
            }

            public Mesh Mesh { get; }

            /// <param name="height">World height of a glyph cell.</param>
            public void SetNumber(int value, float height)
            {
                int count = 0;
                if (value < 0)
                {
                    _chars[count++] = '?';
                }
                else
                {
                    int digits = value >= 1000 ? 4 : value >= 100 ? 3 : value >= 10 ? 2 : 1;
                    for (int i = digits - 1; i >= 0; i--)
                    {
                        _chars[i] = (char)('0' + value % 10);
                        value /= 10;
                    }
                    count = digits;
                }
                Write(count, height);
            }

            public void SetText(string text, float height)
            {
                int count = Mathf.Min(text.Length, MaxGlyphs);
                for (int i = 0; i < count; i++)
                {
                    _chars[i] = text[i];
                }
                Write(count, height);
            }

            private void Write(int count, float height)
            {
                float scale = height / CellHeight;
                float totalWidth = ((count - 1) * Advance + CellWidth) * scale;
                float x = -totalWidth * 0.5f;
                float y = -height * 0.5f;
                for (int i = 0; i < MaxGlyphs; i++)
                {
                    int v = i * 4;
                    if (i >= count)
                    {
                        _vertices[v] = _vertices[v + 1] = _vertices[v + 2] = _vertices[v + 3] = Vector3.zero;
                        continue;
                    }

                    int glyph = GlyphIndex(_chars[i]);
                    float left = x + i * Advance * scale;
                    float right = left + CellWidth * scale;
                    _vertices[v] = new Vector3(left, y, 0f);
                    _vertices[v + 1] = new Vector3(right, y, 0f);
                    _vertices[v + 2] = new Vector3(right, y + height, 0f);
                    _vertices[v + 3] = new Vector3(left, y + height, 0f);

                    float u0 = (float)(glyph * CellWidth) / AtlasWidth;
                    float u1 = (float)(glyph * CellWidth + CellWidth) / AtlasWidth;
                    _uvs[v] = new Vector2(u0, 0f);
                    _uvs[v + 1] = new Vector2(u1, 0f);
                    _uvs[v + 2] = new Vector2(u1, 1f);
                    _uvs[v + 3] = new Vector2(u0, 1f);

                    int t = i * 6;
                    // Clockwise when viewed from -Z (the label faces the camera along its local -Z).
                    _indices[t] = v;
                    _indices[t + 1] = v + 2;
                    _indices[t + 2] = v + 1;
                    _indices[t + 3] = v;
                    _indices[t + 4] = v + 3;
                    _indices[t + 5] = v + 2;
                }

                Mesh.vertices = _vertices;
                Mesh.uv = _uvs;
                Mesh.SetTriangles(_indices, 0, false);
                Mesh.bounds = new Bounds(Vector3.zero, new Vector3(totalWidth + 0.1f, height + 0.1f, 0.1f));
            }

            private static int GlyphIndex(char c)
            {
                if (c >= '0' && c <= '9')
                {
                    return c - '0';
                }
                return c == 'x' ? 11 : 10;
            }
        }
    }
}
