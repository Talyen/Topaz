using System;
using System.Linq;
using TMPro;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using Topaz.Menus;
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
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";

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
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            Transform oldRoot = canvas.transform.Find("Desktop Menus");
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            var root = new GameObject("Desktop Menus", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject title = FullPanel(root.transform, "Title Screen", .77f);
            RectTransform titleCard = Card(title.transform, new Vector2(780,640));
            Label(titleCard, "TOPAZ", 78, -45, 110, new Color(.87f,.96f,.95f));
            Label(titleCard, "An authored world of steel, craft, and survival", 25,
                -150, 55, new Color(.75f,.85f,.84f));
            UnityEngine.UI.Button continueButton = Button(titleCard, "Continue", -250, 560);
            UnityEngine.UI.Button titleOptions = Button(titleCard, "Options", -345, 560);
            UnityEngine.UI.Button titleQuit = Button(titleCard, "Quit", -440, 560);
            Label(titleCard, "WASD move  •  Mouse aim  •  Esc pauses", 21,
                -555, 35, new Color(.64f,.75f,.75f));

            GameObject pause = FullPanel(root.transform, "Pause Screen", .64f);
            RectTransform pauseCard = Card(pause.transform, new Vector2(740,650));
            Label(pauseCard, "Paused", 62, -48, 90, Color.white);
            UnityEngine.UI.Button resume = Button(pauseCard, "Resume", -185, 540);
            UnityEngine.UI.Button pauseOptions = Button(pauseCard, "Options", -275, 540);
            UnityEngine.UI.Button mainMenu = Button(pauseCard, "Main Menu", -365, 540);
            UnityEngine.UI.Button pauseQuit = Button(pauseCard, "Quit", -455, 540);

            GameObject options = FullPanel(root.transform, "Options Screen", .65f);
            RectTransform optionsCard = Card(options.transform, new Vector2(880,680));
            Label(optionsCard, "Options", 59, -34, 85, Color.white);
            Label(optionsCard, "Display and graphics preferences save automatically.", 24,
                -115, 50, new Color(.77f,.87f,.85f));
            UnityEngine.UI.Button displayMode = Button(optionsCard, "Display: Borderless native", -195, 670);
            UnityEngine.UI.Button windowSize = Button(optionsCard, "Window size: 1600 × 900", -285, 670);
            UnityEngine.UI.Button visualLabButton = Button(optionsCard, "Graphics", -375, 670);
            TMP_Text displayInfo = Label(optionsCard,
                "Uses the display's native resolution. Choose Window size to switch to a window.",
                21, -463, 56, new Color(.69f,.80f,.79f));
            UnityEngine.UI.Button back = Button(optionsCard, "Back  •  Esc", -555, 670);

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
            Ref(menus, "pauseOptionsButton", pauseOptions);
            Ref(menus, "mainMenuButton", mainMenu);
            Ref(menus, "pauseQuitButton", pauseQuit);
            Ref(menus, "displayModeButton", displayMode);
            Ref(menus, "windowSizeButton", windowSize);
            Ref(menus, "visualLabButton", visualLabButton);
            Ref(menus, "optionsBackButton", back);
            Ref(menus, "displayInfo", displayInfo);
            Ref(player.GetComponent<WorldSession>(), "menus", menus);

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
            image.color = new Color(.04f,.09f,.12f,opacity);
            image.raycastTarget = true;
            return panel.gameObject;
        }

        static RectTransform Card(Transform parent, Vector2 size)
        {
            RectTransform card = Rect("Card", parent, new Vector2(.5f,.5f),
                new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, size);
            var image = card.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.08f,.15f,.18f,.97f);
            image.raycastTarget = true;
            return card;
        }

        static UnityEngine.UI.Button Button(Transform parent, string name, float top, float width)
        {
            RectTransform rect = Rect(name, parent, new Vector2(.5f,1f),
                new Vector2(.5f,1f), new Vector2(.5f,1f), new Vector2(0f,top),
                new Vector2(width,70f));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.22f,.47f,.52f,1f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            TMP_Text label = Label(rect, name, 30, -12, 50, Color.white);
            label.raycastTarget = false;
            return button;
        }

        static TMP_Text Label(Transform parent, string value, float fontSize,
            float top, float height, Color color)
        {
            RectTransform rect = Rect("Text", parent, new Vector2(.5f,1f),
                new Vector2(.5f,1f), new Vector2(.5f,1f), new Vector2(0f,top),
                new Vector2(720f,height));
            TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
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
