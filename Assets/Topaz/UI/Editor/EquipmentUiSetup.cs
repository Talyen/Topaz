using System;
using System.Linq;
using TMPro;
using Topaz.LoopStudy;
using Topaz.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Topaz.Editor
{
    /// <summary>Adds task-focused equipment and rack pages to the existing journal Canvas.</summary>
    public static class EquipmentUiSetup
    {
        const string ThemePath = "Assets/Topaz/UI/Themes/TopazUiTheme.asset";
        const string BookPath = "Assets/Topaz/UI/Art/JournalBackground.png";

        public static void ApplyToCanvas(Transform canvas)
        {
            TopazUiTheme theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(ThemePath);
            LoopHud hud = canvas.GetComponent<LoopHud>();
            Transform backpackJournal = canvas.Find("Backpack/Open Journal");
            if (theme == null || hud == null || backpackJournal == null)
                throw new InvalidOperationException("Apply the UI design system before Equipment UI.");
            foreach (string name in new[] { "Equipment", "Gear Rack" })
            {
                Transform old = canvas.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            Transform oldTab = backpackJournal.Find("Equipment Tab");
            if (oldTab != null) UnityEngine.Object.DestroyImmediate(oldTab.gameObject);

            Button equipmentTab = Button("Equipment Tab", backpackJournal, theme,
                "Equipment", new Vector2(1248f, -100f), new Vector2(198f, 56f));
            Set(hud, "equipmentTabButton", equipmentTab);

            RectTransform panel = JournalPanel("Equipment", canvas, theme, out RectTransform book);
            Label("Heading", book, theme, "Equipment", 58,
                new Vector2(242f, -102f), new Vector2(500f, 82f));
            Button backTab = Button("Backpack Tab", book, theme, "Backpack",
                new Vector2(1248f, -100f), new Vector2(198f, 56f));
            Image portrait = Rect("Character Portrait", book, new Vector2(247f, -210f),
                new Vector2(315f, 495f)).gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.enabled = false;
            var slots = new Button[EquipmentState.Slots.Length];
            var slotLabels = new TMP_Text[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Button("Gear " + EquipmentState.Slots[i], book, theme, "",
                    new Vector2(585f, -188f - 75f * i), new Vector2(293f, 66f));
                slotLabels[i] = slots[i].GetComponentInChildren<TMP_Text>();
                slotLabels[i].fontSize = 24;
                RectTransform iconRect = Rect("Slot Icon", slots[i].transform,
                    new Vector2(12f, -13f), new Vector2(40f, 40f));
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    EquipmentIconSetup.Path(EquipmentState.Slots[i]));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            TMP_Text totals = Label("Stat Totals", book, theme,
                "Attack 0    Attack Speed 0\nArmor 0    Move Speed 0\nDodge 0    Logging 0",
                27, new Vector2(920f, -185f), new Vector2(555f, 134f));
            TMP_Text skills = Label("Skills", book, theme, "", 23,
                new Vector2(247f, -665f), new Vector2(630f, 90f));
            Button swordTool = Button("Weapon Tool", book, theme, "Weapon",
                new Vector2(247f, -770f), new Vector2(190f, 54f));
            Button axeTool = Button("Logging Axe Tool", book, theme, "Logging Axe",
                new Vector2(443f, -770f), new Vector2(190f, 54f));
            Button pickaxeTool = Button("Pickaxe Tool", book, theme, "Pickaxe",
                new Vector2(639f, -770f), new Vector2(190f, 54f));
            TMP_Text selected = Label("Selected Gear", book, theme, "",
                35, new Vector2(925f, -708f), new Vector2(540f, 49f));
            var packButtons = new Button[WorldSession.BackpackCapacity];
            var packLabels = new TMP_Text[packButtons.Length];
            for (int i = 0; i < packButtons.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;
                packButtons[i] = Button($"Gear Pack {i + 1:00}", book, theme, "—",
                    new Vector2(925f + 138f * col, -345f - 84f * row),
                    new Vector2(126f, 72f));
                packLabels[i] = packButtons[i].GetComponentInChildren<TMP_Text>();
                packLabels[i].fontSize = 21;
            }
            Button equip = Button("Equip", book, theme, "Equip",
                new Vector2(925f, -762f), new Vector2(155f, 58f));
            Button unequip = Button("Unequip", book, theme, "Unequip",
                new Vector2(1095f, -762f), new Vector2(155f, 58f));
            Button close = Button("Close", book, theme, "Close",
                new Vector2(1265f, -762f), new Vector2(155f, 58f));

            RectTransform rackPanel = JournalPanel("Gear Rack", canvas, theme, out RectTransform rackBook);
            Label("Heading", rackBook, theme, "Gear Rack", 58,
                new Vector2(250f, -100f), new Vector2(570f, 80f));
            var rackButtons = new Button[4];
            var rackLabels = new TMP_Text[4];
            for (int i = 0; i < rackButtons.Length; i++)
            {
                rackButtons[i] = Button("Rack Item " + (i + 1), rackBook, theme, "",
                    new Vector2(280f, -220f - i * 135f), new Vector2(1060f, 108f));
                rackLabels[i] = rackButtons[i].GetComponentInChildren<TMP_Text>();
                rackLabels[i].fontSize = 37;
            }
            Button rackClose = Button("Close", rackBook, theme, "Close",
                new Vector2(1190f, -760f), new Vector2(210f, 58f));

            EquipmentJournalView view = canvas.GetComponent<EquipmentJournalView>() ??
                canvas.gameObject.AddComponent<EquipmentJournalView>();
            Set(view, "equipmentPanel", panel.gameObject);
            Set(view, "rackPanel", rackPanel.gameObject);
            Set(view, "backpackTab", backTab);
            Set(view, "equipmentClose", close);
            Set(view, "rackClose", rackClose);
            Set(view, "equipButton", equip);
            Set(view, "unequipButton", unequip);
            Set(view, "selectedName", selected);
            Set(view, "totals", totals);
            Set(view, "skills", skills);
            Set(view, "swordToolButton", swordTool);
            Set(view, "axeToolButton", axeTool);
            Set(view, "pickaxeToolButton", pickaxeTool);
            Set(view, "portrait", portrait);
            Sprite[] portraits = CharacterLooks.All.Select(id =>
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Topaz/UI/Art/EquipmentPortraits/" + id.Replace('.', '-') + ".png"))
                .ToArray();
            SetArray(view, "portraits", portraits);
            SetArray(view, "slotButtons", slots);
            SetArray(view, "slotLabels", slotLabels);
            SetArray(view, "packButtons", packButtons);
            SetArray(view, "packLabels", packLabels);
            SetArray(view, "rackButtons", rackButtons);
            SetArray(view, "rackLabels", rackLabels);
            Set(hud, "equipmentView", view);
            panel.gameObject.SetActive(false);
            rackPanel.gameObject.SetActive(false);
        }

        static RectTransform JournalPanel(string name, Transform parent, TopazUiTheme theme,
            out RectTransform book)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(theme.Backdrop.r, theme.Backdrop.g,
                theme.Backdrop.b, 0.7f);
            book = Rect("Open Journal", rect, Vector2.zero, new Vector2(1650f, 930f), false);
            book.anchorMin = book.anchorMax = new Vector2(.5f, .5f);
            book.pivot = new Vector2(.5f, .5f);
            Image image = book.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BookPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 position,
            Vector2 size, bool topLeft = true)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = topLeft ? new Vector2(0f, 1f) : new Vector2(.5f, .5f);
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static TMP_Text Label(string name, Transform parent, TopazUiTheme theme,
            string value, float size, Vector2 position, Vector2 bounds)
        {
            RectTransform rect = Rect(name, parent, position, bounds);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = theme.DisplayFont;
            label.fontSize = size;
            label.color = theme.Ink;
            label.text = value;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }

        static Button Button(string name, Transform parent, TopazUiTheme theme,
            string value, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(name, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = theme.Raised;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.99f, .93f, .79f);
            colors.selectedColor = new Color(.98f, .85f, .59f);
            button.colors = colors;
            TMP_Text label = Label("Label", rect, theme, value, 26, Vector2.zero, size);
            label.color = theme.Text;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(.5f, .5f);
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray<T>(UnityEngine.Object target, string field, T[] values)
            where T : UnityEngine.Object
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
