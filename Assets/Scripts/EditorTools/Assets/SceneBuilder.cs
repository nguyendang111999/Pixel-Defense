using PixelDefense.App;
using PixelDefense.Gameplay;
using PixelDefense.Services.Audio;
using PixelDefense.Services.Input;
using PixelDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PixelDefense.EditorTools
{
    /// <summary>Rebuilds Game.unity from scratch so the scene is reproducible from code and never hand-edited.</summary>
    internal static class SceneBuilder
    {
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(AssetPaths.Scene, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            // Load after OpenScene: opening a scene unloads assets that nothing referenced yet.
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetPaths.GameConfig);
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetPaths.PanelSettings);
            var post = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetPaths.PostProfile);
            VisualConfig visuals = config.Visuals;

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = visuals.BackgroundColor;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 120f;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 22f, -16f), Quaternion.Euler(58f, 0f, 0f));
            cameraObject.AddComponent<AudioListener>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.renderShadows = true;
            var rig = cameraObject.AddComponent<CameraRig>();

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            sun.shadowNormalBias = 0.4f;
            sunObject.transform.rotation = Quaternion.Euler(54f, -28f, 0f);
            sunObject.AddComponent<UniversalAdditionalLightData>();

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.49f, 0.78f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.26f, 0.42f);
            RenderSettings.fog = false;

            var postObject = new GameObject("Post");
            var volume = postObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = post;

            var battleObject = new GameObject("Battle");
            var look = battleObject.AddComponent<ToyLook>();
            Set(look, "_config", visuals);
            var battle = battleObject.AddComponent<BattleView>();
            Set(battle, "_visuals", visuals);
            Set(battle, "_cameraRig", rig);
            Set(battle, "_look", look);

            var uiObject = new GameObject("UI");
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetPaths.Uxml);
            var ui = uiObject.AddComponent<GameUi>();

            var appObject = new GameObject("App");
            var audio = appObject.AddComponent<AudioService>();
            Set(audio, "_library", config.Audio);
            var input = appObject.AddComponent<PointerInput>();
            var flow = appObject.AddComponent<GameFlow>();
            Set(flow, "_config", config);
            Set(flow, "_battle", battle);
            Set(flow, "_ui", ui);
            Set(flow, "_audio", audio);
            Set(flow, "_input", input);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError(target.GetType().Name + " has no serialized field " + field);
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
