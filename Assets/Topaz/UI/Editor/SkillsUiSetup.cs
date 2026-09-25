using System;
using TMPro;
using Topaz.LoopStudy;
using Topaz.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Topaz.Editor
{
    /// <summary>Adds a Skills page beside the existing Backpack and Equipment journal pages.</summary>
    public static class SkillsUiSetup
    {
        const string ThemePath = "Assets/Topaz/UI/Themes/TopazUiTheme.asset";
        const string BookPath = "Assets/Topaz/UI/Art/JournalBackground.png";

        public static void ApplyToCanvas(Transform canvas)
        {
            TopazUiTheme theme = AssetDatabase.LoadAssetAtPath<TopazUiTheme>(ThemePath);
            Transform backpack = canvas.Find("Backpack/Open Journal");
            Transform equipment = canvas.Find("Equipment/Open Journal");
            LoopHud hud = canvas.GetComponent<LoopHud>();
            if (theme == null || backpack == null || equipment == null || hud == null)
                throw new InvalidOperationException("Apply the UI design system and Equipment UI first.");

            foreach (Transform parent in new[] { backpack, equipment })
            {
                Transform prior = parent.Find("Skills Tab");
                if (prior != null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
            }
            Transform old = canvas.Find("Skills");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            Button backpackTab = Button("Skills Tab", backpack, theme, "Skills",
                new Vector2(1040f, -100f), new Vector2(198f, 56f));
            Button equipmentTab = Button("Skills Tab", equipment, theme, "Skills",
                new Vector2(1040f, -100f), new Vector2(198f, 56f));
            Set(hud, "skillsTabButton", backpackTab);
            EquipmentJournalView equipmentView = canvas.GetComponent<EquipmentJournalView>();
            Set(equipmentView, "skillsTab", equipmentTab);

            GameObject root = new GameObject("Skills", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas, false);
            RectTransform panel = root.GetComponent<RectTransform>();
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(theme.Backdrop.r, theme.Backdrop.g,
                theme.Backdrop.b, .7f);
            RectTransform book = Rect("Open Journal", panel, Vector2.zero,
                new Vector2(1650f, 930f), false);
            book.anchorMin = book.anchorMax = new Vector2(.5f, .5f);
            book.pivot = new Vector2(.5f, .5f);
            Image bookImage = book.gameObject.AddComponent<Image>();
            bookImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BookPath);
            bookImage.preserveAspect = true;
            bookImage.raycastTarget = false;
            Label("Heading", book, theme, "Skills", 58,
                new Vector2(242f, -102f), new Vector2(500f, 82f));
            Button back = Button("Equipment Tab", book, theme, "Equipment",
                new Vector2(1248f, -100f), new Vector2(198f, 56f));
            var skillButtons = new Button[SkillIds.All.Length];
            var skillLabels = new TMP_Text[skillButtons.Length];
            for (int i = 0; i < skillButtons.Length; i++)
            {
                skillButtons[i] = Button("Skill " + SkillIds.All[i], book, theme, "",
                    new Vector2(242f, -208f - i * 96f), new Vector2(550f, 78f));
                skillLabels[i] = skillButtons[i].GetComponentInChildren<TMP_Text>();
                skillLabels[i].fontSize = 25;
            }
            TMP_Text summary = Label("Skill Summary", book, theme, "", 21,
                new Vector2(890f, -185f), new Vector2(530f, 140f));
            summary.textWrappingMode = TextWrappingModes.Normal;
            summary.alignment = TextAlignmentOptions.TopLeft;
            var talentButtons = new Button[4];
            var talentLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                talentButtons[i] = Button("Talent " + (i + 1), book, theme, "",
                    new Vector2(890f, -340f - i * 66f), new Vector2(530f, 58f));
                talentLabels[i] = talentButtons[i].GetComponentInChildren<TMP_Text>();
                talentLabels[i].fontSize = 21;
            }
            TMP_Text detail = Label("Talent Detail", book, theme, "", 21,
                new Vector2(890f, -610f), new Vector2(530f, 95f));
            detail.textWrappingMode = TextWrappingModes.Normal;
            detail.alignment = TextAlignmentOptions.TopLeft;
            Button action = Button("Talent Action", book, theme, "Learn",
                new Vector2(890f, -715f), new Vector2(245f, 58f));
            Button close = Button("Close", book, theme, "Close",
                new Vector2(1222f, -715f), new Vector2(198f, 58f));

            SkillsJournalView view = canvas.GetComponent<SkillsJournalView>() ??
                canvas.gameObject.AddComponent<SkillsJournalView>();
            Set(view, "skillsPanel", root);
            Set(view, "equipmentTab", back);
            Set(view, "closeButton", close);
            Set(view, "actionButton", action);
            Set(view, "actionLabel", action.GetComponentInChildren<TMP_Text>());
            Set(view, "summary", summary);
            Set(view, "detail", detail);
            SetArray(view, "skillButtons", skillButtons);
            SetArray(view, "skillLabels", skillLabels);
            SetArray(view, "talentButtons", talentButtons);
            SetArray(view, "talentLabels", talentLabels);
            Set(hud, "skillsView", view);
            root.SetActive(false);
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
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = size >= 50f ? theme.DisplayFont : TMP_Settings.defaultFontAsset;
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
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = theme.Raised;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.99f, .93f, .79f);
            colors.selectedColor = new Color(.98f, .85f, .59f);
            button.colors = colors;
            TMP_Text label = Label("Label", rect, theme, value, 26, Vector2.zero, size);
            label.color = theme.Text;
            RectTransform area = label.rectTransform;
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.pivot = new Vector2(.5f, .5f);
            area.offsetMin = new Vector2(10f, 4f);
            area.offsetMax = new Vector2(-10f, -4f);
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
