using System;
using PixelDefense.Core;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>Look and layout tunables for the battle scene: palette, skins, materials, sizes and juice.</summary>
    [CreateAssetMenu(menuName = "Pixel Defense/Visual Config", fileName = "VisualConfig")]
    public sealed class VisualConfig : ScriptableObject
    {
        [Header("World scale (slice units → world)")]
        public float WorldPerSlice = 0.3f;
        [Range(0.5f, 1f)] public float CubeFill = 0.86f;
        public float CubeHeight = 0.92f;
        public float HeadVoxel = 0.5f;
        public float NeckGap = 0.62f;
        public float WallHeight = 1.15f;

        [Header("Layout (slice units)")]
        public float SlotSize = 3.1f;
        public float SlotGap = 0.9f;

        [Tooltip("Widest the slot row may get, as a fraction of the base platform's diameter.")]
        [Range(0.5f, 1f)] public float SlotRowFill = 0.91f;

        public float ColumnSpacing = 5.4f;
        public float RowSpacing = 4.7f;
        public float ColumnsGap = 1.6f;

        [Tooltip("Cannons shown per column; deeper ones stay hidden until they move up.")]
        public int VisibleRows = 3;

        [Tooltip("Cannon size while waiting in the tray (slice units).")]
        public float CannonSize = 4.5f;

        [Tooltip("Largest cannon size once seated on a base slot (slice units).")]
        public float SlotCannonSize = 2.7f;

        [Tooltip("Seated cannon size as a fraction of the slot pitch, so a barrel aimed sideways clears its neighbour.")]
        [Range(0.4f, 1f)] public float SeatPitchFill = 0.68f;

        [Header("Palette (sRGB, indexed by ScaleColors)")]
        public Color[] ScaleColors = DefaultPalette();

        [Header("Environment")]
        public Color BackgroundColor = new Color(0.20f, 0.22f, 0.36f);
        public Color FloorColor = new Color(0.30f, 0.33f, 0.52f);
        public Color WallColor = new Color(0.80f, 0.82f, 0.95f);
        public Color PlatformColor = new Color(0.47f, 0.51f, 0.74f);
        public Color PlatformRimColor = new Color(0.86f, 0.88f, 1f);
        public Color SlotColor = new Color(0.21f, 0.23f, 0.38f);
        public Color TrayColor = new Color(0.24f, 0.26f, 0.42f);
        public Color DangerColor = new Color(1f, 0.25f, 0.2f);

        [Header("Dragon skins")]
        public DragonSkin[] Skins = DefaultSkins();

        [Header("Toy lighting")]
        public Color ShadowTint = new Color(0.55f, 0.5f, 0.9f);
        [Range(0f, 1f)] public float ShadowTintStrength = 0.45f;
        public Color RimColor = new Color(0.9f, 0.92f, 1f);
        [Range(0f, 1f)] public float RimStrength = 0.22f;
        [Range(0f, 1f)] public float Wrap = 0.45f;
        public float SpecularPower = 28f;
        [Range(0f, 1f)] public float SpecularStrength = 0.18f;
        public float AmbientMultiplier = 0.85f;

        [Header("Materials")]
        public Material ToyMaterial;
        public Material ToyInstancedMaterial;
        public Material DigitsMaterial;
        public Material GlowMaterial;
        public Material RingMaterial;
        public Material ParticleAdditive;
        public Material ParticleAlpha;
        public Material ParticleConfetti;
        public Material ParticleSparkle;

        [Header("Juice")]
        public float ShotArcHeight = 1.4f;
        public float ProjectileSize = 0.95f;
        public int DebrisPerCube = 5;
        public float DebrisSpeed = 5.5f;
        public float RecoilFrequency = 5.5f;
        [Range(0f, 1.5f)] public float RecoilDamping = 0.55f;
        public float BodyWaveAmplitude = 0.16f;
        public float ExposedPulse = 0.22f;
        [Range(0f, 0.5f)] public float UnexposedDarken = 0.1f;

        public Color ScaleColor(byte color)
        {
            return color < ScaleColors.Length ? ScaleColors[color] : Color.magenta;
        }

        public DragonSkin Skin(string id)
        {
            for (int i = 0; i < Skins.Length; i++)
            {
                if (string.Equals(Skins[i].Id, id, StringComparison.Ordinal))
                {
                    return Skins[i];
                }
            }
            return Skins.Length > 0 ? Skins[0] : new DragonSkin();
        }

        public static Color[] DefaultPalette()
        {
            var colors = new Color[Core.ScaleColors.Count];
            colors[0] = Hex(0xF2453D);  // Red
            colors[1] = Hex(0xFF8C1F);  // Orange
            colors[2] = Hex(0xFFD23A);  // Yellow
            colors[3] = Hex(0x47CF4A);  // Green
            colors[4] = Hex(0xA5E33A);  // Lime
            colors[5] = Hex(0x33D2E8);  // Cyan
            colors[6] = Hex(0x3F7CF4);  // Blue
            colors[7] = Hex(0x9C55F2);  // Purple
            colors[8] = Hex(0xFF73B9);  // Pink
            colors[9] = Hex(0xF6F2EA);  // White
            colors[10] = Hex(0xB0743F); // Brown
            colors[11] = Hex(0x404659); // Dark
            return colors;
        }

        public static DragonSkin[] DefaultSkins()
        {
            return new[]
            {
                new DragonSkin { Id = "ember", Main = Hex(0xFF7A1A), Belly = Hex(0xFFC27A), Horn = Hex(0xFFF1D8), Accent = Hex(0xD9481C) },
                new DragonSkin { Id = "jade", Main = Hex(0x36C25A), Belly = Hex(0xB9F28C), Horn = Hex(0xFFF4C9), Accent = Hex(0x1E8C44) },
                new DragonSkin { Id = "frost", Main = Hex(0x5CC8F2), Belly = Hex(0xE2F7FF), Horn = Hex(0xFFFFFF), Accent = Hex(0x2E86C9) },
                new DragonSkin { Id = "venom", Main = Hex(0x9C55F2), Belly = Hex(0xE3C8FF), Horn = Hex(0xFFE36E), Accent = Hex(0x6A2DC0) },
                new DragonSkin { Id = "gold", Main = Hex(0xFFC21F), Belly = Hex(0xFFF0B0), Horn = Hex(0xFFFFFF), Accent = Hex(0xE07A10) },
                new DragonSkin { Id = "shadow", Main = Hex(0x4A4F66), Belly = Hex(0x8F94AD), Horn = Hex(0xFF5A3C), Accent = Hex(0x2A2D3D) },
            };
        }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }
    }

    [Serializable]
    public sealed class DragonSkin
    {
        public string Id = "ember";
        public Color Main = Color.red;
        public Color Belly = Color.white;
        public Color Horn = Color.white;
        public Color Accent = Color.black;
    }
}
