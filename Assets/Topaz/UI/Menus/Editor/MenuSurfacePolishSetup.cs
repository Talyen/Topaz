using System.Linq;
using TMPro;
using Topaz.Menus;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Topaz.Editor
{
    /// <summary>Brings selection, pause, options, and graphics into the warm title language.</summary>
    public static class MenuSurfacePolishSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string ThemePath = "Assets/Topaz/UI/Themes/TopazUiTheme.asset";
        const string ButtonPrefabPath = "Assets/Topaz/UI/Prefabs/ActionButton.prefab";
        static readonly Color Dark = new Color32(23, 19, 18, 255);
        static readonly Color Row = new Color32(49, 37, 30, 255);
        static readonly Color Ivory = new Color32(255, 241, 216, 255);
        static readonly Color Supporting = new Color32(224, 198, 163, 255);
        static readonly Color Brass = new Color32(239, 199, 132, 255);

        [MenuItem("Topaz/Polish Desktop Menu Surfaces")]
        public static void Apply()
        {
            SetTheme();
            SetButtonPrefab();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("Loop HUD");
            if (canvas == null) throw new System.InvalidOperationException("Loop HUD is missing.");
            ApplyToCanvas(canvas.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Desktop menu surfaces use the warm refuge palette.");
        }

        internal static void ApplyToCanvas(Transform canvas)
        {
            Transform desktop = canvas.Find("Desktop Menus");
            if (desktop == null) return;
            StyleCard(desktop.Find("Pause Screen/Pause Composition"), .86f);
            StyleCard(desktop.Find("Options Screen/Options Composition"), .9f);
            Transform pause = desktop.Find("Pause Screen/Pause Composition");
            if (pause != null)
            {
                foreach (Button button in pause.GetComponentsInChildren<Button>(true))
                {
                    TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.color = Ivory;
                    Image marker = button.transform.Find("Focus Marker")?.GetComponent<Image>();
                    if (marker != null) marker.color = Brass;
                }
            }

            Transform options = desktop.Find("Options Screen/Options Composition");
            if (options != null)
            {
                foreach (Button button in options.GetComponentsInChildren<Button>(true))
                {
                    StyleRow(button, false);
                    RectTransform label = button.GetComponentInChildren<TMP_Text>(true)?.rectTransform;
                    if (label != null)
                    {
                        label.anchorMin = label.anchorMax = new Vector2(0f, .5f);
                        label.pivot = new Vector2(0f, .5f);
                        label.anchoredPosition = new Vector2(32f, 0f);
                        label.sizeDelta = new Vector2(485f, 60f);
                        TMP_Text text = label.GetComponent<TMP_Text>();
                        text.alignment = TextAlignmentOptions.Left;
                        text.font = TMP_Settings.defaultFontAsset;
                        text.fontSize = 30f;
                    }
                    EnsureDiamond(button.transform);
                }
                foreach (TMP_Text text in options.GetComponentsInChildren<TMP_Text>(true))
                    if (text.GetComponentInParent<Button>() == null)
                        text.color = text.text == "Options" ? Ivory : Supporting;
                GameMenus menus = canvas.GetComponent<GameMenus>();
                TMP_Text info = new SerializedObject(menus).FindProperty("displayInfo")
                    .objectReferenceValue as TMP_Text;
                Button[] rows = options.GetComponentsInChildren<Button>(true);
                TopazResponsiveOptions layout = options.GetComponent<TopazResponsiveOptions>();
                if (layout == null) layout = options.gameObject.AddComponent<TopazResponsiveOptions>();
                layout.Configure(
                    options.GetComponentsInChildren<TMP_Text>(true)
                        .First(value => value.text == "Options" && value.transform.parent == options),
                    options.Find("Rule") as RectTransform,
                    rows.First(value => value.name.StartsWith("Display:")),
                    rows.First(value => value.name.StartsWith("Window size:")),
                    rows.First(value => value.name.StartsWith("UI Scale:")),
                    rows.First(value => value.name == "Graphics"), info,
                    rows.First(value => value.name == "Back"));
            }

            Transform selection = desktop.Find("Character Selection/Iron Selection Card");
            if (selection != null)
            {
                TopazPanelFit selectionFit = selection.GetComponent<TopazPanelFit>();
                if (selectionFit == null)
                    selectionFit = selection.gameObject.AddComponent<TopazPanelFit>();
                selectionFit.Configure(new Vector2(1920f, 1080f));
                Image rail = selection.Find("Menu Rail")?.GetComponent<Image>();
                if (rail != null) rail.color = new Color(Dark.r, Dark.g, Dark.b, .88f);
                foreach (Button button in selection.GetComponentsInChildren<Button>(true))
                    StyleRow(button, button.name.Contains("Delete"));
                foreach (TMP_Text text in selection.GetComponentsInChildren<TMP_Text>(true))
                    if (text.GetComponentInParent<Button>() == null)
                        text.color = text.name == "Selection Status" ?
                            new Color32(255, 188, 170, 255) : Ivory;
            }
            StyleGraphics(canvas.Find("Graphics"));
            StyleInWorldCues(canvas);
        }

        static void StyleInWorldCues(Transform canvas)
        {
            Transform chip = canvas.Find("Interaction Chip");
            Image chipBackground = chip?.GetComponent<Image>();
            if (chipBackground != null)
                chipBackground.color = new Color(Dark.r, Dark.g, Dark.b, .94f);
            Image keycap = chip?.Find("Keycap")?.GetComponent<Image>();
            if (keycap != null) keycap.color = new Color(Row.r, Row.g, Row.b, 1f);
            TMP_Text key = chip?.Find("Keycap/Key")?.GetComponent<TMP_Text>();
            if (key != null) key.color = Brass;
            TMP_Text verb = chip?.Find("Verb")?.GetComponent<TMP_Text>();
            if (verb != null) verb.color = Ivory;
            Transform vitality = canvas.Find("Transient Vitality");
            Image healthBackground = vitality?.GetComponent<Image>();
            if (healthBackground != null)
                healthBackground.color = new Color(Dark.r, Dark.g, Dark.b, .9f);
            TMP_Text health = vitality?.Find("Value")?.GetComponent<TMP_Text>();
            if (health != null) health.color = Ivory;
        }

        static void StyleCard(Transform card, float opacity)
        {
            if (card == null) return;
            Image background = card.GetComponent<Image>();
            if (background != null) background.color = new Color(Dark.r, Dark.g, Dark.b, opacity);
            Image rail = card.Find("Brass Rail")?.GetComponent<Image>();
            if (rail != null) rail.color = Brass;
            foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>(true))
                if (text.fontSize >= 60f) text.color = Ivory;
            foreach (Image line in card.GetComponentsInChildren<Image>(true))
                if (line.name == "Rule")
                    line.color = new Color(Brass.r, Brass.g, Brass.b, .62f);
        }

        static void StyleRow(Button button, bool warning)
        {
            Image image = button.targetGraphic as Image;
            if (image == null) return;
            button.transition = Selectable.Transition.None;
            TopazMenuRowVisual visual = button.GetComponent<TopazMenuRowVisual>();
            if (visual == null) visual = button.gameObject.AddComponent<TopazMenuRowVisual>();
            visual.SetPalette(image,
                new Color(Row.r, Row.g, Row.b, .20f),
                new Color(Row.r, Row.g, Row.b, .75f),
                new Color(Row.r, Row.g, Row.b, .96f));
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = warning ? new Color32(255, 188, 170, 255) : Ivory;
                label.font = TMP_Settings.defaultFontAsset;
                label.raycastTarget = false;
            }
            Outline outline = image.GetComponent<Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = Brass;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            TopazFocusIndicator focus = button.GetComponent<TopazFocusIndicator>();
            if (focus == null) focus = button.gameObject.AddComponent<TopazFocusIndicator>();
            focus.SetOutline(outline);
        }

        static void EnsureDiamond(Transform button)
        {
            Transform existing = button.Find("Cycle Marker");
            Image mark;
            if (existing == null)
            {
                var go = new GameObject("Cycle Marker", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(button, false);
                mark = go.GetComponent<Image>();
            }
            else mark = existing.GetComponent<Image>();
            RectTransform rect = mark.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(-28f, 0f);
            rect.sizeDelta = new Vector2(10f, 10f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            mark.color = Brass;
            mark.raycastTarget = false;
        }

        static void StyleGraphics(Transform panel)
        {
            if (panel == null) return;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            TopazPanelFit fit = panel.GetComponent<TopazPanelFit>();
            if (fit == null) fit = panel.gameObject.AddComponent<TopazPanelFit>();
            fit.Configure(rect.sizeDelta);
            Image background = panel.GetComponent<Image>();
            if (background != null) background.color = new Color(Dark.r, Dark.g, Dark.b, .96f);
            foreach (string name in new[] { "Camera Zoom", "Anti-aliasing", "Depth of Field",
                         "Bloom", "Ambient Occlusion" })
            {
                Transform row = panel.Find(name);
                Image rowImage = row?.GetComponent<Image>();
                if (rowImage != null)
                    rowImage.color = new Color(Row.r, Row.g, Row.b, .94f);
                Image checkbox = row?.Find("Checkbox")?.GetComponent<Image>();
                if (checkbox != null) checkbox.color = Dark;
                Image checkmark = row?.Find("Checkbox/Checkmark")?.GetComponent<Image>();
                if (checkmark != null) checkmark.color = Brass;
            }
            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
                text.color = text.name == "Explanation" || text.name == "Status" ?
                    Supporting : Ivory;
            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
                if (button.name == "Reset to Default" || button.name == "Back")
                    StyleRow(button, false);
            foreach (TMP_Dropdown dropdown in panel.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                Image image = dropdown.targetGraphic as Image;
                if (image != null)
                    image.color = new Color(83f / 255f, 59f / 255f, 40f / 255f, 1f);
                if (dropdown.captionText != null) dropdown.captionText.color = Ivory;
                if (dropdown.itemText != null) dropdown.itemText.color = Ivory;
                Image list = dropdown.template?.GetComponent<Image>();
                if (list != null) list.color = Dark;
                Image item = dropdown.template?.Find("Viewport/Content/Item/Item Background")
                    ?.GetComponent<Image>();
                if (item != null) item.color = Row;
            }
        }

        static void SetTheme()
        {
            TopazUiTheme theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(ThemePath);
            if (theme == null) throw new System.InvalidOperationException("UI theme is missing.");
            var data = new SerializedObject(theme);
            ColorField(data, "backdrop", Dark);
            ColorField(data, "panel", new Color32(41, 33, 29, 255));
            ColorField(data, "raised", Row);
            ColorField(data, "text", Ivory);
            ColorField(data, "mutedText", Supporting);
            ColorField(data, "copper", Brass);
            ColorField(data, "seaGlass", new Color32(170, 181, 158, 255));
            ColorField(data, "warning", new Color32(255, 188, 170, 255));
            ColorField(data, "parchment", new Color32(231, 213, 180, 255));
            ColorField(data, "ink", new Color32(55, 39, 31, 255));
            ColorField(data, "iron", new Color32(51, 42, 36, 255));
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ColorField(SerializedObject data, string name, Color value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.colorValue = value;
        }

        static void SetButtonPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ButtonPrefabPath);
            try
            {
                Button button = root.GetComponent<Button>();
                Image image = button?.targetGraphic as Image;
                if (image != null) image.color = Row;
                TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.font = TMP_Settings.defaultFontAsset;
                    label.color = Ivory;
                }
                Outline outline = image?.GetComponent<Outline>();
                if (outline != null) outline.effectColor = Brass;
                PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
