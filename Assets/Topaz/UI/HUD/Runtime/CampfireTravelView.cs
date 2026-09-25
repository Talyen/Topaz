using System.Collections.Generic;
using TMPro;
using Topaz.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Topaz.LoopStudy
{
    /// <summary>Facing Travel and Cook pages over the existing Journal backdrop.</summary>
    public sealed class CampfireTravelView : MonoBehaviour
    {
        static readonly Color Ink = new Color32(23, 19, 18, 248);
        static readonly Color Raised = new Color32(249, 229, 196, 245);
        static readonly Color Ivory = new Color32(45, 31, 25, 255);
        static readonly Color Brass = new Color32(239, 199, 132, 255);
        static readonly Color Disabled = new Color32(135, 119, 101, 255);

        readonly List<UnityEngine.UI.Button> _activeRows = new List<UnityEngine.UI.Button>();
        GameObject _panel;
        RectTransform _content;
        UnityEngine.UI.ScrollRect _scroll;
        UnityEngine.UI.Button _close;
        UnityEngine.UI.Button _cook;
        TMP_Text _ingredients;
        WorldSession _session;
        TMP_FontAsset _font;
        Sprite _icon;
        Sprite _journal;
        int _focusedIndex = -1;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Bind(WorldSession session, TMP_FontAsset font, Sprite journal)
        {
            _session = session;
            _font = font;
            _icon = Resources.Load<Sprite>("CampfireIcon");
            _journal = journal;
            if (_panel == null) Create();
        }

        void Create()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null || _icon == null)
            {
                Debug.LogError("Travel menu needs the HUD Canvas and Campfire icon.", this);
                return;
            }
            _panel = new GameObject("Campfire Travel", typeof(RectTransform),
                typeof(UnityEngine.UI.Image));
            _panel.transform.SetParent(canvas.transform, false);
            RectTransform full = (RectTransform)_panel.transform;
            full.anchorMin = Vector2.zero;
            full.anchorMax = Vector2.one;
            full.offsetMin = full.offsetMax = Vector2.zero;
            var dimmer = _panel.GetComponent<UnityEngine.UI.Image>();
            dimmer.color = new Color(0f, 0f, 0f, .68f);

            RectTransform card = Rect(_panel.transform, "Campfire Journal", new Vector2(1650f, 930f));
            var cardImage = card.gameObject.AddComponent<UnityEngine.UI.Image>();
            cardImage.sprite = _journal;
            cardImage.preserveAspect = true;
            cardImage.color = _journal == null ? Raised : Color.white;
            Label(card, "TRAVEL", 54f, new Vector2(-395f, 320f),
                new Vector2(540f, 80f), Ivory, TextAlignmentOptions.Center);
            Label(card, "COOK", 54f, new Vector2(395f, 320f),
                new Vector2(540f, 80f), Ivory, TextAlignmentOptions.Center);
            _close = Button(card, "Back", new Vector2(0f, -362f),
                new Vector2(225f, 66f));
            Label(_close.transform, "Back", 34f, Vector2.zero, new Vector2(210f, 60f),
                Ivory, TextAlignmentOptions.Center);
            _close.onClick.AddListener(Hide);

            Label(card, "Mushroom Stew", 38f, new Vector2(395f, 165f),
                new Vector2(560f, 70f), Ivory, TextAlignmentOptions.Center);
            Label(card, "+50% stamina refill for 24 World hours", 28f,
                new Vector2(395f, 95f), new Vector2(620f, 75f), Ivory,
                TextAlignmentOptions.Center);
            _ingredients = Label(card, "", 29f, new Vector2(395f, 0f),
                new Vector2(570f, 70f), Ivory, TextAlignmentOptions.Center);
            _cook = Button(card, "Cook Mushroom Stew", new Vector2(395f, -115f),
                new Vector2(420f, 84f));
            Label(_cook.transform, "Cook", 38f, Vector2.zero,
                new Vector2(410f, 78f), Ivory, TextAlignmentOptions.Center);
            _cook.onClick.AddListener(() =>
            {
                if (_session.TryCookStew()) Show();
            });

            RectTransform scrollRect = Rect(card, "Destinations", new Vector2(625f, 535f));
            scrollRect.anchoredPosition = new Vector2(-395f, -25f);
            _scroll = scrollRect.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            RectTransform viewport = Rect(scrollRect, "Viewport", Vector2.zero);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            var viewportImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = false;
            var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;
            _content = Rect(viewport, "Content", Vector2.zero);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;
            var layout = _content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fit = _content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            _scroll.viewport = viewport;
            _scroll.content = _content;
            card.gameObject.AddComponent<TopazJournalFit>();
            _panel.SetActive(false);
        }

        public void Show()
        {
            if (_panel == null || _session == null) return;
            foreach (Transform child in _content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            _activeRows.Clear();
            _ingredients.text = "Mushrooms  " + _session.MushroomsAtFire + "/1";
            _cook.interactable = _session.MushroomsAtFire > 0;
            foreach (CampfireTravelCatalog.Destination destination in _session.TravelDestinations)
            {
                var row = Button(_content, "Travel to " + destination.label,
                    Vector2.zero, new Vector2(600f, 88f));
                var size = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                size.preferredHeight = 88f;
                var iconObject = new GameObject("Campfire Icon", typeof(RectTransform),
                    typeof(UnityEngine.UI.Image));
                iconObject.transform.SetParent(row.transform, false);
                RectTransform iconRect = (RectTransform)iconObject.transform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, .5f);
                iconRect.pivot = new Vector2(0f, .5f);
                iconRect.anchoredPosition = new Vector2(25f, 0f);
                iconRect.sizeDelta = new Vector2(52f, 52f);
                var iconImage = iconObject.GetComponent<UnityEngine.UI.Image>();
                iconImage.sprite = _icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                TMP_Text label = Label(row.transform,
                    destination.label + (destination.stableId == _session.CurrentCampfireId
                        ? "  •  Here" : ""), 31f,
                    new Vector2(25f, 0f), new Vector2(520f, 80f), Ivory,
                    TextAlignmentOptions.Left);
                iconImage.enabled = false;
                bool current = destination.stableId == _session.CurrentCampfireId;
                if (current)
                {
                    row.interactable = false;
                    label.color = Disabled;
                }
                else
                {
                    string id = destination.stableId;
                    row.onClick.AddListener(() =>
                    {
                        if (_session.TryFastTravel(id)) Hide();
                    });
                    _activeRows.Add(row);
                }
            }
            ConfigureNavigation();
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
            _focusedIndex = -1;
            EventSystem.current?.SetSelectedGameObject(
                _activeRows.Count > 0 ? _activeRows[0].gameObject :
                    _cook.interactable ? _cook.gameObject : _close.gameObject);
        }

        void ConfigureNavigation()
        {
            for (int i = 0; i < _activeRows.Count; i++)
            {
                var navigation = new UnityEngine.UI.Navigation { mode =
                    UnityEngine.UI.Navigation.Mode.Explicit };
                navigation.selectOnUp = i == 0 ? _close : _activeRows[i - 1];
                navigation.selectOnDown = i == _activeRows.Count - 1 ? _close :
                    _activeRows[i + 1];
                navigation.selectOnRight = _cook.interactable ? _cook : _close;
                _activeRows[i].navigation = navigation;
            }
            var closeNavigation = new UnityEngine.UI.Navigation { mode =
                UnityEngine.UI.Navigation.Mode.Explicit };
            if (_activeRows.Count > 0)
            {
                closeNavigation.selectOnDown = _activeRows[0];
                closeNavigation.selectOnUp = _activeRows[_activeRows.Count - 1];
            }
            closeNavigation.selectOnRight = _cook.interactable ? _cook : null;
            _close.navigation = closeNavigation;
            var cookNavigation = new UnityEngine.UI.Navigation { mode =
                UnityEngine.UI.Navigation.Mode.Explicit };
            cookNavigation.selectOnLeft = _activeRows.Count > 0 ? _activeRows[0] : _close;
            cookNavigation.selectOnDown = _close;
            cookNavigation.selectOnUp = _close;
            _cook.navigation = cookNavigation;
        }

        void Update()
        {
            if (!IsOpen || _activeRows.Count < 2 || EventSystem.current == null) return;
            int index = _activeRows.FindIndex(button =>
                button.gameObject == EventSystem.current.currentSelectedGameObject);
            if (index < 0 || index == _focusedIndex) return;
            _focusedIndex = index;
            _scroll.verticalNormalizedPosition = 1f - (float)index /
                (_activeRows.Count - 1);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        static RectTransform Rect(Transform parent, string name, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            return rect;
        }

        UnityEngine.UI.Button Button(Transform parent, string name, Vector2 position,
            Vector2 size)
        {
            RectTransform rect = Rect(parent, name, size);
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Raised;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .93f, .8f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.8f, .72f, .6f);
            colors.disabledColor = new Color(.58f, .55f, .5f, .65f);
            button.colors = colors;
            var outline = rect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Brass;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = false;
            rect.gameObject.AddComponent<TopazFocusIndicator>();
            return button;
        }

        TMP_Text Label(Transform parent, string value, float size, Vector2 position,
            Vector2 dimensions, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = Rect(parent, "Label", dimensions);
            rect.anchoredPosition = position;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = _font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}
