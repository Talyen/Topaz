using System;
using System.Linq;
using TMPro;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using Topaz.Menus;
using Topaz.UI;
using Topaz.VisualStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Author the desktop title, pause, and options panels in uGUI.</summary>
    public static class MenuSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        static TopazUiTheme theme;

        [MenuItem("Topaz/Build Desktop Menus")]
        public static void Configure()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("Loop HUD");
            GameObject player = GameObject.Find("Player");
            Camera camera = Camera.main;
            if (canvas == null || player == null || camera == null)
                throw new InvalidOperationException("The Bootstrap HUD, player, or camera is missing.");
            LoopHud hud = canvas.GetComponent<LoopHud>();
            VisualOptionsMenu visualLab = canvas.GetComponent<VisualOptionsMenu>();
            FeelStudyCamera cameraRig = camera.GetComponent<FeelStudyCamera>();
            if (hud == null || visualLab == null || cameraRig == null)
                throw new InvalidOperationException("Build the Visual Study before desktop menus.");
            theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(
                "Assets/Topaz/UI/Themes/TopazUiTheme.asset");
            if (theme == null) throw new InvalidOperationException("Create the Topaz UI theme first.");
            Transform oldRoot = canvas.transform.Find("Desktop Menus");
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            var root = new GameObject("Desktop Menus", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject title = FullPanel(root.transform, "Title Screen", .24f);
            RectTransform titleCard = Card(title.transform, "Title Composition",
                new Vector2(650, 900), new Vector2(0f, .5f), new Vector2(80f, 0f), .88f);
            Label(titleCard, "TOPAZ", 108, -108, 142, theme.Copper);
            Rule(titleCard, -263f, 440f);
            UnityEngine.UI.Button continueButton = Button(titleCard, "Continue", -342, 490);
            UnityEngine.UI.Button titleOptions = Button(titleCard, "Options", -440, 490);
            UnityEngine.UI.Button titleQuit = Button(titleCard, "Quit", -538, 490);

            GameObject pause = FullPanel(root.transform, "Pause Screen", .38f);
            RectTransform pauseCard = Card(pause.transform, "Pause Composition",
                new Vector2(520, 830), new Vector2(1f, .5f), new Vector2(-78f, 0f), .84f);
            Label(pauseCard, "Paused", 67, -100, 90, theme.Text);
            Rule(pauseCard, -214f, 390f);
            UnityEngine.UI.Button resume = Button(pauseCard, "Resume", -265, 390, true);
            UnityEngine.UI.Button pauseBackpack = Button(pauseCard, "Backpack", -355, 390, true);
            UnityEngine.UI.Button pauseOptions = Button(pauseCard, "Options", -445, 390, true);
            UnityEngine.UI.Button mainMenu = Button(pauseCard, "Main Menu", -535, 390, true);
            UnityEngine.UI.Button pauseQuit = Button(pauseCard, "Quit", -625, 390, true);

            GameObject options = FullPanel(root.transform, "Options Screen", .48f);
            RectTransform optionsCard = Card(options.transform, "Options Composition",
                new Vector2(720, 850), new Vector2(1f, .5f), new Vector2(-80f, 0f), .92f);
            Label(optionsCard, "Options", 66, -70, 92, theme.Text);
            Rule(optionsCard, -174f, 550f);
            UnityEngine.UI.Button displayMode = Button(optionsCard, "Display: Borderless native", -220, 560);
            UnityEngine.UI.Button windowSize = Button(optionsCard, "Window size: 1600 × 900", -320, 560);
            UnityEngine.UI.Button visualLabButton = Button(optionsCard, "Graphics", -420, 560);
            TMP_Text displayInfo = Label(optionsCard,
                "Uses the display's native resolution.", 22,
                -530, 65, theme.MutedText);
            displayInfo.rectTransform.sizeDelta = new Vector2(550f, 82f);
            displayInfo.textWrappingMode = TextWrappingModes.Normal;
            UnityEngine.UI.Button back = Button(optionsCard, "Back", -645, 560);

            GameMenus menus = canvas.GetComponent<GameMenus>();
            if (menus == null) menus = canvas.AddComponent<GameMenus>();
            Ref(menus, "loopHud", hud);
            Ref(menus, "visualLab", visualLab);
            Ref(menus, "cameraRig", cameraRig);
            Ref(menus, "titlePanel", title);
            Ref(menus, "pausePanel", pause);
            Ref(menus, "optionsPanel", options);
            Ref(menus, "continueButton", continueButton);
            Ref(menus, "titleOptionsButton", titleOptions);
            Ref(menus, "titleQuitButton", titleQuit);
            Ref(menus, "resumeButton", resume);
            Ref(menus, "pauseBackpackButton", pauseBackpack);
            Ref(menus, "pauseOptionsButton", pauseOptions);
            Ref(menus, "mainMenuButton", mainMenu);
            Ref(menus, "pauseQuitButton", pauseQuit);
            Ref(menus, "displayModeButton", displayMode);
            Ref(menus, "windowSizeButton", windowSize);
            Ref(menus, "visualLabButton", visualLabButton);
            Ref(menus, "optionsBackButton", back);
            Ref(menus, "displayInfo", displayInfo);
            Ref(player.GetComponent<WorldSession>(), "menus", menus);

            CharacterSelectionSetup.Configure(scene, canvas, player, menus);

            camera.orthographicSize = 7.2f;
            Float(cameraRig, "minimumZoom", 5.5f);
            Float(cameraRig, "maximumZoom", 12f);
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Desktop title, pause, and options menus are ready.");
        }

        static GameObject FullPanel(Transform parent, string name, float opacity)
        {
            RectTransform panel = Rect(name, parent, Vector2.zero, Vector2.one,
                new Vector2(.5f,.5f), Vector2.zero, Vector2.zero);
            var image = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(theme.Backdrop.r, theme.Backdrop.g, theme.Backdrop.b, opacity);
            image.raycastTarget = true;
            return panel.gameObject;
        }

        static RectTransform Card(Transform parent, string name, Vector2 size,
            Vector2 anchor, Vector2 position, float opacity)
        {
            RectTransform card = Rect(name, parent, anchor,
                anchor, anchor, position, size);
            var image = card.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(theme.Backdrop.r, theme.Backdrop.g, theme.Backdrop.b, opacity);
            image.raycastTarget = true;
            var rail = Rect("Brass Rail", card, new Vector2(0f,0f),
                new Vector2(0f,1f), new Vector2(0f,.5f), Vector2.zero, new Vector2(5f,0f));
            var railImage = rail.gameObject.AddComponent<UnityEngine.UI.Image>();
            railImage.color = theme.Copper;
            railImage.raycastTarget = false;
            return card;
        }

        static void Rule(Transform parent, float top, float width)
        {
            RectTransform rect = Rect("Rule", parent, new Vector2(.5f,1f),
                new Vector2(.5f,1f), new Vector2(.5f,1f), new Vector2(0f,top),
                new Vector2(width,2f));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(theme.Copper.r, theme.Copper.g, theme.Copper.b, .55f);
            image.raycastTarget = false;
        }

        static UnityEngine.UI.Button Button(Transform parent, string name, float top, float width,
            bool minimal = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Topaz/UI/Prefabs/ActionButton.prefab");
            RectTransform rect = prefab != null
                ? ((GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)).GetComponent<RectTransform>()
                : Rect(name, parent, new Vector2(.5f,1f),
                    new Vector2(.5f,1f), new Vector2(.5f,1f), new Vector2(0f,top),
                    new Vector2(width,70f));
            rect.name = name;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(width, 70f);
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            if (image == null) image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.GetComponent<UnityEngine.UI.Button>();
            if (button == null) button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            TMP_Text label = rect.GetComponentInChildren<TMP_Text>();
            if (label == null) label = Label(rect, name, 31, -12, 50, theme.Text);
            label.text = name;
            label.fontSize = 31f;
            label.color = theme.Text;
            label.raycastTarget = false;
            UiDesignSystemSetup.StyleSelectable(button, theme);
            if (parent.name == "Title Composition")
            {
                image.color = new Color(theme.Iron.r, theme.Iron.g, theme.Iron.b, .94f);
                if (theme.DisplayFont != null) label.font = theme.DisplayFont;
                label.fontSize = 42f;
            }
            if (minimal)
            {
                image.color = new Color(0f, 0f, 0f, .01f);
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                label.font = theme.DisplayFont != null ? theme.DisplayFont : label.font;
                label.fontSize = 39f;
                label.alignment = TextAlignmentOptions.Left;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
                labelRect.pivot = new Vector2(0f, 1f);
                labelRect.anchoredPosition = new Vector2(34f, -8f);
                labelRect.sizeDelta = new Vector2(width - 48f, 58f);
                RectTransform markerRect = Rect("Focus Marker", rect,
                    new Vector2(0f, .5f), new Vector2(0f, .5f),
                    new Vector2(0f, .5f), new Vector2(7f, 0f), new Vector2(5f, 46f));
                var marker = markerRect.gameObject.AddComponent<UnityEngine.UI.Image>();
                marker.color = theme.Copper;
                marker.raycastTarget = false;
                button.GetComponent<TopazFocusIndicator>().SetMarker(marker);
            }
            return button;
        }

        static TMP_Text Label(Transform parent, string value, float fontSize,
            float top, float height, Color color)
        {
            RectTransform rect = Rect("Text", parent, new Vector2(.5f,1f),
                new Vector2(.5f,1f), new Vector2(.5f,1f), new Vector2(0f,top),
                new Vector2(720f,height));
            TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = fontSize >= 55f && theme.DisplayFont != null
                ? theme.DisplayFont : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.text = value;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Float(UnityEngine.Object target, string name, float value)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            property.floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
