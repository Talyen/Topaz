using System;
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
    /// <summary>Applies the owner's warm-homestead, field-journal title direction.</summary>
    public static class TitleRedesignSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        static readonly Color Dark = new Color32(23, 19, 18, 255);
        static readonly Color Row = new Color32(44, 36, 30, 255);
        static readonly Color Ivory = new Color32(255, 241, 216, 255);
        static readonly Color Supporting = new Color32(224, 198, 163, 255);
        static readonly Color Brass = new Color32(239, 199, 132, 255);

        [MenuItem("Topaz/Apply Warm Refuge Title")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("Loop HUD");
            if (canvas == null) throw new InvalidOperationException("Bootstrap UI is missing.");
            ApplyToCanvas(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Topaz] Warm refuge title applied to the existing menu stage.");
        }

        internal static void ApplyToCanvas(GameObject canvas)
        {
            Transform card = canvas.transform.Find("Desktop Menus/Title Screen/Title Composition");
            GameMenus menus = canvas.GetComponent<GameMenus>();
            if (card == null || menus == null)
                throw new InvalidOperationException("Build Character and World menus first.");

            RectTransform panel = card.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, .5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(700f, 0f);
            Image panelImage = card.GetComponent<Image>();
            panelImage.color = new Color(Dark.r, Dark.g, Dark.b, .88f);
            panelImage.raycastTarget = true;
            Transform oldRail = card.Find("Brass Rail");
            if (oldRail != null) oldRail.gameObject.SetActive(false);

            TMP_Text heading = card.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(value => value.text == "TOPAZ");
            if (heading == null) throw new InvalidOperationException("TOPAZ heading is missing.");
            Place(heading.rectTransform, 86f, -45f, 550f, 132f);
            heading.fontSize = 110f;
            heading.color = Ivory;
            heading.alignment = TextAlignmentOptions.Left;
            heading.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rule = card.Find("Rule") as RectTransform;
            if (rule != null)
            {
                Place(rule, 86f, -184f, 540f, 2f);
                Image ruleImage = rule.GetComponent<Image>();
                ruleImage.color = new Color(Brass.r, Brass.g, Brass.b, .8f);
                ruleImage.raycastTarget = false;
            }
            TMP_Text tagline = EnsureText(card, "Title Tagline", "Come home. Venture out.");
            Place(tagline.rectTransform, 86f, -209f, 550f, 42f);
            tagline.font = TMP_Settings.defaultFontAsset;
            tagline.fontSize = 24f;
            tagline.color = Supporting;

            TMP_Text status = card.Find("Title Status")?.GetComponent<TMP_Text>();
            if (status != null)
            {
                Place(status.rectTransform, 86f, -248f, 540f, 44f);
                status.font = TMP_Settings.defaultFontAsset;
                status.fontSize = 20f;
                status.color = new Color32(255, 188, 170, 255);
                status.alignment = TextAlignmentOptions.Left;
                status.textWrappingMode = TextWrappingModes.Normal;
            }

            Button continueButton = RequireButton(card, "Continue");
            Button playButton = RequireButton(card, "Play");
            Button optionsButton = RequireButton(card, "Options");
            Button quitButton = RequireButton(card, "Quit");
            StyleRow(continueButton, -298f, 90f);
            StyleRow(playButton, -405f, 90f);
            StyleRow(optionsButton, -515f, 75f);
            StyleRow(quitButton, -608f, 75f);
            TMP_Text continueDetail = EnsureText(continueButton.transform, "Continue Detail", "Last journey");
            Place(continueDetail.rectTransform, 38f, -57f, 470f, 28f);
            continueDetail.fontSize = 20f;
            continueDetail.color = Supporting;
            TMP_Text playDetail = EnsureText(playButton.transform, "Play Detail",
                "Choose a Character and World");
            Place(playDetail.rectTransform, 38f, -57f, 470f, 28f);
            playDetail.fontSize = 20f;
            playDetail.color = Supporting;
            Divider(card, "Divider After Continue", -397f);
            Divider(card, "Divider After Play", -506f);
            Divider(card, "Divider After Options", -599f);

            var menuData = new SerializedObject(menus);
            menuData.FindProperty("continueDetail").objectReferenceValue = continueDetail;
            menuData.ApplyModifiedPropertiesWithoutUndo();
            if (card.GetComponent<TopazResponsiveTitle>() == null)
                card.gameObject.AddComponent<TopazResponsiveTitle>();
        }

        static Button RequireButton(Transform parent, string name) =>
            parent.Find(name)?.GetComponent<Button>() ??
            throw new InvalidOperationException("Title button is missing: " + name);

        static void StyleRow(Button button, float top, float height)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            Place(rect, 86f, top, 540f, height);
            Image image = button.targetGraphic as Image;
            if (image == null) throw new InvalidOperationException("Title button needs an Image.");
            button.transition = Selectable.Transition.None;
            TopazMenuRowVisual rowVisual = button.GetComponent<TopazMenuRowVisual>();
            if (rowVisual == null) rowVisual = button.gameObject.AddComponent<TopazMenuRowVisual>();
            rowVisual.SetPalette(image,
                new Color(Row.r, Row.g, Row.b, .035f),
                new Color(Row.r, Row.g, Row.b, .65f),
                new Color(Row.r, Row.g, Row.b, .9f));

            TMP_Text label = button.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(value => value.name != "Continue Detail" && value.name != "Play Detail");
            if (label == null) throw new InvalidOperationException("Title label is missing.");
            Place(label.rectTransform, 38f, height > 80f ? -11f : -15f, 470f, 54f);
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 34f;
            label.color = Ivory;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            Outline outline = image.GetComponent<Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = Brass;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            TopazFocusIndicator focus = button.GetComponent<TopazFocusIndicator>();
            if (focus == null) focus = button.gameObject.AddComponent<TopazFocusIndicator>();
            focus.SetOutline(outline);

            Transform markerTransform = button.transform.Find("Focus Marker");
            Image marker;
            if (markerTransform == null)
            {
                var go = new GameObject("Focus Marker", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(button.transform, false);
                marker = go.GetComponent<Image>();
            }
            else marker = markerTransform.GetComponent<Image>();
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0f, .5f);
            markerRect.pivot = new Vector2(0f, .5f);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.sizeDelta = new Vector2(9f, height);
            marker.color = Brass;
            marker.raycastTarget = false;
            focus.SetMarker(marker, true);
        }

        static void Divider(Transform parent, string name, float top)
        {
            Transform existing = parent.Find(name);
            RectTransform rect;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                rect = go.GetComponent<RectTransform>();
            }
            else rect = existing.GetComponent<RectTransform>();
            Place(rect, 86f, top, 540f, 2f);
            Image line = rect.GetComponent<Image>();
            line.color = new Color(Brass.r, Brass.g, Brass.b, .5f);
            line.raycastTarget = false;
        }

        static TMP_Text EnsureText(Transform parent, string name, string value)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<TMP_Text>();
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text label = go.GetComponent<TMP_Text>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = value;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, top);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
