using System;
using System.Linq;
using TMPro;
using Topaz.VisualStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Author a reversible URP lighting and post-processing comparison.</summary>
    public static class VisualStudySetup
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string ProfileFolder = "Assets/Topaz/VisualStudy/Profiles";
        const string OrthographicDofShaderPath =
            "Assets/Topaz/VisualStudy/Shaders/OrthographicGaussianDepthOfField.shader";
        const string OrthographicBokehShaderPath =
            "Assets/Topaz/VisualStudy/Shaders/OrthographicBokehDepthOfField.shader";
        const string PostProcessDataPath =
            "Assets/Topaz/VisualStudy/OrthographicPostProcessData.asset";

        [MenuItem("Topaz/Apply Focus And Shadow Defaults")]
        public static void ApplyFocusAndShadowDefaults()
        {
            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            if (urp == null) throw new InvalidOperationException("Topaz URP asset is missing.");
            ConfigureShadows(urp);
            ConfigureFocus(Profile("Focus Preview"));
            ConfigureOrthographicDepthOfField();
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Focus and shadow defaults updated.");
        }

        static void ConfigureOrthographicDepthOfField()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                "Assets/Settings/PC_Renderer.asset");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(OrthographicDofShaderPath);
            var bokehShader = AssetDatabase.LoadAssetAtPath<Shader>(OrthographicBokehShaderPath);
            if (renderer == null || shader == null || bokehShader == null)
                throw new InvalidOperationException("Topaz renderer or orthographic depth shaders are missing.");

            var data = AssetDatabase.LoadAssetAtPath<PostProcessData>(PostProcessDataPath);
            if (data == null)
            {
                var source = renderer.postProcessData;
                if (source == null) throw new InvalidOperationException("URP PostProcessData is missing.");
                data = UnityEngine.Object.Instantiate(source);
                data.name = "Topaz Orthographic Post-process Data";
                AssetDatabase.CreateAsset(data, PostProcessDataPath);
            }
            if (data.shaders == null)
                throw new InvalidOperationException("URP PostProcessData shader resources are missing.");
            data.shaders.gaussianDepthOfFieldPS = shader;
            data.shaders.bokehDepthOfFieldPS = bokehShader;
            renderer.postProcessData = data;
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(renderer);
        }

        static void ConfigureShadows(UniversalRenderPipelineAsset urp)
        {
            // Keep the last-cascade fade beyond the ground visible at maximum zoom.
            urp.shadowDistance = 52f;
            urp.shadowCascadeCount = 2;
            urp.cascade2Split = .45f;
            EditorUtility.SetDirty(urp);
        }

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
            ConfigureOrthographicDepthOfField();
            URPShaderStrippingSetting stripping =
                GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            if (stripping == null) throw new InvalidOperationException("URP shader stripping settings are missing.");
            // This review build changes Volume settings at runtime. Headless builds can
            // otherwise classify Gaussian DoF as inactive and remove its shader resource.
            stripping.stripUnusedPostProcessingVariants = false;
            UnityEngine.Object globalSettings = AssetDatabase.LoadMainAssetAtPath(
                "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset");
            if (globalSettings == null)
                throw new InvalidOperationException("URP global settings asset is missing.");
            EditorUtility.SetDirty(globalSettings);
            EnsureFolder(ProfileFolder);
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
            focusVolume.enabled = true;

            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1 << 0;
            cameraData.volumeTrigger = player.transform;
            cameraData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.Medium;
            cameraData.dithering = true;
            camera.allowHDR = true;

            urp.msaaSampleCount = 1; // SMAA comparison stays independent of MSAA cost.
            urp.hdrColorBufferPrecision = HDRColorBufferPrecision._64Bits;
            ConfigureShadows(urp);

            Light sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (sun == null) throw new InvalidOperationException("Main directional light is missing.");
            sun.color = new Color(.93f,.96f,1f);
            sun.intensity = 1.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .90f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.34f,.36f,.40f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.28f,.32f,.34f);
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 65f;

            AddLantern(root.transform);

            GameObject homeLightObject = new GameObject("Home Warm Fill");
            homeLightObject.transform.SetParent(root.transform, false);
            homeLightObject.transform.position = new Vector3(-1.6f,1.5f,-1.1f);
            Light homeLight = homeLightObject.AddComponent<Light>();
            homeLight.type = LightType.Point;
            homeLight.color = new Color(1f,.73f,.48f);
            homeLight.intensity = 10f;
            homeLight.range = 7.5f;
            homeLight.shadows = LightShadows.None;

            VisualOptionsMenu optionsMenu = CreateOptionsMenu(hud.transform,
                hud.GetComponent<Topaz.LoopStudy.LoopHud>());
            VisualLookController controller = root.AddComponent<VisualLookController>();
            Ref(controller, "cameraData", cameraData);
            Ref(controller, "painterlyVolume", painterlyVolume);
            Ref(controller, "homeVolume", homeVolume);
            Ref(controller, "focusVolume", focusVolume);
            Ref(controller, "homeLight", homeLight);
            Ref(controller, "sun", sun);
            Ref(controller, "optionsMenu", optionsMenu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            VisualPolishSetup.Configure();
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
            color.postExposure.Override(0f);
            color.contrast.Override(0f);
            color.saturation.Override(10f);
            WhiteBalance balance = Effect<WhiteBalance>(profile);
            balance.temperature.Override(25f);
            Bloom bloom = Effect<Bloom>(profile);
            bloom.threshold.Override(.70f);
            bloom.intensity.Override(1f);
            bloom.scatter.Override(.65f);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.highQualityFiltering.Override(false);
            Vignette vignette = Effect<Vignette>(profile);
            vignette.intensity.Override(0f);
            vignette.smoothness.Override(.55f);
            Dirty(profile);
        }

        static void ConfigureHome(VolumeProfile profile)
        {
            WhiteBalance balance = Effect<WhiteBalance>(profile);
            balance.temperature.Override(60f);
            ColorAdjustments color = Effect<ColorAdjustments>(profile);
            color.postExposure.Override(.06f);
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
            depth.gaussianStart.Override(30f);
            depth.gaussianEnd.Override(40f);
            depth.gaussianMaxRadius.Override(.5f);
            depth.focusDistance.Override(22f);
            depth.aperture.Override(2.8f);
            depth.focalLength.Override(120f);
            depth.bladeCount.Override(6);
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

        [MenuItem("Topaz/Rebuild Graphics Menu")]
        public static void RebuildGraphicsMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("Loop HUD");
            if (canvas == null) throw new InvalidOperationException("Bootstrap HUD is missing.");
            VisualOptionsMenu menu = CreateOptionsMenu(canvas.transform,
                canvas.GetComponent<Topaz.LoopStudy.LoopHud>());
            ConfigureFocus(Profile("Focus Preview"));
            GameObject menus = canvas;
            if (menus.GetComponent<Topaz.Menus.GameMenus>() != null)
                Ref(menus.GetComponent<Topaz.Menus.GameMenus>(), "visualLab", menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Graphics menu rebuilt.");
        }

        static VisualOptionsMenu CreateOptionsMenu(Transform canvas,
            Topaz.LoopStudy.LoopHud hud)
        {
            Transform previous = canvas.Find("Visual Lab") ?? canvas.Find("Graphics");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            RectTransform panel = Box("Graphics", canvas, new Vector2(980f,800f),
                new Vector2(.5f,.5f), Vector2.zero, new Color(.07f,.12f,.15f,.98f));
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Text(panel, "Heading", font, "Graphics", 42,
                new Vector2(0f,-23f), new Vector2(900f,56f), TextAlignmentOptions.Center);
            Text(panel, "Explanation", font, "Changes apply and save automatically.", 21,
                new Vector2(0f,-79f), new Vector2(900f,40f), TextAlignmentOptions.Center);

            TMP_Dropdown zoom = DropdownRow(panel, font, "Camera Zoom", -138f);
            TMP_Dropdown aa = DropdownRow(panel, font, "Anti-aliasing", -236f);
            TMP_Dropdown depth = DropdownRow(panel, font, "Depth of Field", -334f);
            UnityEngine.UI.Toggle bloom = ToggleRow(panel, font, "Bloom", -432f);
            UnityEngine.UI.Toggle ao = ToggleRow(panel, font, "Ambient Occlusion", -530f);
            TMP_Text status = Text(panel, "Status", font, "Changes save automatically.", 19,
                new Vector2(0f,-653f), new Vector2(880f,34f), TextAlignmentOptions.Center);
            UnityEngine.UI.Button reset = Button(panel, "Reset to Default", font,
                new Vector2(-220f,-714f), new Vector2(330f,58f));
            UnityEngine.UI.Button close = Button(panel, "Back", font,
                new Vector2(220f,-714f), new Vector2(330f,58f));

            VisualOptionsMenu menu = canvas.GetComponent<VisualOptionsMenu>();
            if (menu == null) menu = canvas.gameObject.AddComponent<VisualOptionsMenu>();
            Ref(menu, "panel", panel.gameObject);
            Ref(menu, "cameraZoomDropdown", zoom);
            Ref(menu, "antiAliasingDropdown", aa);
            Ref(menu, "depthOfFieldDropdown", depth);
            Ref(menu, "bloomToggle", bloom);
            Ref(menu, "ambientOcclusionToggle", ao);
            Ref(menu, "resetButton", reset);
            Ref(menu, "closeButton", close);
            Ref(menu, "status", status);
            Ref(menu, "loopHud", hud);
            Ref(hud, "visualOptionsPanel", panel.gameObject);
            panel.gameObject.SetActive(false);
            return menu;
        }

        static TMP_Dropdown DropdownRow(Transform parent, TMP_FontAsset font,
            string name, float top)
        {
            RectTransform row = Box(name, parent, new Vector2(880f,80f),
                new Vector2(.5f,1f), new Vector2(0f,top),
                new Color(.16f,.23f,.27f,.96f));
            row.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(row, "Name", font, name, 25, new Vector2(-198f,-19f),
                new Vector2(430f,48f), TextAlignmentOptions.Left);
            var resources = new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
            GameObject control = TMP_DefaultControls.CreateDropdown(resources);
            control.name = name + " Dropdown";
            control.transform.SetParent(row, false);
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f,.5f);
            rect.pivot = new Vector2(1f,.5f);
            rect.anchoredPosition = new Vector2(-24f,0f);
            rect.sizeDelta = new Vector2(340f,54f);
            TMP_Dropdown dropdown = control.GetComponent<TMP_Dropdown>();
            control.GetComponent<UnityEngine.UI.Image>().color = new Color(.23f,.44f,.49f,1f);
            dropdown.captionText.font = font;
            dropdown.captionText.fontSize = 23;
            dropdown.captionText.color = Color.white;
            dropdown.itemText.font = font;
            dropdown.itemText.fontSize = 20;
            dropdown.itemText.color = Color.white;
            dropdown.template.GetComponent<UnityEngine.UI.Image>().color =
                new Color(.10f,.18f,.22f,1f);
            RectTransform template = dropdown.template;
            template.sizeDelta = new Vector2(0f,250f);
            Transform item = template.Find("Viewport/Content/Item");
            if (item == null) throw new InvalidOperationException("TMP dropdown item is missing.");
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f,42f);
            item.Find("Item Background").GetComponent<UnityEngine.UI.Image>().color =
                new Color(.14f,.24f,.28f,1f);
            control.transform.Find("Arrow").GetComponent<UnityEngine.UI.Image>().color = Color.white;
            dropdown.template.gameObject.SetActive(false);
            return dropdown;
        }

        static UnityEngine.UI.Toggle ToggleRow(Transform parent, TMP_FontAsset font,
            string name, float top)
        {
            RectTransform row = Box(name, parent, new Vector2(880f,80f),
                new Vector2(.5f,1f), new Vector2(0f,top),
                new Color(.16f,.23f,.27f,.96f));
            Text(row, "Name", font, name, 25, new Vector2(-198f,-19f),
                new Vector2(430f,48f), TextAlignmentOptions.Left);
            RectTransform square = Box("Checkbox", row, new Vector2(46f,46f),
                new Vector2(.5f,.5f), new Vector2(235f,0f),
                new Color(.07f,.12f,.15f,1f));
            RectTransform mark = Box("Checkmark", square, new Vector2(28f,28f),
                new Vector2(.5f,.5f), Vector2.zero, new Color(.63f,.90f,.80f,1f));
            UnityEngine.UI.Toggle toggle = row.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = row.GetComponent<UnityEngine.UI.Image>();
            toggle.graphic = mark.GetComponent<UnityEngine.UI.Image>();
            toggle.isOn = true;
            return toggle;
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
