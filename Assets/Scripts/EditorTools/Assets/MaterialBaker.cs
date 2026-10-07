using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelDefense.EditorTools
{
    /// <summary>Creates (or updates in place) every material the game uses.</summary>
    internal static class MaterialBaker
    {
        public const string Toy = AssetPaths.Materials + "/Toy.mat";
        public const string ToyInstanced = AssetPaths.Materials + "/ToyInstanced.mat";
        public const string Digits = AssetPaths.Materials + "/PixelDigits.mat";
        public const string Glow = AssetPaths.Materials + "/Glow.mat";
        public const string Ring = AssetPaths.Materials + "/Ring.mat";
        public const string ParticleAdditive = AssetPaths.Materials + "/ParticleAdditive.mat";
        public const string ParticleAlpha = AssetPaths.Materials + "/ParticleAlpha.mat";
        public const string ParticleConfetti = AssetPaths.Materials + "/ParticleConfetti.mat";
        public const string ParticleSparkle = AssetPaths.Materials + "/ParticleSparkle.mat";

        public static void BakeAll()
        {
            Material toy = Ensure(Toy, "PixelDefense/Toy");
            toy.SetColor("_BaseColor", Color.white);
            toy.SetFloat("_VertexColorWeight", 1f);
            toy.enableInstancing = true;

            Material instanced = Ensure(ToyInstanced, "PixelDefense/ToyInstanced");
            instanced.enableInstancing = true;

            Material digits = Ensure(Digits, "PixelDefense/PixelDigits");
            digits.SetTexture("_BaseMap", Load(TextureBaker.Digits));
            digits.SetFloat("_Cutoff", 0.5f);

            SetupGlow(Ensure(Glow, "PixelDefense/Glow"), TextureBaker.SoftCircle, additive: true);
            SetupGlow(Ensure(Ring, "PixelDefense/Glow"), TextureBaker.Ring, additive: true);
            SetupGlow(Ensure(ParticleAdditive, "PixelDefense/Glow"), TextureBaker.SoftCircle, additive: true);
            SetupGlow(Ensure(ParticleAlpha, "PixelDefense/Glow"), TextureBaker.Smoke, additive: false);
            SetupGlow(Ensure(ParticleConfetti, "PixelDefense/Glow"), TextureBaker.Square, additive: false);
            SetupGlow(Ensure(ParticleSparkle, "PixelDefense/Glow"), TextureBaker.Sparkle, additive: true);
            AssetDatabase.SaveAssets();
        }

        private static void SetupGlow(Material material, string texture, bool additive)
        {
            material.SetTexture("_BaseMap", Load(texture));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static Texture2D Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Ensure(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
