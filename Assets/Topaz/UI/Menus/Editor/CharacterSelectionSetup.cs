using System;
using System.Linq;
using TMPro;
using Topaz.AnimationStudy;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using Topaz.Menus;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Topaz.Editor
{
    /// <summary>Adds Character and World selection to the existing Bootstrap menus.</summary>
    public static class CharacterSelectionSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string ButtonPath = "Assets/Topaz/UI/Prefabs/ActionButton.prefab";

        [MenuItem("Topaz/Build Character And World Selection")]
        public static void Configure()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("Loop HUD");
            GameObject player = GameObject.Find("Player");
            if (canvas == null || player == null)
                throw new InvalidOperationException("Bootstrap UI or player is missing.");
            GameMenus menus = canvas.GetComponent<GameMenus>();
            if (menus == null) throw new InvalidOperationException("Build desktop menus first.");
            Configure(scene, canvas, player, menus);
        }

        internal static void Configure(Scene scene, GameObject canvas, GameObject player, GameMenus menus)
        {
            var theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(
                "Assets/Topaz/UI/Themes/TopazUiTheme.asset");
            if (theme == null) throw new InvalidOperationException("Topaz UI theme is missing.");
            Transform desktop = canvas.transform.Find("Desktop Menus");
            Transform titleCard = desktop?.Find("Title Screen/Title Composition");
            if (titleCard == null) throw new InvalidOperationException("Title composition is missing.");

            PlayerAppearance appearance = ConfigureAppearance(player);
            Transform previous = desktop.Find("Character Selection");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            GameObject root = Stretch("Character Selection", desktop);
            var shade = root.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0f);
            shade.raycastTarget = false;
            RectTransform card = Rect("Iron Selection Card", root.transform,
                new Vector2(0f, 0f), new Vector2(1920f, 1080f));
            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0f, 0f, 0f, 0f);
            cardImage.raycastTarget = false;
            RectTransform rail = Rect("Menu Rail", card, new Vector2(-610f, 0f),
                new Vector2(700f, 1080f));
            var railImage = rail.gameObject.AddComponent<Image>();
            railImage.color = new Color(theme.Iron.r, theme.Iron.g, theme.Iron.b, .82f);
            railImage.raycastTarget = false;

            GameObject characters = Page("Characters", card, theme,
                "Choose Character", "Pick an adventurer or create a new one.");
            ScrollRect characterScroll = List("Character List", characters.transform,
                new Vector2(-610f, -5f), new Vector2(610f, 545f));
            Button newCharacter = Action("New Character", characters.transform,
                new Vector2(-760f, -440f), new Vector2(300f, 72f), theme);
            Button charactersBack = Action("Back", characters.transform,
                new Vector2(-440f, -440f), new Vector2(220f, 72f), theme);

            GameObject looks = Page("Looks", card, theme,
                "Choose Your Look", "Appearance only. Every look plays the same.");
            ScrollRect lookScroll = List("Look List", looks.transform,
                new Vector2(-610f, -5f), new Vector2(610f, 545f));
            Button rotate = Action("Rotate", looks.transform,
                new Vector2(355f, -430f), new Vector2(190f, 62f), theme);
            Button createCharacter = Action("Create Character", looks.transform,
                new Vector2(-760f, -440f), new Vector2(300f, 72f), theme);
            Button looksBack = Action("Back", looks.transform,
                new Vector2(-440f, -440f), new Vector2(220f, 72f), theme);

            GameObject worlds = Page("Worlds", card, theme,
                "Choose World", "Any Character can enter any World.");
            ScrollRect worldScroll = List("World List", worlds.transform,
                new Vector2(-610f, -5f), new Vector2(610f, 545f));
            Button newWorld = Action("New World", worlds.transform,
                new Vector2(-610f, -345f), new Vector2(340f, 72f), theme);
            TMP_Text worldChoice = Text("World Choice", worlds.transform,
                "Start a fresh World", new Vector2(350f, 350f),
                new Vector2(560f, 80f), 30f, theme.Text, theme);
            Button enterWorld = Action("Enter World", worlds.transform,
                new Vector2(-760f, -440f), new Vector2(300f, 72f), theme);
            Button worldsBack = Action("Back", worlds.transform,
                new Vector2(-440f, -440f), new Vector2(220f, 72f), theme);
            TMP_Text error = Text("Selection Status", card,
                "", new Vector2(-610f, -510f), new Vector2(650f, 43f),
                21f, theme.Warning, theme);
            MainMenuStage stage = MainMenuStageSetup.Configure(scene, player, appearance);

            CharacterWorldMenu selection = canvas.GetComponent<CharacterWorldMenu>();
            if (selection == null) selection = canvas.AddComponent<CharacterWorldMenu>();
            Ref(selection, "menus", menus);
            Ref(selection, "session", player.GetComponent<WorldSession>());
            Ref(selection, "stage", stage);
            Ref(selection, "root", root);
            Ref(selection, "charactersPage", characters);
            Ref(selection, "looksPage", looks);
            Ref(selection, "worldsPage", worlds);
            Ref(selection, "characterList", characterScroll.content);
            Ref(selection, "lookList", lookScroll.content);
            Ref(selection, "worldList", worldScroll.content);
            Ref(selection, "characterScroll", characterScroll);
            Ref(selection, "lookScroll", lookScroll);
            Ref(selection, "worldScroll", worldScroll);
            Ref(selection, "buttonPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath));
            Ref(selection, "newCharacterButton", newCharacter);
            Ref(selection, "createCharacterButton", createCharacter);
            Ref(selection, "rotateButton", rotate);
            Ref(selection, "newWorldButton", newWorld);
            Ref(selection, "enterWorldButton", enterWorld);
            Ref(selection, "charactersBackButton", charactersBack);
            Ref(selection, "looksBackButton", looksBack);
            Ref(selection, "worldsBackButton", worldsBack);
            Ref(selection, "worldChoiceLabel", worldChoice);
            Ref(selection, "errorLabel", error);

            Transform priorPlay = titleCard.Find("Play");
            if (priorPlay != null) UnityEngine.Object.DestroyImmediate(priorPlay.gameObject);
            Button play = Action("Play", titleCard,
                new Vector2(0f, -440f), new Vector2(490f, 70f), theme);
            RectTransform playRect = play.GetComponent<RectTransform>();
            playRect.anchorMin = playRect.anchorMax = new Vector2(.5f, 1f);
            playRect.pivot = new Vector2(.5f, 1f);
            playRect.anchoredPosition = new Vector2(0f, -440f);
            Image playImage = play.GetComponent<Image>();
            playImage.color = new Color(theme.Iron.r, theme.Iron.g, theme.Iron.b, .94f);
            TMP_Text playText = play.GetComponentInChildren<TMP_Text>();
            if (playText != null)
            {
                if (theme.DisplayFont != null) playText.font = theme.DisplayFont;
                playText.fontSize = 42f;
                playText.color = theme.Text;
            }
            Shift(titleCard.Find("Options"), -538f);
            Shift(titleCard.Find("Quit"), -636f);
            Transform priorStatus = titleCard.Find("Title Status");
            if (priorStatus != null) UnityEngine.Object.DestroyImmediate(priorStatus.gameObject);
            TMP_Text titleStatus = Text("Title Status", titleCard, "",
                new Vector2(0f, -760f), new Vector2(520f, 100f), 22f,
                theme.Warning, theme);
            RectTransform statusRect = titleStatus.rectTransform;
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(.5f, 1f);
            statusRect.pivot = new Vector2(.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -760f);

            Ref(menus, "session", player.GetComponent<WorldSession>());
            Ref(menus, "selection", selection);
            Ref(menus, "menuStage", stage);
            Ref(menus, "playButton", play);
            Ref(menus, "titleStatus", titleStatus);
            Ref(player.GetComponent<WorldSession>(), "appearance", appearance);
            Image titleShade = desktop.Find("Title Screen")?.GetComponent<Image>();
            if (titleShade != null)
            {
                titleShade.color = new Color(0f, 0f, 0f, 0f);
                titleShade.raycastTarget = false;
            }
            RectTransform titleRail = titleCard.GetComponent<RectTransform>();
            titleRail.sizeDelta = new Vector2(700f, 1080f);
            titleRail.anchoredPosition = Vector2.zero;
            Image titleBackground = titleCard.GetComponent<Image>();
            if (titleBackground != null)
                titleBackground.color = new Color(theme.Iron.r, theme.Iron.g, theme.Iron.b, .82f);
            root.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Character and World selection is ready.");
        }

        static PlayerAppearance ConfigureAppearance(GameObject player)
        {
            FeelStudyPlayer movement = player.GetComponent<FeelStudyPlayer>();
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            Transform facing = player.transform.Find("Facing Visual");
            GameObject rogue = facing?.GetComponentInChildren<CharacterVisual>(true)?.gameObject;
            if (rogue == null) throw new InvalidOperationException("Animated Rogue visual is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Topaz/Characters/Animation/Controllers/Player.controller");
            if (controller == null) throw new InvalidOperationException("Player Animator Controller is missing.");
            PlayerAppearance appearance = player.GetComponent<PlayerAppearance>();
            if (appearance == null) appearance = player.AddComponent<PlayerAppearance>();
            Ref(appearance, "visualRoot", facing);
            Ref(appearance, "rogueVisual", rogue);
            Ref(appearance, "movement", movement);
            Ref(appearance, "combat", combat);
            Ref(appearance, "playerController", controller);
            string[] ids = CharacterLooks.All;
            string[] models = { "Rogue", "Rogue Hooded", "Knight", "Ranger", "Mage", "Barbarian" };
            var objectData = new SerializedObject(appearance);
            SerializedProperty array = objectData.FindProperty("looks");
            array.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                string path = "Assets/Topaz/Characters/Prefabs/Visuals/" + models[i] + " Player.prefab";
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Avatar avatar = model != null ? model.GetComponent<CharacterVisual>()?.Animator.avatar : null;
                Material material = model != null ? model.GetComponent<CharacterVisual>()?.BodyRenderer.sharedMaterial : null;
                if (model == null || avatar == null || material == null)
                    throw new InvalidOperationException("Character asset is incomplete: " + path);
                SerializedProperty entry = array.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = ids[i];
                entry.FindPropertyRelative("model").objectReferenceValue = model;
                entry.FindPropertyRelative("avatar").objectReferenceValue = avatar;
                entry.FindPropertyRelative("material").objectReferenceValue = material;
            }
            objectData.ApplyModifiedPropertiesWithoutUndo();
            return appearance;
        }

        static GameObject Page(string name, RectTransform parent, TopazUiTheme theme,
            string heading, string explanation)
        {
            RectTransform page = Rect(name, parent, Vector2.zero, parent.sizeDelta);
            Text("Heading", page, heading, new Vector2(-610f, 390f),
                new Vector2(690f, 94f), 58f, theme.Text, theme);
            Text("Description", page, explanation, new Vector2(-610f, 312f),
                new Vector2(690f, 52f), 24f, theme.MutedText, theme);
            return page.gameObject;
        }

        static ScrollRect List(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform scrollRoot = Rect(name, parent, position, size);
            var background = scrollRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, .15f);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            GameObject viewport = Stretch("Viewport", scrollRoot);
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, .01f);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            GameObject content = Stretch("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var group = content.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(14, 14, 12, 12);
            group.spacing = 9f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            return scroll;
        }

        static Button Action(string label, Transform parent, Vector2 position,
            Vector2 size, TopazUiTheme theme)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if (prefab == null) throw new InvalidOperationException("ActionButton prefab is missing.");
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not create UI button.");
            instance.name = label;
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var button = instance.GetComponent<Button>();
            TMP_Text text = instance.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = label;
                text.fontSize = 30f;
            }
            UiDesignSystemSetup.StyleSelectable(button, theme);
            return button;
        }

        static TMP_Text Text(string name, Transform parent, string value, Vector2 position,
            Vector2 size, float fontSize, Color color, TopazUiTheme theme)
        {
            RectTransform rect = Rect(name, parent, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = fontSize > 45f && theme.DisplayFont != null
                ? theme.DisplayFont : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static GameObject Stretch(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return obj;
        }

        static void Shift(Transform target, float y)
        {
            if (target == null) throw new InvalidOperationException("Title button is missing.");
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            if (property == null) throw new InvalidOperationException("Missing " + field + " on " + target.name);
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
