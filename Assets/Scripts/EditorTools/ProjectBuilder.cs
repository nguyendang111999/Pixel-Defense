using System.IO;
using PixelDefense.App;
using PixelDefense.Gameplay;
using PixelDefense.Services.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace PixelDefense.EditorTools
{
    /// <summary>One-click regeneration of every procedural asset and the game scene.</summary>
    public static class ProjectBuilder
    {
        [MenuItem("Pixel Defense/Rebuild Everything", priority = 0)]
        public static void RebuildEverything()
        {
            BuildAssets();
            BuildScene();
        }

        [MenuItem("Pixel Defense/Rebuild Generated Assets", priority = 20)]
        public static void BuildAssets()
        {
            foreach (string folder in new[]
                     {
                         AssetPaths.Textures, AssetPaths.Materials, AssetPaths.Sfx, AssetPaths.Music, AssetPaths.Config,
                         AssetPaths.UiFonts, AssetPaths.UiTextures, AssetPaths.Levels
                     })
            {
                Directory.CreateDirectory(folder);
            }

            AssetDatabase.Refresh();
            TextureBaker.BakeAll();
            MaterialBaker.BakeAll();
            FontBaker.BakeAll();
            AudioLibrary audio = AudioBaker.BakeAll();
            VisualConfig visuals = ConfigBaker.BakeVisualConfig();
            ConfigBaker.BakeGameConfig(visuals, audio);
            ConfigBaker.BakePanelSettings();
            ConfigBaker.BakePostProfile();
            ConfigBaker.TunePipelineAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Pixel Defense: generated assets rebuilt.");
        }

        /// <summary>Discards inspector tweaks on the visual and game configs, keeping asset references.</summary>
        [MenuItem("Pixel Defense/Reset Configs To Code Defaults", priority = 22)]
        public static void ResetConfigs()
        {
            var visuals = AssetDatabase.LoadAssetAtPath<VisualConfig>(AssetPaths.VisualConfig);
            var defaults = ScriptableObject.CreateInstance<VisualConfig>();
            Material toy = visuals.ToyMaterial;
            Material instanced = visuals.ToyInstancedMaterial;
            Material digits = visuals.DigitsMaterial;
            Material glow = visuals.GlowMaterial;
            Material ring = visuals.RingMaterial;
            Material additive = visuals.ParticleAdditive;
            Material alpha = visuals.ParticleAlpha;
            Material confetti = visuals.ParticleConfetti;
            Material sparkle = visuals.ParticleSparkle;
            string assetName = visuals.name;
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(defaults), visuals);
            visuals.name = assetName;
            visuals.ToyMaterial = toy;
            visuals.ToyInstancedMaterial = instanced;
            visuals.DigitsMaterial = digits;
            visuals.GlowMaterial = glow;
            visuals.RingMaterial = ring;
            visuals.ParticleAdditive = additive;
            visuals.ParticleAlpha = alpha;
            visuals.ParticleConfetti = confetti;
            visuals.ParticleSparkle = sparkle;
            Object.DestroyImmediate(defaults);
            EditorUtility.SetDirty(visuals);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetPaths.GameConfig);
            config.Battle = new Core.BattleSettings { HeadLength = DragonView.HeadReach(visuals) };
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Pixel Defense: configs reset to code defaults.");
        }

        [MenuItem("Pixel Defense/Rebuild Game Scene", priority = 21)]
        public static void BuildScene()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(AssetPaths.GameConfig) == null ||
                AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetPaths.PanelSettings) == null ||
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetPaths.PostProfile) == null)
            {
                Debug.LogError("Pixel Defense: run 'Rebuild Generated Assets' first.");
                return;
            }

            SceneBuilder.Build();
            Debug.Log("Pixel Defense: Game scene rebuilt.");
        }
    }
}
