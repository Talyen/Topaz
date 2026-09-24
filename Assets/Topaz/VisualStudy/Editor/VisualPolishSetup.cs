using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Topaz.VisualStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Editor
{
    /// <summary>Adds optional native URP polish to the existing Visual Lab.</summary>
    public static class VisualPolishSetup
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        const string ControlsPath = "Assets/Topaz/Input/TopazControls.inputactions";

        [MenuItem("Topaz/Apply Clean Gameplay Presentation")]
        public static void CleanGameplayPresentation()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hud = GameObject.Find("Loop HUD");
            GameObject visualRoot = GameObject.Find("Visual Study");
            if (hud == null || visualRoot == null)
                throw new InvalidOperationException("Bootstrap HUD or Visual Study is missing.");
            Hide(hud.transform.Find("Resources"));
            Remove(hud.transform.Find("Visual Study Label"));
            Hide(visualRoot.transform.Find("Ground Details"));
            Transform panel = hud.transform.Find("Visual Lab");
            if (panel != null)
            {
                Remove(panel.Find("Ground detail"));
                Transform close = panel.Find("Close  •  F7") ?? panel.Find("Close");
                if (close == null) throw new InvalidOperationException("Visual Lab close button is missing.");
                close.name = "Close";
                close.GetComponentInChildren<TMP_Text>(true).text = "Close";
                Move(panel, "Focus mode", -175f, -174f);
                Move(panel, "Ambient occlusion", 175f, -174f);
            }

            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null) throw new InvalidOperationException("Desktop URP renderer is missing.");
            foreach (DecalRendererFeature feature in renderer.rendererFeatures.OfType<DecalRendererFeature>())
            {
                feature.SetActive(false);
                EditorUtility.SetDirty(feature);
            }
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null) throw new InvalidOperationException("Topaz input actions are missing.");
            InputActionMap map = controls.FindActionMap("Player", true);
            foreach (string name in new[] { "CycleLook", "ToggleAA", "VisualOptions" })
                map.FindAction(name)?.RemoveAction();
            File.WriteAllText(ControlsPath, controls.ToJson());
            AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);

            VolumeProfile focus = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Topaz/VisualStudy/Profiles/Focus Preview.asset");
            if (focus == null || !focus.TryGet(out DepthOfField depth))
                throw new InvalidOperationException("Focus profile is missing depth of field.");
            depth.mode.Override(DepthOfFieldMode.Gaussian);
            depth.gaussianStart.Override(30f);
            depth.gaussianEnd.Override(40f);
            depth.gaussianMaxRadius.Override(.5f);
            EditorUtility.SetDirty(depth);
            EditorUtility.SetDirty(focus);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(hud.scene);
            EditorSceneManager.SaveScene(hud.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Gameplay overlays, decals, and function-key toggles removed.");
        }

        static void Hide(Transform target)
        {
            if (target != null) target.gameObject.SetActive(false);
        }

        static void Remove(Transform target)
        {
            if (target != null) UnityEngine.Object.DestroyImmediate(target.gameObject);
        }

        [MenuItem("Topaz/Apply Visual Polish Study")]
        public static void Configure()
        {
            VisualStudySetup.ApplyFocusAndShadowDefaults();
            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            if (urp == null) throw new InvalidOperationException("Topaz URP asset is missing.");
            urp.hdrColorBufferPrecision = HDRColorBufferPrecision._64Bits;
            EditorUtility.SetDirty(urp);
            SetDesktopLightmapEncodingHigh();
            ConfigureTemperature("Painterly Clear", 25f);
            ConfigureTemperature("Warm Home", 60f);
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            ScreenSpaceAmbientOcclusion ao = renderer.rendererFeatures
                .OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if (ao == null) throw new InvalidOperationException("The existing URP AO feature is missing.");
            ConfigureAo(ao);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject visualRoot = GameObject.Find("Visual Study");
            GameObject hud = GameObject.Find("Loop HUD");
            if (visualRoot == null || hud == null)
                throw new InvalidOperationException("Bootstrap Visual Study or Loop HUD is missing.");
            Camera camera = Camera.main;
            if (camera == null || !camera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
                throw new InvalidOperationException("Main URP camera is missing.");
            cameraData.dithering = true;
            VisualLookController controller = visualRoot.GetComponent<VisualLookController>();
            VisualOptionsMenu menu = hud.GetComponent<VisualOptionsMenu>();
            if (controller == null || menu == null)
                throw new InvalidOperationException("Visual Lab controllers are missing.");
            Connect(controller, "ambientOcclusionFeature", ao);

            EditorSceneManager.MarkSceneDirty(visualRoot.scene);
            EditorSceneManager.SaveScene(visualRoot.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Bokeh and AO study configured.");
        }

        static void SetDesktopLightmapEncodingHigh()
        {
            // Unity's own HDRP setup wizard uses this PlayerSettings API via reflection.
            Type qualityType = typeof(PlayerSettings).Assembly.GetType("UnityEditor.LightmapEncodingQuality");
            MethodInfo set = typeof(PlayerSettings).GetMethod("SetLightmapEncodingQualityForPlatform",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (qualityType == null || set == null)
                throw new InvalidOperationException("Unity lightmap encoding API is unavailable.");
            object high = Enum.Parse(qualityType, "High");
            set.Invoke(null, new object[] { BuildTarget.StandaloneOSX, high });
            set.Invoke(null, new object[] { BuildTarget.StandaloneWindows64, high });
        }

        static void ConfigureTemperature(string name, float value)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                $"Assets/Topaz/VisualStudy/Profiles/{name}.asset");
            if (profile == null || !profile.TryGet(out WhiteBalance balance))
                throw new InvalidOperationException("Missing color profile " + name);
            balance.temperature.Override(value);
            EditorUtility.SetDirty(balance);
            EditorUtility.SetDirty(profile);
        }

        static void ConfigureAo(ScreenSpaceAmbientOcclusion ao)
        {
            var data = new SerializedObject(ao);
            SetFloat(data, "m_Settings.Intensity", .85f);
            SetFloat(data, "m_Settings.Radius", .25f);
            SetFloat(data, "m_Settings.DirectLightingStrength", .4f);
            SetBool(data, "m_Settings.Downsample", true);
            SetInt(data, "m_Settings.Samples", 2); // Low: four samples.
            SetInt(data, "m_Settings.BlurQuality", 1); // Medium Gaussian blur.
            data.ApplyModifiedPropertiesWithoutUndo();
            ao.SetActive(true);
            EditorUtility.SetDirty(ao);
        }

        static void Move(Transform panel, string name, float x, float y)
        {
            Transform child = panel.Find(name);
            if (child == null) throw new InvalidOperationException("Visual Lab is missing " + name);
            child.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }

        static void Connect(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException("Missing field " + name);
            field.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(SerializedObject data, string name, float value)
        {
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException("Missing setting " + name);
            field.floatValue = value;
        }

        static void SetInt(SerializedObject data, string name, int value)
        {
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException("Missing setting " + name);
            field.intValue = value;
        }

        static void SetBool(SerializedObject data, string name, bool value)
        {
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException("Missing setting " + name);
            field.boolValue = value;
        }

    }
}
