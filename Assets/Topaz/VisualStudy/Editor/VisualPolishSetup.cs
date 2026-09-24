using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Topaz.VisualStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Editor
{
    /// <summary>Adds optional native URP polish to the existing Visual Lab.</summary>
    public static class VisualPolishSetup
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        const string DecalFolder = "Assets/Topaz/VisualStudy/Decals";
        const string TexturePath = DecalFolder + "/Ground Wear.png";
        const string MaterialPath = DecalFolder + "/Ground Wear.mat";

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
            DecalRendererFeature decals = EnsureDecalFeature(renderer);
            Material material = EnsureGroundMaterial();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject visualRoot = GameObject.Find("Visual Study");
            GameObject hud = GameObject.Find("Loop HUD");
            if (visualRoot == null || hud == null)
                throw new InvalidOperationException("Bootstrap Visual Study or Loop HUD is missing.");
            GameObject ground = EnsureGroundDetails(visualRoot.transform, material);
            Camera camera = Camera.main;
            if (camera == null || !camera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
                throw new InvalidOperationException("Main URP camera is missing.");
            cameraData.dithering = true;
            VisualLookController controller = visualRoot.GetComponent<VisualLookController>();
            VisualOptionsMenu menu = hud.GetComponent<VisualOptionsMenu>();
            if (controller == null || menu == null)
                throw new InvalidOperationException("Visual Lab controllers are missing.");
            Connect(controller, "ambientOcclusionFeature", ao);
            Connect(controller, "groundDecalFeature", decals);
            Connect(controller, "groundDetailRoot", ground);
            ConfigureMenu(hud.transform, menu);

            EditorSceneManager.MarkSceneDirty(visualRoot.scene);
            EditorSceneManager.SaveScene(visualRoot.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Bokeh, AO, and ground detail study configured.");
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

        static DecalRendererFeature EnsureDecalFeature(UniversalRendererData renderer)
        {
            DecalRendererFeature feature = renderer.rendererFeatures
                .OfType<DecalRendererFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<DecalRendererFeature>();
                feature.name = "Ground Decals";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
            }
            var data = new SerializedObject(feature);
            SetInt(data, "m_Settings.technique", 2); // URP screen-space decals.
            SetFloat(data, "m_Settings.maxDrawDistance", 45f);
            data.ApplyModifiedPropertiesWithoutUndo();
            feature.SetActive(true);
            feature.Create();
            var rendererData = new SerializedObject(renderer);
            SerializedProperty featureMap = rendererData.FindProperty("m_RendererFeatureMap");
            if (featureMap == null) throw new InvalidOperationException("URP renderer feature map is missing.");
            featureMap.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i],
                    out string _, out long localId))
                    throw new InvalidOperationException("URP renderer feature has no local asset ID.");
                featureMap.GetArrayElementAtIndex(i).longValue = localId;
            }
            rendererData.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            return feature;
        }

        static Material EnsureGroundMaterial()
        {
            EnsureFolder(DecalFolder);
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    float u = (x + .5f) / texture.width * 2f - 1f;
                    float v = (y + .5f) / texture.height * 2f - 1f;
                    float wobble = .92f + .08f * Mathf.PerlinNoise(x * .075f, y * .075f);
                    float r = Mathf.Sqrt(u * u + v * v) / wobble;
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - r) / .62f));
                    float fleck = .88f + .12f * Mathf.PerlinNoise(x * .18f + 31f, y * .18f + 7f);
                    texture.SetPixel(x, y, new Color(.22f, .17f, .11f, edge * fleck * .72f));
                }
                texture.Apply();
                File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Decal.mat");
                if (source == null) throw new InvalidOperationException("Unity's URP Decal material is missing.");
                material = new Material(source) { name = "Ground Wear" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("Base_Map", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject EnsureGroundDetails(Transform parent, Material material)
        {
            Transform existing = parent.Find("Ground Details");
            GameObject root = existing != null ? existing.gameObject : new GameObject("Ground Details");
            root.transform.SetParent(parent, false);
            Patch(root.transform, "Camp Clearing", material, new Vector3(.3f, .55f, -2.6f),
                new Vector3(5f, 3.2f, 1f), 13f, .86f);
            Patch(root.transform, "West Trail Wear", material, new Vector3(-3.8f, .55f, -1.0f),
                new Vector3(2.8f, 2f, 1f), -21f, .76f);
            Patch(root.transform, "East Trail Wear", material, new Vector3(3.2f, .55f, 2.2f),
                new Vector3(2.8f, 1.9f, 1f), 24f, .75f);
            root.SetActive(true);
            return root;
        }

        static void Patch(Transform parent, string name, Material material,
            Vector3 position, Vector3 size, float rotation, float fade)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90f, 0f, rotation);
            DecalProjector projector = go.GetComponent<DecalProjector>();
            if (projector == null) projector = go.AddComponent<DecalProjector>();
            projector.material = material;
            projector.size = size;
            projector.drawDistance = 45f;
            projector.fadeFactor = fade;
        }

        static void ConfigureMenu(Transform hud, VisualOptionsMenu menu)
        {
            Transform panel = hud.Find("Visual Lab");
            if (panel == null) throw new InvalidOperationException("Visual Lab panel is missing.");
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1120f, 960f);
            Move(panel, "Settings Grid", 0f, -226f);
            Move(panel, "Status", 0f, -805f);
            Move(panel, "Reset", -390f, -865f);
            Move(panel, "Save selection", -130f, -865f);
            Move(panel, "Copy values", 130f, -865f);
            Move(panel, "Close  •  F7", 390f, -865f);
            Connect(menu, "focusModeButton", CloneButton(panel, "Look", "Focus mode", -350f));
            Connect(menu, "ambientOcclusionButton", CloneButton(panel, "AA", "Ambient occlusion", 0f));
            Connect(menu, "groundDetailButton", CloneButton(panel, "Depth of field", "Ground detail", 350f));
        }

        static UnityEngine.UI.Button CloneButton(Transform panel, string sourceName,
            string name, float x)
        {
            Transform existing = panel.Find(name);
            GameObject go = existing != null ? existing.gameObject :
                UnityEngine.Object.Instantiate(panel.Find(sourceName).gameObject, panel);
            go.name = name;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, -174f);
            TMP_Text label = go.GetComponentInChildren<TMP_Text>();
            label.text = name;
            return go.GetComponent<UnityEngine.UI.Button>();
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

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
