using System;
using TMPro;
using Topaz.LoopStudy;
using Topaz.CombatStudy;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Topaz.Editor
{
    /// <summary>Author the selected title, journal, and minimal play-screen direction.</summary>
    public static class UiDesignSystemSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string ThemePath = "Assets/Topaz/UI/Themes/TopazUiTheme.asset";
        const string PrefabFolder = "Assets/Topaz/UI/Prefabs";
        const string JournalArtPath = "Assets/Topaz/UI/Art/JournalBackground.png";
        const string WoodArtPath = "Assets/Topaz/UI/Art/WoodJournal.png";
        const string SourceFontPath =
            "Assets/ThirdParty/Fonts/CormorantGaramond/CormorantGaramond-wght.ttf";
        const string DisplayFontPath = "Assets/Topaz/UI/Fonts/CormorantGaramond SDF.asset";

        [MenuItem("Topaz/Apply UI Design System")]
        public static void Apply()
        {
            TopazUiTheme theme = GetOrCreateTheme();
            PrepareJournalArt();
            PrepareSprite(WoodArtPath);
            SetWoodJournalIcon();
            PrepareDisplayFont(theme);
            CreatePrefabs(theme);
            MenuSetup.Configure();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(ThemePath);
            GameObject canvas = GameObject.Find("Loop HUD");
            if (canvas == null || canvas.GetComponent<Canvas>() == null)
                throw new InvalidOperationException("The Bootstrap uGUI Canvas is missing.");
            ApplyToCanvas(canvas.transform, theme);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Grounded adventure UI authored; exploration overlay removed.");
        }

        public static void ApplyToCanvas(Transform canvas, TopazUiTheme theme)
        {
            if (canvas == null || theme == null)
                throw new ArgumentNullException(canvas == null ? nameof(canvas) : nameof(theme));

            Transform resources = canvas.Find("Resources");
            if (resources != null) UnityEngine.Object.DestroyImmediate(resources.gameObject);
            BuildBackpack(canvas, theme);
            BuildWorkbench(canvas, theme);
            BuildStorage(canvas, theme);
            BuildInteractionChip(canvas);
            BuildVitality(canvas, theme);
            StyleGraphics(canvas.Find("Graphics"), theme);
        }

        internal static void StyleSelectable(UnityEngine.UI.Selectable selectable, TopazUiTheme theme)
        {
            if (selectable.targetGraphic == null) return;
            UnityEngine.UI.Image image = selectable.targetGraphic as UnityEngine.UI.Image;
            if (image != null) image.color = theme.Raised;
            selectable.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = selectable.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.83f, .96f, .94f);
            colors.selectedColor = new Color(1f, .83f, .67f);
            colors.pressedColor = new Color(.72f, .82f, .79f);
            colors.disabledColor = new Color(.45f, .52f, .51f, .75f);
            colors.colorMultiplier = 1f;
            selectable.colors = colors;

            UnityEngine.UI.Outline outline = selectable.targetGraphic.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
                outline = selectable.targetGraphic.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = theme.Copper;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            TopazFocusIndicator indicator = selectable.GetComponent<TopazFocusIndicator>();
            if (indicator == null) indicator = selectable.gameObject.AddComponent<TopazFocusIndicator>();
            indicator.SetOutline(outline);
        }

        static void StylePanel(Transform panel, TopazUiTheme theme)
        {
            if (panel == null) return;
            UnityEngine.UI.Image image = panel.GetComponent<UnityEngine.UI.Image>();
            if (image == null) return;
            image.color = WithAlpha(theme.Panel, image.color.a);
            Transform rail = panel.Find("UI Copper Rail");
            if (rail == null)
            {
                var railObject = new GameObject("UI Copper Rail", typeof(RectTransform),
                    typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
                railObject.transform.SetParent(panel, false);
                rail = railObject.transform;
            }
            RectTransform rect = rail.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(7f, 0f);
            UnityEngine.UI.Image railImage = rail.GetComponent<UnityEngine.UI.Image>();
            railImage.color = theme.Copper;
            railImage.raycastTarget = false;
            UnityEngine.UI.LayoutElement railLayout = rail.GetComponent<UnityEngine.UI.LayoutElement>();
            if (railLayout == null) railLayout = rail.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            railLayout.ignoreLayout = true;
            rail.SetAsLastSibling();
        }

        static void PrepareJournalArt()
        {
            PrepareSprite(JournalArtPath);
        }

        static void PrepareSprite(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"UI art is missing: {path}");
            if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        static void SetWoodJournalIcon()
        {
            ItemDefinition wood = AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                "Assets/Topaz/Gameplay/WorldLoop/Definitions/Wood.asset");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WoodArtPath);
            if (wood == null || sprite == null)
                throw new InvalidOperationException("Wood definition or journal icon is missing.");
            var serialized = new SerializedObject(wood);
            serialized.FindProperty("journalIcon").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void PrepareDisplayFont(TopazUiTheme theme)
        {
            TMP_FontAsset display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
            if (display == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
                if (source == null)
                {
                    AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceSynchronousImport);
                    source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
                }
                if (source == null) throw new InvalidOperationException("Cormorant font did not import.");
                display = TMP_FontAsset.CreateFontAsset(source);
                if (display == null) throw new InvalidOperationException("Could not create TMP display font.");
                AssetDatabase.CreateAsset(display, DisplayFontPath);
                foreach (Texture2D texture in display.atlasTextures)
                    if (texture != null && !AssetDatabase.Contains(texture))
                        AssetDatabase.AddObjectToAsset(texture, display);
                if (display.material != null && !AssetDatabase.Contains(display.material))
                    AssetDatabase.AddObjectToAsset(display.material, display);
                EditorUtility.SetDirty(display);
            }
            var serialized = new SerializedObject(theme);
            serialized.FindProperty("displayFont").objectReferenceValue = display;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildBackpack(Transform canvas, TopazUiTheme theme)
        {
            LoopHud hud = canvas.GetComponent<LoopHud>();
            if (hud == null) throw new InvalidOperationException("Loop HUD view is missing.");
            Transform old = canvas.Find("Backpack");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            RectTransform root = Rect("Backpack", canvas, new Vector2(.5f, .5f),
                Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dimmer = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            dimmer.color = WithAlpha(theme.Backdrop, .63f);
            dimmer.raycastTarget = true;

            RectTransform journal = Rect("Open Journal", root, new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(1650f, 930f));
            UnityEngine.UI.Image bookImage = journal.gameObject.AddComponent<UnityEngine.UI.Image>();
            bookImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(JournalArtPath);
            if (bookImage.sprite == null)
                throw new InvalidOperationException("Journal background sprite did not import.");
            bookImage.preserveAspect = true;
            bookImage.raycastTarget = false;

            TMP_Text heading = Text("Backpack Heading", journal, "Backpack", 58f, theme.Ink);
            PlaceFromTopLeft(heading.rectTransform, new Vector2(242f, -107f),
                new Vector2(500f, 84f));
            heading.alignment = TextAlignmentOptions.Left;
            TMP_Text capacity = Text("Capacity", journal, "0 / 16 slots", 25f, theme.Ink);
            PlaceFromTopLeft(capacity.rectTransform, new Vector2(580f, -128f),
                new Vector2(196f, 48f));
            capacity.alignment = TextAlignmentOptions.Right;

            RectTransform rule = Rect("Page Rule", journal, new Vector2(0f, 1f),
                new Vector2(250f, -192f), new Vector2(590f, 2f));
            UnityEngine.UI.Image ruleImage = rule.gameObject.AddComponent<UnityEngine.UI.Image>();
            ruleImage.color = WithAlpha(theme.Ink, .34f);
            ruleImage.raycastTarget = false;

            RectTransform gridRect = Rect("Backpack Slots", journal, new Vector2(0f, 1f),
                new Vector2(252f, -220f), new Vector2(544f, 430f));
            gridRect.pivot = new Vector2(0f, 1f);
            var grid = gridRect.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(126f, 94f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperLeft;

            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "/JournalSlot.prefab");
            if (slotPrefab == null) throw new InvalidOperationException("Journal slot prefab is missing.");
            var slotButtons = new UnityEngine.UI.Button[WorldSession.BackpackCapacity];
            var slotLabels = new TMP_Text[WorldSession.BackpackCapacity];
            for (int i = 0; i < slotButtons.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, gridRect);
                instance.name = $"Slot {i + 1:00}";
                slotButtons[i] = instance.GetComponent<UnityEngine.UI.Button>();
                slotLabels[i] = instance.GetComponentInChildren<TMP_Text>();
            }

            RectTransform itemArt = Rect("Selected Item Art", journal, new Vector2(0f, 1f),
                new Vector2(1010f, -190f), new Vector2(410f, 270f));
            itemArt.pivot = new Vector2(0f, 1f);
            UnityEngine.UI.Image selectedIcon = itemArt.gameObject.AddComponent<UnityEngine.UI.Image>();
            selectedIcon.preserveAspect = true;
            selectedIcon.raycastTarget = false;
            selectedIcon.enabled = false;

            TMP_Text selectedName = Text("Selected Item Name", journal, "Empty slot", 54f, theme.Ink);
            PlaceFromTopLeft(selectedName.rectTransform, new Vector2(900f, -448f),
                new Vector2(550f, 82f));
            selectedName.alignment = TextAlignmentOptions.Left;
            TMP_Text selectedDetail = Text("Selected Item Detail", journal, "", 27f, theme.Ink);
            PlaceFromTopLeft(selectedDetail.rectTransform, new Vector2(900f, -522f),
                new Vector2(540f, 92f));
            selectedDetail.alignment = TextAlignmentOptions.TopLeft;

            RectTransform closeRect = Rect("Close", journal, new Vector2(1f, 0f),
                new Vector2(-170f, 112f), new Vector2(174f, 56f));
            UnityEngine.UI.Image closeImage = closeRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            closeImage.color = WithAlpha(theme.Ink, .82f);
            UnityEngine.UI.Button close = closeRect.gameObject.AddComponent<UnityEngine.UI.Button>();
            close.targetGraphic = closeImage;
            TMP_Text closeText = Text("Label", closeRect, "Close", 27f, theme.Text);
            Stretch(closeText.rectTransform, 8f, 3f);
            StyleJournalSelectable(close, theme);

            SetRef(hud, "inventoryPanel", root.gameObject);
            SetRef(hud, "inventoryCloseButton", close);
            SetRef(hud, "backpackCapacityLabel", capacity);
            SetRef(hud, "selectedItemName", selectedName);
            SetRef(hud, "selectedItemDetail", selectedDetail);
            SetRef(hud, "selectedItemIcon", selectedIcon);
            SetRefArray(hud, "backpackSlotButtons", slotButtons);
            SetRefArray(hud, "backpackSlotLabels", slotLabels);
            root.gameObject.SetActive(false);
        }

        static void BuildInteractionChip(Transform canvas)
        {
            Transform old = canvas.Find("Interaction Chip");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "/InteractionChip.prefab");
            if (prefab == null) throw new InvalidOperationException("Interaction chip prefab is missing.");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.name = "Interaction Chip";
            if (instance.GetComponent<CanvasGroup>() == null) instance.AddComponent<CanvasGroup>();
            InteractionChipView view = instance.AddComponent<InteractionChipView>();
            GameObject player = GameObject.Find("Player");
            if (player == null || Camera.main == null)
                throw new InvalidOperationException("Player or gameplay camera is missing.");
            SetRef(view, "session", player.GetComponent<WorldSession>());
            SetRef(view, "worldCamera", Camera.main);
            SetRef(view, "canvasRect", canvas.GetComponent<RectTransform>());
            SetRef(view, "bindingLabel", instance.transform.Find("Keycap/Key").GetComponent<TMP_Text>());
            SetRef(view, "verbLabel", instance.transform.Find("Verb").GetComponent<TMP_Text>());
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Topaz/Core/Input/TopazControls.inputactions");
            InputAction interact = controls.FindAction("Player/Interact", true);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("keyboardBindingDisplay").stringValue =
                BindingDisplay(interact, "<Keyboard>");
            serialized.FindProperty("gamepadBindingDisplay").stringValue =
                BindingDisplay(interact, "<Gamepad>");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildVitality(Transform canvas, TopazUiTheme theme)
        {
            Transform old = canvas.Find("Transient Vitality");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            RectTransform rect = Rect("Transient Vitality", canvas, new Vector2(.5f, 0f),
                new Vector2(0f, 64f), new Vector2(228f, 50f));
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = WithAlpha(theme.Backdrop, .84f);
            image.raycastTarget = false;
            rect.gameObject.AddComponent<CanvasGroup>();
            TMP_Text value = Text("Value", rect, "Health  3 / 3", 26f, theme.Text);
            Stretch(value.rectTransform, 12f, 4f);
            TransientVitalityView view = rect.gameObject.AddComponent<TransientVitalityView>();
            GameObject player = GameObject.Find("Player");
            if (player == null) throw new InvalidOperationException("Player is missing.");
            SetRef(view, "vitality", player.GetComponent<PlayerVitality>());
            SetRef(view, "session", player.GetComponent<WorldSession>());
            SetRef(view, "value", value);
        }

        static string BindingDisplay(InputAction action, string prefix)
        {
            for (int i = 0; i < action.bindings.Count; i++)
                if (action.bindings[i].effectivePath.StartsWith(prefix,
                    StringComparison.OrdinalIgnoreCase))
                    return action.GetBindingDisplayString(i);
            throw new InvalidOperationException($"Interact has no binding for {prefix}.");
        }

        static void BuildWorkbench(Transform canvas, TopazUiTheme theme)
        {
            LoopHud hud = canvas.GetComponent<LoopHud>();
            RectTransform root = CreateJournalPanel("Workbench", canvas, theme,
                out RectTransform journal);
            TMP_Text title = Text("Heading", journal, "Workbench", 58f, theme.Ink);
            PlaceFromTopLeft(title.rectTransform, new Vector2(242f, -110f), new Vector2(520f, 90f));
            title.alignment = TextAlignmentOptions.Left;

            TMP_Text description = Text("Description", journal, "", 30f, theme.Ink);
            PlaceFromTopLeft(description.rectTransform, new Vector2(242f, -270f),
                new Vector2(530f, 250f));
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;

            RectTransform artRect = Rect("Material Art", journal, new Vector2(0f, 1f),
                new Vector2(1010f, -190f), new Vector2(410f, 270f));
            artRect.pivot = new Vector2(0f, 1f);
            UnityEngine.UI.Image art = artRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WoodArtPath);
            art.preserveAspect = true;
            art.raycastTarget = false;

            UnityEngine.UI.Button craft = JournalButton("Craft chest", journal, theme,
                new Vector2(1000f, -570f), new Vector2(420f, 68f));
            UnityEngine.UI.Button close = JournalButton("Close", journal, theme,
                new Vector2(1000f, -660f), new Vector2(420f, 68f));
            SetRef(hud, "craftPanel", root.gameObject);
            SetRef(hud, "craftDescription", description);
            SetRef(hud, "craftButton", craft);
            SetRef(hud, "craftCloseButton", close);
            root.gameObject.SetActive(false);
        }

        static void BuildStorage(Transform canvas, TopazUiTheme theme)
        {
            LoopHud hud = canvas.GetComponent<LoopHud>();
            RectTransform root = CreateJournalPanel("Storage Chest", canvas, theme,
                out RectTransform journal);
            TMP_Text title = Text("Heading", journal, "Storage chest", 58f, theme.Ink);
            PlaceFromTopLeft(title.rectTransform, new Vector2(242f, -110f), new Vector2(550f, 90f));
            title.alignment = TextAlignmentOptions.Left;
            TMP_Text description = Text("Description", journal, "", 28f, theme.Ink);
            PlaceFromTopLeft(description.rectTransform, new Vector2(242f, -235f),
                new Vector2(545f, 138f));
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;

            UnityEngine.UI.Button deposit = JournalButton("Deposit all", journal, theme,
                new Vector2(242f, -415f), new Vector2(510f, 70f));
            UnityEngine.UI.Button withdraw = JournalButton("Withdraw all", journal, theme,
                new Vector2(242f, -505f), new Vector2(510f, 70f));
            UnityEngine.UI.Button close = JournalButton("Close", journal, theme,
                new Vector2(242f, -595f), new Vector2(510f, 70f));

            TMP_Text slotHeading = Text("Chest Slots Heading", journal, "Chest contents", 46f,
                theme.Ink);
            PlaceFromTopLeft(slotHeading.rectTransform, new Vector2(1010f, -118f),
                new Vector2(500f, 72f));
            slotHeading.alignment = TextAlignmentOptions.Left;
            RectTransform gridRect = Rect("Chest Slots", journal, new Vector2(0f, 1f),
                new Vector2(1010f, -228f), new Vector2(500f, 322f));
            gridRect.pivot = new Vector2(0f, 1f);
            var grid = gridRect.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(116f, 93f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            var labels = new TMP_Text[12];
            for (int i = 0; i < labels.Length; i++)
            {
                RectTransform slot = Rect($"Chest Slot {i + 1:00}", gridRect,
                    new Vector2(.5f, .5f), Vector2.zero, grid.cellSize);
                UnityEngine.UI.Image image = slot.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = WithAlpha(theme.Ink, .12f);
                image.raycastTarget = false;
                labels[i] = Text("Contents", slot, "—", 20f, theme.Ink);
                Stretch(labels[i].rectTransform, 6f, 6f);
            }

            SetRef(hud, "chestPanel", root.gameObject);
            SetRef(hud, "chestDescription", description);
            SetRef(hud, "depositButton", deposit);
            SetRef(hud, "withdrawButton", withdraw);
            SetRef(hud, "chestCloseButton", close);
            SetRefArray(hud, "chestSlotLabels", labels);
            root.gameObject.SetActive(false);
        }

        static void StyleGraphics(Transform panel, TopazUiTheme theme)
        {
            if (panel == null) return;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, .5f);
            rect.pivot = new Vector2(1f, .5f);
            rect.anchoredPosition = new Vector2(-80f, 0f);
            rect.sizeDelta = new Vector2(850f, 880f);
            StylePanel(panel, theme);
            UnityEngine.UI.Image background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = WithAlpha(theme.Backdrop, .93f);
            foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                label.color = theme.Text;
                label.raycastTarget = false;
            }
            Transform heading = panel.Find("Heading");
            if (heading != null)
            {
                TMP_Text title = heading.GetComponent<TMP_Text>();
                title.font = theme.DisplayFont != null ? theme.DisplayFont : title.font;
                title.fontSize = 64f;
                title.rectTransform.sizeDelta = new Vector2(760f, 86f);
            }
            Transform explanation = panel.Find("Explanation");
            if (explanation != null) explanation.GetComponent<TMP_Text>().text = "";
            foreach (string rowName in new[] { "Camera Zoom", "Anti-aliasing",
                "Depth of Field", "Bloom", "Ambient Occlusion" })
            {
                Transform row = panel.Find(rowName);
                if (row == null) continue;
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 80f);
                UnityEngine.UI.Image image = row.GetComponent<UnityEngine.UI.Image>();
                if (image != null) image.color = WithAlpha(theme.Panel, .94f);
                Transform rowLabel = row.Find("Name");
                if (rowLabel != null)
                {
                    RectTransform labelRect = rowLabel.GetComponent<RectTransform>();
                    labelRect.anchoredPosition = new Vector2(-140f, labelRect.anchoredPosition.y);
                }
                Transform mark = row.Find("Checkbox/Checkmark");
                if (mark != null)
                    mark.GetComponent<UnityEngine.UI.Image>().color = theme.Copper;
            }
            foreach (UnityEngine.UI.Selectable selectable in
                panel.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
                StyleSelectable(selectable, theme);
            Transform status = panel.Find("Status");
            if (status != null) status.GetComponent<TMP_Text>().color = theme.MutedText;
        }

        static RectTransform CreateJournalPanel(string name, Transform canvas,
            TopazUiTheme theme, out RectTransform journal)
        {
            Transform old = canvas.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            RectTransform root = Rect(name, canvas, new Vector2(.5f, .5f),
                Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dimmer = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            dimmer.color = WithAlpha(theme.Backdrop, .63f);
            dimmer.raycastTarget = true;
            journal = Rect("Open Journal", root, new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(1650f, 930f));
            UnityEngine.UI.Image book = journal.gameObject.AddComponent<UnityEngine.UI.Image>();
            book.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(JournalArtPath);
            book.preserveAspect = true;
            book.raycastTarget = false;
            return root;
        }

        static UnityEngine.UI.Button JournalButton(string label, Transform parent,
            TopazUiTheme theme, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(label, parent, new Vector2(0f, 1f), position, size);
            rect.pivot = new Vector2(0f, 1f);
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = WithAlpha(theme.Ink, .82f);
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            TMP_Text text = Text("Label", rect, label, 30f, theme.Text);
            Stretch(text.rectTransform, 10f, 6f);
            StyleJournalSelectable(button, theme);
            return button;
        }

        static void StyleJournalSelectable(UnityEngine.UI.Selectable selectable, TopazUiTheme theme)
        {
            var colors = selectable.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.99f, .93f, .79f);
            colors.selectedColor = new Color(.98f, .85f, .59f);
            colors.pressedColor = new Color(.91f, .76f, .55f);
            colors.disabledColor = new Color(.75f, .72f, .67f, .65f);
            selectable.colors = colors;
            selectable.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            UnityEngine.UI.Outline outline = selectable.targetGraphic.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
                outline = selectable.targetGraphic.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = theme.Copper;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            TopazFocusIndicator indicator = selectable.GetComponent<TopazFocusIndicator>();
            if (indicator == null) indicator = selectable.gameObject.AddComponent<TopazFocusIndicator>();
            indicator.SetOutline(outline);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void PlaceFromTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void SetRef(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefArray<T>(UnityEngine.Object target, string name, T[] values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static TopazUiTheme GetOrCreateTheme()
        {
            TopazUiTheme theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(ThemePath);
            if (theme != null) return theme;
            theme = ScriptableObject.CreateInstance<TopazUiTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            return theme;
        }

        static void CreatePrefabs(TopazUiTheme theme)
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Topaz/UI", "Prefabs");
            CreateButtonPrefab(theme);
            CreatePanelPrefab(theme);
            CreateInteractionChipPrefab(theme);
            CreateJournalSlotPrefab(theme);
        }

        static void CreateJournalSlotPrefab(TopazUiTheme theme)
        {
            const string path = PrefabFolder + "/JournalSlot.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    contents.GetComponent<UnityEngine.UI.Image>().color =
                        WithAlpha(theme.Ink, .12f);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                return;
            }
            var root = new GameObject("Journal Slot", typeof(RectTransform),
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            try
            {
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(126f, 94f);
                UnityEngine.UI.Image image = root.GetComponent<UnityEngine.UI.Image>();
                image.color = WithAlpha(theme.Ink, .12f);
                UnityEngine.UI.Button button = root.GetComponent<UnityEngine.UI.Button>();
                button.targetGraphic = image;
                TMP_Text label = Text("Contents", root.transform, "—", 21f, theme.Ink);
                Stretch(label.rectTransform, 7f, 7f);
                StyleJournalSelectable(button, theme);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CreateButtonPrefab(TopazUiTheme theme)
        {
            const string path = PrefabFolder + "/ActionButton.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject("Action Button", typeof(RectTransform),
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button),
                typeof(UnityEngine.UI.LayoutElement));
            try
            {
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 58f);
                root.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58f;
                UnityEngine.UI.Button button = root.GetComponent<UnityEngine.UI.Button>();
                button.targetGraphic = root.GetComponent<UnityEngine.UI.Image>();
                TMP_Text label = Text("Label", root.transform, "Continue", 26f, theme.Text);
                Stretch(label.rectTransform, 16f, 8f);
                StyleSelectable(button, theme);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CreatePanelPrefab(TopazUiTheme theme)
        {
            const string path = PrefabFolder + "/Panel.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject("Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            try
            {
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 480f);
                StylePanel(root.transform, theme);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CreateInteractionChipPrefab(TopazUiTheme theme)
        {
            const string path = PrefabFolder + "/InteractionChip.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (contents.GetComponent<CanvasGroup>() == null)
                        contents.AddComponent<CanvasGroup>();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                return;
            }
            var root = new GameObject("Interaction Chip", typeof(RectTransform),
                typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            try
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(184f, 52f);
                UnityEngine.UI.Image background = root.GetComponent<UnityEngine.UI.Image>();
                background.color = theme.Panel;
                background.raycastTarget = false;
                var keycap = new GameObject("Keycap", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                keycap.transform.SetParent(root.transform, false);
                RectTransform keycapRect = keycap.GetComponent<RectTransform>();
                keycapRect.anchorMin = keycapRect.anchorMax = new Vector2(0f, .5f);
                keycapRect.pivot = new Vector2(0f, .5f);
                keycapRect.anchoredPosition = new Vector2(10f, 0f);
                keycapRect.sizeDelta = new Vector2(42f, 38f);
                UnityEngine.UI.Image keycapImage = keycap.GetComponent<UnityEngine.UI.Image>();
                keycapImage.color = theme.Raised;
                keycapImage.raycastTarget = false;
                TMP_Text key = Text("Key", keycap.transform, "E", 23f, theme.Copper);
                Stretch(key.rectTransform, 0f, 0f);
                TMP_Text verb = Text("Verb", root.transform, "Open", 23f, theme.Text);
                verb.rectTransform.anchorMin = new Vector2(0f, .5f);
                verb.rectTransform.anchorMax = new Vector2(0f, .5f);
                verb.rectTransform.pivot = new Vector2(0f, .5f);
                verb.rectTransform.anchoredPosition = new Vector2(65f, 0f);
                verb.rectTransform.sizeDelta = new Vector2(107f, 38f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static TMP_Text Text(string name, Transform parent, string value, float size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
            label.font = size >= 50f
                ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath) ??
                  TMP_Settings.defaultFontAsset
                : TMP_Settings.defaultFontAsset;
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        static void Stretch(RectTransform rect, float horizontalInset, float verticalInset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        }

        static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);
    }
}
