using System;
using TMPro;
using Topaz.Audio;
using Topaz.VisualStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Topaz.Editor
{
    /// <summary>Adds the audio page to the existing Options detail panel and routes authored sources.</summary>
    public static class AudioOptionsSetup
    {
        static readonly Color Ink = new Color32(23, 19, 18, 255);
        static readonly Color Row = new Color32(49, 37, 30, 255);
        static readonly Color Ivory = new Color32(255, 241, 216, 255);
        static readonly Color Brass = new Color32(239, 199, 132, 255);

        [MenuItem("Topaz/Apply Audio Options")]
        public static void Apply()
        {
            RoutePrefab("Assets/Topaz/Player/Prefabs/Player.prefab", .45f);
            RoutePrefab("Assets/Topaz/Gameplay/Combat/Prefabs/EnemyCombatant CryptRogue.prefab", .4f);
            Scene crypt = EditorSceneManager.OpenScene(
                "Assets/Topaz/World/Scenes/Crypt.unity", OpenSceneMode.Single);
            AudioSource cryptSource = GameObject.Find("Home Crypt").GetComponent<AudioSource>();
            ConfigureOutput(cryptSource, TopazAudioOutput.Category.Ambience, .14f);
            EditorSceneManager.MarkSceneDirty(crypt);
            EditorSceneManager.SaveScene(crypt);

            Scene home = EditorSceneManager.OpenScene(
                "Assets/Topaz/World/Scenes/Bootstrap.unity", OpenSceneMode.Single);
            Transform canvas = GameObject.Find("Loop HUD").transform;
            VisualOptionsMenu menu = canvas.GetComponent<VisualOptionsMenu>();
            Transform panel = canvas.Find("Graphics");
            if (menu == null || panel == null) throw new InvalidOperationException("Options panel missing");
            if (canvas.GetComponent<TopazAudioSettings>() == null)
                canvas.gameObject.AddComponent<TopazAudioSettings>();
            AddToPanel(panel, menu);
            Transform options = canvas.Find("Desktop Menus/Options Screen/Options Composition/Graphics");
            if (options != null)
            {
                TMP_Text label = options.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "Graphics & Audio";
            }
            EditorSceneManager.MarkSceneDirty(home);
            EditorSceneManager.SaveScene(home);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Audio options and source categories are ready.");
        }

        static void RoutePrefab(string path, float volume)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            AudioSource source = root.GetComponent<AudioSource>();
            if (source == null) throw new InvalidOperationException("Audio source missing: " + path);
            ConfigureOutput(source, TopazAudioOutput.Category.Effects, volume);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void ConfigureOutput(AudioSource source, TopazAudioOutput.Category category, float volume)
        {
            TopazAudioOutput output = source.GetComponent<TopazAudioOutput>();
            if (output == null) output = source.gameObject.AddComponent<TopazAudioOutput>();
            var data = new SerializedObject(output);
            data.FindProperty("category").enumValueIndex = (int)category;
            data.FindProperty("baseVolume").floatValue = volume;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AddToPanel(Transform panel, VisualOptionsMenu menu)
        {
            if (panel.Find("Audio Tab") != null) return;
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            panel.Find("Heading").GetComponent<TMP_Text>().text = "Settings";
            Button graphics = Button(panel, "Graphics Tab", "Graphics", font, -240f);
            Button audio = Button(panel, "Audio Tab", "Audio", font, 240f);
            string[] graphicsNames = { "Camera Zoom", "Anti-aliasing", "Depth of Field",
                "Bloom", "Ambient Occlusion" };
            var graphicsRows = new GameObject[graphicsNames.Length];
            float[] tops = { -190f, -282f, -374f, -466f, -558f };
            for (int i = 0; i < graphicsNames.Length; i++)
            {
                RectTransform rect = panel.Find(graphicsNames[i]) as RectTransform;
                if (rect == null) throw new InvalidOperationException("Graphics row missing: " + graphicsNames[i]);
                rect.anchoredPosition = new Vector2(0f, tops[i]);
                graphicsRows[i] = rect.gameObject;
            }
            var audioRows = new GameObject[5];
            Slider master = SliderRow(panel, font, "Master Volume", tops[0]);
            Slider music = SliderRow(panel, font, "Music Volume", tops[1]);
            Slider ambience = SliderRow(panel, font, "Ambient Volume", tops[2]);
            Slider effects = SliderRow(panel, font, "Sound Effects", tops[3]);
            Toggle mute = ToggleRow(panel, font, "Mute in Background", tops[4]);
            audioRows[0] = master.transform.parent.gameObject;
            audioRows[1] = music.transform.parent.gameObject;
            audioRows[2] = ambience.transform.parent.gameObject;
            audioRows[3] = effects.transform.parent.gameObject;
            audioRows[4] = mute.gameObject;
            foreach (GameObject row in audioRows) row.SetActive(false);
            var data = new SerializedObject(menu);
            Set(data, "graphicsTabButton", graphics);
            Set(data, "audioTabButton", audio);
            SetArray(data, "graphicsRows", graphicsRows);
            SetArray(data, "audioRows", audioRows);
            Set(data, "masterSlider", master);
            Set(data, "musicSlider", music);
            Set(data, "ambienceSlider", ambience);
            Set(data, "effectsSlider", effects);
            Set(data, "muteInBackgroundToggle", mute);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static Button Button(Transform parent, string name, string label, TMP_FontAsset font, float x)
        {
            RectTransform rect = Box(name, parent, new Vector2(390f, 52f),
                new Vector2(x, -126f), Brass);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Text(rect, label, font, 25f, new Vector2(0f, -4f), new Vector2(360f, 44f));
            return button;
        }

        static Slider SliderRow(Transform parent, TMP_FontAsset font, string name, float top)
        {
            RectTransform row = Box(name, parent, new Vector2(780f, 76f),
                new Vector2(0f, top), Row);
            Text(row, name, font, 25f, new Vector2(-180f, -14f), new Vector2(360f, 46f));
            Sprite background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var resources = new DefaultControls.Resources { background = background, knob = knob,
                standard = background };
            GameObject control = DefaultControls.CreateSlider(resources);
            control.name = name + " Slider";
            control.transform.SetParent(row, false);
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, .5f);
            rect.pivot = new Vector2(1f, .5f);
            rect.anchoredPosition = new Vector2(-28f, 0f);
            rect.sizeDelta = new Vector2(320f, 38f);
            Slider slider = control.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            Image fill = control.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fill != null) fill.color = Brass;
            Image handle = control.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
            if (handle != null) handle.color = Ivory;
            return slider;
        }

        static Toggle ToggleRow(Transform parent, TMP_FontAsset font, string name, float top)
        {
            RectTransform row = Box(name, parent, new Vector2(780f, 76f),
                new Vector2(0f, top), Row);
            Text(row, name, font, 25f, new Vector2(-180f, -14f), new Vector2(360f, 46f));
            RectTransform box = Box("Checkbox", row, new Vector2(42f, 42f),
                new Vector2(260f, 0f), Ink);
            RectTransform check = Box("Checkmark", box, new Vector2(25f, 25f),
                Vector2.zero, Brass);
            Toggle toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = true;
            return toggle;
        }

        static RectTransform Box(string name, Transform parent, Vector2 size,
            Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        static TMP_Text Text(Transform parent, string value, TMP_FontAsset font,
            float size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(value + " Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = Ivory;
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static void Set(SerializedObject data, string field, UnityEngine.Object value) =>
            data.FindProperty(field).objectReferenceValue = value;

        static void SetArray(SerializedObject data, string field, GameObject[] values)
        {
            SerializedProperty property = data.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
