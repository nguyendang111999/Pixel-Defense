using System;
using System.IO;
using PixelDefense.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PixelDefense.EditorTools
{
    /// <summary>Procedural effect textures saved as PNGs with matching import settings.</summary>
    internal static class TextureBaker
    {
        public const string SoftCircle = AssetPaths.Textures + "/SoftCircle.png";
        public const string Ring = AssetPaths.Textures + "/RingBand.png";
        public const string Smoke = AssetPaths.Textures + "/Smoke.png";
        public const string Sparkle = AssetPaths.Textures + "/Sparkle.png";
        public const string Square = AssetPaths.Textures + "/Confetti.png";
        public const string Digits = AssetPaths.Textures + "/PixelDigits.png";
        public const string Vignette = AssetPaths.UiTextures + "/Vignette.png";

        public static void BakeAll()
        {
            Write(SoftCircle, 128, 128, (u, v) =>
            {
                float r = Mathf.Clamp01(Radius(u, v));
                float a = Mathf.Pow(1f - Smooth(r), 1.6f);
                return new Color(1f, 1f, 1f, a);
            });

            Write(Ring, 8, 64, (u, v) =>
            {
                float band = Mathf.Sin(v * Mathf.PI);
                return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Max(0f, band), 1.4f));
            });

            var random = new System.Random(7);
            var noise = new float[16, 16];
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    noise[x, y] = (float)random.NextDouble();
                }
            }

            Write(Smoke, 128, 128, (u, v) =>
            {
                float r = Radius(u, v);
                float n = ValueNoise(noise, u * 5f, v * 5f) * 0.55f + ValueNoise(noise, u * 11f + 3f, v * 11f + 7f) * 0.45f;
                float a = Mathf.Clamp01((1f - Smooth(Mathf.Clamp01(r * 1.05f))) * (0.55f + 0.6f * n));
                return new Color(1f, 1f, 1f, a);
            });

            Write(Sparkle, 64, 64, (u, v) =>
            {
                float x = (u - 0.5f) * 2f;
                float y = (v - 0.5f) * 2f;
                float cross = Mathf.Exp(-Mathf.Abs(x) * 9f) * Mathf.Exp(-Mathf.Abs(y) * 2.2f)
                              + Mathf.Exp(-Mathf.Abs(y) * 9f) * Mathf.Exp(-Mathf.Abs(x) * 2.2f);
                float core = Mathf.Exp(-(x * x + y * y) * 10f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(cross * 0.9f + core));
            });

            Write(Square, 16, 16, (u, v) =>
            {
                float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 16f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(edge));
            });

            Write(Vignette, 256, 256, (u, v) =>
            {
                float x = (u - 0.5f) * 2f;
                float y = (v - 0.5f) * 2f;
                float d = Mathf.Sqrt(x * x * 0.8f + y * y);
                float a = Mathf.Clamp01((d - 0.55f) / 0.6f);
                return new Color(1f, 1f, 1f, a * a);
            });

            Texture2D atlas = PixelFont.CreateAtlas(readable: true);
            File.WriteAllBytes(Digits, atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Configure(SoftCircle, FilterMode.Bilinear, true);
            Configure(Ring, FilterMode.Bilinear, false);
            Configure(Smoke, FilterMode.Bilinear, true);
            Configure(Sparkle, FilterMode.Bilinear, true);
            Configure(Square, FilterMode.Bilinear, false);
            Configure(Vignette, FilterMode.Bilinear, false);
            Configure(Digits, FilterMode.Point, false);
        }

        private static float Radius(float u, float v)
        {
            float x = (u - 0.5f) * 2f;
            float y = (v - 0.5f) * 2f;
            return Mathf.Sqrt(x * x + y * y);
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float ValueNoise(float[,] grid, float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = Smooth(x - x0);
            float fy = Smooth(y - y0);
            float a = grid[(x0 % 16 + 16) % 16, (y0 % 16 + 16) % 16];
            float b = grid[((x0 + 1) % 16 + 16) % 16, (y0 % 16 + 16) % 16];
            float c = grid[(x0 % 16 + 16) % 16, ((y0 + 1) % 16 + 16) % 16];
            float d = grid[((x0 + 1) % 16 + 16) % 16, ((y0 + 1) % 16 + 16) % 16];
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static void Write(string path, int width, int height, Func<float, float, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixel((x + 0.5f) / width, (y + 0.5f) / height);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void Configure(string path, FilterMode filter, bool mipmaps)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = filter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
