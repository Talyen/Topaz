using System;
using System.IO;
using System.Linq;
using TMPro;
using Topaz.VisualStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Author a reversible URP lighting and post-processing comparison.</summary>
    public static class VisualStudySetup
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string ControlsPath = "Assets/Topaz/Input/TopazControls.inputactions";
        const string ProfileFolder = "Assets/Topaz/VisualStudy/Profiles";

        [MenuItem("Topaz/Build Visual Study")]
        public static void Configure()
        {
            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            if (urp == null || !urp.supportsHDR)
                throw new InvalidOperationException("Topaz needs active HDR URP for this visual study.");
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                "Assets/Settings/PC_Renderer.asset");
            if (renderer == null || new SerializedObject(renderer).FindProperty("postProcessData")
                ?.objectReferenceValue == null)
                throw new InvalidOperationException("URP renderer PostProcessData is missing.");
            EnsureFolder(ProfileFolder);
            AddBindings();
            VolumeProfile painterly = Profile("Painterly Clear");
            ConfigurePainterly(painterly);
            VolumeProfile home = Profile("Warm Home");
            ConfigureHome(home);
            VolumeProfile focus = Profile("Focus Preview");
            ConfigureFocus(focus);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            GameObject player = GameObject.Find("Player");
            GameObject hud = GameObject.Find("Loop HUD");
            GameObject globalObject = GameObject.Find("Global Volume") ??
                Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(go =>
                    go.scene == scene && go.name == "Global Volume");
            if (camera == null || player == null || hud == null || globalObject == null)
                throw new InvalidOperationException("Bootstrap camera, player, HUD, or Global Volume is missing.");
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null) throw new InvalidOperationException("URP camera data is missing.");

            GameObject prior = GameObject.Find("Visual Study");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior);
            var root = new GameObject("Visual Study");
            var painterlyVolume = globalObject.GetComponent<Volume>();
            globalObject.SetActive(true);
            painterlyVolume.isGlobal = true;
            painterlyVolume.priority = 0f;
            painterlyVolume.weight = 1f;
            painterlyVolume.sharedProfile = painterly;

            GameObject homeObject = new GameObject("Warm Home Grade");
            homeObject.transform.SetParent(root.transform, false);
            homeObject.transform.position = Vector3.zero;
            SphereCollider homeCollider = homeObject.AddComponent<SphereCollider>();
            homeCollider.isTrigger = true;
            homeCollider.radius = 3.1f;
            Volume homeVolume = homeObject.AddComponent<Volume>();
            homeVolume.isGlobal = false;
            homeVolume.priority = 5f;
            homeVolume.blendDistance = 2.5f;
            homeVolume.weight = 1f;
            homeVolume.sharedProfile = home;

            GameObject focusObject = new GameObject("Focus Preview Grade");
            focusObject.transform.SetParent(root.transform, false);
            Volume focusVolume = focusObject.AddComponent<Volume>();
            focusVolume.isGlobal = true;
            focusVolume.priority = 10f;
            focusVolume.weight = 1f;
            focusVolume.sharedProfile = focus;
            focusVolume.enabled = false;

            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1 << 0;
            cameraData.volumeTrigger = player.transform;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.Medium;
            camera.allowHDR = true;

            urp.msaaSampleCount = 1; // SMAA comparison stays independent of MSAA cost.
            urp.shadowDistance = 32f;
            urp.shadowCascadeCount = 2;
            EditorUtility.SetDirty(urp);

            Light sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (sun == null) throw new InvalidOperationException("Main directional light is missing.");
            sun.color = new Color(.93f,.96f,1f);
            sun.intensity = 1.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .72f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.34f,.36f,.40f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.28f,.32f,.34f);
            RenderSettings.fogStartDistance = 34f;
            RenderSettings.fogEndDistance = 70f;

            AddLantern(root.transform);

            GameObject homeLightObject = new GameObject("Home Warm Fill");
            homeLightObject.transform.SetParent(root.transform, false);
            homeLightObject.transform.position = new Vector3(-1.6f,1.5f,-1.1f);
            Light homeLight = homeLightObject.AddComponent<Light>();
            homeLight.type = LightType.Point;
            homeLight.color = new Color(1f,.73f,.48f);
            homeLight.intensity = 8f;
            homeLight.range = 7.5f;
            homeLight.shadows = LightShadows.None;

            TMP_Text lookLabel = CreateLabel(hud.transform);
            VisualOptionsMenu optionsMenu = CreateOptionsMenu(hud.transform,
                hud.GetComponent<Topaz.LoopStudy.LoopHud>());
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            VisualLookController controller = root.AddComponent<VisualLookController>();
            Ref(controller, "controls", controls);
            Ref(controller, "cameraData", cameraData);
            Ref(controller, "painterlyVolume", painterlyVolume);
            Ref(controller, "homeVolume", homeVolume);
            Ref(controller, "focusVolume", focusVolume);
            Ref(controller, "homeLight", homeLight);
            Ref(controller, "sun", sun);
            Ref(controller, "lookLabel", lookLabel);
            Ref(controller, "optionsMenu", optionsMenu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] URP visual study configured.");
        }

        static VolumeProfile Profile(string name)
        {
            string path = $"{ProfileFolder}/{name}.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null)
            {
                profile.components.RemoveAll(component => component == null);
                EditorUtility.SetDirty(profile);
                return profile;
            }
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = name;
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out T effect)) return effect;
            effect = profile.Add<T>();
            if (!AssetDatabase.Contains(effect)) AssetDatabase.AddObjectToAsset(effect, profile);
            EditorUtility.SetDirty(profile);
            return effect;
        }

        static void ConfigurePainterly(VolumeProfile profile)
        {
            Tonemapping tone = Effect<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);
            ColorAdjustments color = Effect<ColorAdjustments>(profile);
            color.postExposure.Override(.12f);
            color.contrast.Override(7f);
            color.saturation.Override(7f);
            WhiteBalance balance = Effect<WhiteBalance>(profile);
            balance.temperature.Override(-2f);
            Bloom bloom = Effect<Bloom>(profile);
            bloom.threshold.Override(.95f);
            bloom.intensity.Override(.28f);
            bloom.scatter.Override(.65f);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.highQualityFiltering.Override(false);
            Vignette vignette = Effect<Vignette>(profile);
            vignette.intensity.Override(.08f);
            vignette.smoothness.Override(.55f);
            Dirty(profile);
        }

        static void ConfigureHome(VolumeProfile profile)
        {
            WhiteBalance balance = Effect<WhiteBalance>(profile);
            balance.temperature.Override(16f);
            ColorAdjustments color = Effect<ColorAdjustments>(profile);
            color.postExposure.Override(.18f);
            Dirty(profile);
        }

        static void ConfigureFocus(VolumeProfile profile)
        {
            if (profile.Has<Tonemapping>()) profile.Remove<Tonemapping>();
            if (profile.Has<ColorAdjustments>()) profile.Remove<ColorAdjustments>();
            if (profile.Has<Bloom>()) profile.Remove<Bloom>();
            if (profile.Has<Vignette>()) profile.Remove<Vignette>();
            DepthOfField depth = Effect<DepthOfField>(profile);
            depth.mode.Override(DepthOfFieldMode.Gaussian);
            depth.gaussianStart.Override(23f);
            depth.gaussianEnd.Override(36f);
            depth.gaussianMaxRadius.Override(1.0f);
            Dirty(profile);
        }

        static void Dirty(VolumeProfile profile)
        {
            foreach (VolumeComponent component in profile.components) EditorUtility.SetDirty(component);
            EditorUtility.SetDirty(profile);
        }

        static void AddLantern(Transform parent)
        {
            var anchor = new GameObject("KayKit Lantern");
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = new Vector3(-1.85f,0f,-.75f);
            string modelPath = "Assets/ThirdParty/KayKit/RPGTools/Models/lantern.fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Material tools = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Topaz/ArtStudy/Materials/Tools.mat");
            if (source == null || tools == null)
                throw new InvalidOperationException("KayKit lantern or its URP material is missing.");
            GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
            model.transform.SetParent(anchor.transform, false);
            model.transform.localPosition = Vector3.zero;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer meshRenderer in renderers)
            {
                meshRenderer.sharedMaterials = Enumerable.Repeat(tools,
                    Math.Max(1, meshRenderer.sharedMaterials.Length)).ToArray();
                bounds.Encapsulate(meshRenderer.bounds);
            }
            if (bounds.size.y > .001f) model.transform.localScale = Vector3.one * (.55f / bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (Renderer meshRenderer in renderers.Skip(1)) bounds.Encapsulate(meshRenderer.bounds);
            model.transform.position += Vector3.up * (anchor.transform.position.y - bounds.min.y);

            Material ember = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Topaz/VisualStudy/Materials/Lantern Ember.mat");
            if (ember == null)
            {
                EnsureFolder("Assets/Topaz/VisualStudy/Materials");
                ember = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    { name = "Lantern Ember" };
                ember.SetColor("_BaseColor", new Color(1f,.72f,.35f));
                ember.SetColor("_EmissionColor", new Color(4f,1.8f,.45f));
                ember.EnableKeyword("_EMISSION");
                AssetDatabase.CreateAsset(ember, "Assets/Topaz/VisualStudy/Materials/Lantern Ember.mat");
            }
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "Lantern Ember";
            glow.transform.SetParent(anchor.transform, false);
            glow.transform.localPosition = new Vector3(0f,.40f,0f);
            glow.transform.localScale = Vector3.one * .12f;
            glow.GetComponent<Renderer>().sharedMaterial = ember;
            UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());

            GameObject motes = new GameObject("Lantern Motes", typeof(ParticleSystem));
            motes.transform.SetParent(anchor.transform, false);
            motes.transform.localPosition = new Vector3(0f,.45f,0f);
            ParticleSystem particles = motes.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f,3.2f);
            main.startSpeed = .15f;
            main.startSize = new ParticleSystem.MinMaxCurve(.035f,.065f);
            main.startColor = new Color(1f,.76f,.45f,.6f);
            main.maxParticles = 24;
            var emission = particles.emission;
            emission.rateOverTime = 2.5f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .4f;
            var particleRenderer = motes.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = ember;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        static TMP_Text CreateLabel(Transform canvas)
        {
            Transform prior = canvas.Find("Visual Study Label");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
            var panel = new GameObject("Visual Study Label", typeof(RectTransform),
                typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(canvas, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f,1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(1f,1f);
            rect.anchoredPosition = new Vector2(-24f,-24f);
            rect.sizeDelta = new Vector2(450f,102f);
            var image = panel.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(.07f,.11f,.14f,.72f);
            image.raycastTarget = false;
            var text = new GameObject("Look and AA", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(panel.transform, false);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f,10f);
            textRect.offsetMax = new Vector2(-12f,-10f);
            TextMeshProUGUI label = text.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 23;
            label.color = Color.white;
            label.text = "LOOK  Painterly  •  AA SMAA\nF5 look   F6 AA   F7 options";
            label.alignment = TextAlignmentOptions.Right;
            label.raycastTarget = false;
            return label;
        }

        static VisualOptionsMenu CreateOptionsMenu(Transform canvas,
            Topaz.LoopStudy.LoopHud hud)
        {
            Transform prior = canvas.Find("Visual Lab");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            RectTransform panel = Box("Visual Lab", canvas, new Vector2(1120f,860f),
                new Vector2(.5f,.5f), Vector2.zero, new Color(.07f,.12f,.15f,.97f));
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Text(panel, "Heading", font, "Visual Lab", 34,
                new Vector2(0f,-22f), new Vector2(1040f,52f), TextAlignmentOptions.Center);
            Text(panel, "Explanation", font,
                "Adjust live in the Mac build. Save selection to reload it; Copy lets you share exact values.",
                21, new Vector2(0f,-70f), new Vector2(1050f,38f), TextAlignmentOptions.Center);

            UnityEngine.UI.Button look = Button(panel, "Look", font,
                new Vector2(-350f,-115f), new Vector2(320f,50f));
            UnityEngine.UI.Button aa = Button(panel, "AA", font,
                new Vector2(0f,-115f), new Vector2(320f,50f));
            UnityEngine.UI.Button depth = Button(panel, "Depth of field", font,
                new Vector2(350f,-115f), new Vector2(320f,50f));

            RectTransform grid = Box("Settings Grid", panel, new Vector2(1040f,548f),
                new Vector2(.5f,1f), new Vector2(0f,-163f), Color.clear);
            grid.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var layout = grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            layout.cellSize = new Vector2(510f,70f);
            layout.spacing = new Vector2(20f,8f);
            layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            var sliders = new UnityEngine.UI.Slider[VisualLookController.SettingNames.Length];
            var values = new TMP_Text[sliders.Length];
            for (int i = 0; i < sliders.Length; i++)
                SliderRow(grid, font, i, out sliders[i], out values[i]);

            TMP_Text status = Text(panel, "Status", font,
                "Preset changes are local until saved.", 20,
                new Vector2(0f,-735f), new Vector2(1020f,32f), TextAlignmentOptions.Center);
            UnityEngine.UI.Button reset = Button(panel, "Reset", font,
                new Vector2(-390f,-790f), new Vector2(245f,52f));
            UnityEngine.UI.Button save = Button(panel, "Save selection", font,
                new Vector2(-130f,-790f), new Vector2(245f,52f));
            UnityEngine.UI.Button copy = Button(panel, "Copy values", font,
                new Vector2(130f,-790f), new Vector2(245f,52f));
            UnityEngine.UI.Button close = Button(panel, "Close  •  F7", font,
                new Vector2(390f,-790f), new Vector2(245f,52f));

            VisualOptionsMenu menu = canvas.GetComponent<VisualOptionsMenu>();
            if (menu == null) menu = canvas.gameObject.AddComponent<VisualOptionsMenu>();
            Ref(menu, "panel", panel.gameObject);
            Refs(menu, "sliders", sliders);
            Refs(menu, "values", values);
            Ref(menu, "lookButton", look);
            Ref(menu, "aaButton", aa);
            Ref(menu, "depthButton", depth);
            Ref(menu, "resetButton", reset);
            Ref(menu, "saveButton", save);
            Ref(menu, "copyButton", copy);
            Ref(menu, "closeButton", close);
            Ref(menu, "status", status);
            Ref(menu, "loopHud", hud);
            Ref(hud, "visualOptionsPanel", panel.gameObject);
            panel.gameObject.SetActive(false);
            return menu;
        }

        static void SliderRow(Transform parent, TMP_FontAsset font, int index,
            out UnityEngine.UI.Slider slider, out TMP_Text valueLabel)
        {
            RectTransform row = Box(VisualLookController.SettingNames[index], parent,
                new Vector2(510f,70f), new Vector2(.5f,.5f), Vector2.zero,
                new Color(.16f,.23f,.27f,.95f));
            row.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(row, "Name", font, VisualLookController.SettingNames[index], 22,
                new Vector2(-54f,-6f), new Vector2(355f,30f), TextAlignmentOptions.Left);
            valueLabel = Text(row, "Value", font, "0", 22,
                new Vector2(207f,-6f), new Vector2(88f,30f), TextAlignmentOptions.Right);
            RectTransform sliderRect = Box("Slider", row, new Vector2(466f,23f),
                new Vector2(.5f,1f), new Vector2(0f,-40f), Color.clear);
            UnityEngine.Object.DestroyImmediate(sliderRect.GetComponent<UnityEngine.UI.Image>());
            slider = sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = VisualLookController.Minimum[index];
            slider.maxValue = VisualLookController.Maximum[index];
            slider.wholeNumbers = index == 0 || index == 2 || index == 3 || index == 7 ||
                index == 8 || index == 10 || index == 13;
            RectTransform track = Box("Track", sliderRect, new Vector2(466f,9f),
                new Vector2(.5f,.5f), Vector2.zero, new Color(.06f,.10f,.12f,1f));
            track.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            RectTransform fill = Box("Fill", sliderRect, new Vector2(466f,9f),
                new Vector2(.5f,.5f), Vector2.zero, new Color(.44f,.73f,.72f,1f));
            fill.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f,1f);
            fill.pivot = new Vector2(0f,.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            RectTransform handle = Box("Handle", sliderRect, new Vector2(18f,26f),
                new Vector2(0f,.5f), Vector2.zero, new Color(.93f,.96f,.85f,1f));
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
        }

        static RectTransform Box(string name, Transform parent, Vector2 size,
            Vector2 anchor, Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor.y == 1f ? new Vector2(.5f,1f) : new Vector2(.5f,.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<UnityEngine.UI.Image>().color = color;
            return rect;
        }

        static TMP_Text Text(Transform parent, string name, TMP_FontAsset font,
            string value, float size, Vector2 position, Vector2 dimensions,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f,1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(.5f,1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = Color.white;
            label.text = value;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        static UnityEngine.UI.Button Button(Transform parent, string name, TMP_FontAsset font,
            Vector2 position, Vector2 size)
        {
            RectTransform rect = Box(name, parent, size, new Vector2(.5f,1f), position,
                new Color(.24f,.46f,.51f,1f));
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            TMP_Text label = Text(rect, "Text", font, name, 23,
                new Vector2(0f,-7f), new Vector2(size.x-14f,size.y-10f),
                TextAlignmentOptions.Center);
            return button;
        }

        static void Refs<T>(UnityEngine.Object target, string name, T[] values)
            where T : UnityEngine.Object
        {
            var data = new SerializedObject(target);
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            field.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddBindings()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            InputActionMap map = asset.FindActionMap("Player", true);
            if (map.FindAction("CycleLook") == null)
                map.AddAction("CycleLook", InputActionType.Button).AddBinding("<Keyboard>/f5");
            if (map.FindAction("ToggleAA") == null)
                map.AddAction("ToggleAA", InputActionType.Button).AddBinding("<Keyboard>/f6");
            if (map.FindAction("VisualOptions") == null)
                map.AddAction("VisualOptions", InputActionType.Button).AddBinding("<Keyboard>/f7");
            File.WriteAllText(ControlsPath, asset.ToJson());
            AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);
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

        static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            field.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
