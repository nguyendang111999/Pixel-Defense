using PixelDefense.App;
using PixelDefense.Gameplay;
using PixelDefense.Services.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace PixelDefense.EditorTools
{
    /// <summary>Config assets, panel settings, post-processing profile and render pipeline quality tweaks.</summary>
    internal static class ConfigBaker
    {
        public static VisualConfig BakeVisualConfig()
        {
            var config = LoadOrCreate<VisualConfig>(AssetPaths.VisualConfig);
            config.ToyMaterial = Load<Material>(MaterialBaker.Toy);
            config.ToyInstancedMaterial = Load<Material>(MaterialBaker.ToyInstanced);
            config.DigitsMaterial = Load<Material>(MaterialBaker.Digits);
            config.GlowMaterial = Load<Material>(MaterialBaker.Glow);
            config.RingMaterial = Load<Material>(MaterialBaker.Ring);
            config.ParticleAdditive = Load<Material>(MaterialBaker.ParticleAdditive);
            config.ParticleAlpha = Load<Material>(MaterialBaker.ParticleAlpha);
            config.ParticleConfetti = Load<Material>(MaterialBaker.ParticleConfetti);
            config.ParticleSparkle = Load<Material>(MaterialBaker.ParticleSparkle);
            EditorUtility.SetDirty(config);
            return config;
        }

        public static GameConfig BakeGameConfig(VisualConfig visuals, AudioLibrary audio)
        {
            var config = LoadOrCreate<GameConfig>(AssetPaths.GameConfig);
            config.Visuals = visuals;
            config.Audio = audio;
            config.LevelPack = Load<TextAsset>(AssetPaths.LevelPack);
            config.Battle.HeadLength = DragonView.HeadReach(visuals);
            EditorUtility.SetDirty(config);
            return config;
        }

        public static PanelSettings BakePanelSettings()
        {
            var settings = LoadOrCreate<PanelSettings>(AssetPaths.PanelSettings);
            settings.themeStyleSheet = Load<ThemeStyleSheet>(AssetPaths.Theme);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1080, 1920);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.clearColor = false;
            settings.sortingOrder = 0;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        public static VolumeProfile BakePostProfile()
        {
            var profile = Load<VolumeProfile>(AssetPaths.PostProfile);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, AssetPaths.PostProfile);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.62f);
            bloom.highQualityFiltering.Override(false);

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(10f);
            color.contrast.Override(8f);
            color.postExposure.Override(0.12f);

            foreach (VolumeComponent component in profile.components)
            {
                component.name = component.GetType().Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>Crisper edges and softer, higher-resolution shadows for the toy look on both quality tiers.</summary>
        public static void TunePipelineAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/Settings" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = Load<UniversalRenderPipelineAsset>(path);
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("m_MSAA").intValue = 4;
                serialized.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
                serialized.FindProperty("m_ShadowDistance").floatValue = 45f;
                serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
                serialized.FindProperty("m_SupportsHDR").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = Load<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
