using TMPro;
using Topaz.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topaz.LoopStudy
{
    /// <summary>Character status details on the shared Journal paper.</summary>
    public sealed class StatusJournalView : MonoBehaviour
    {
        WorldSession _session;
        LoopHud _hud;
        GameObject _page;
        TMP_Text _restRow;
        TMP_Text _foodRow;
        TMP_Text _empty;
        Image _restIcon;
        Image _foodIcon;
        Button _back;

        public bool IsOpen => _page != null && _page.activeSelf;

        public void Bind(WorldSession session, LoopHud hud, Button skillsTab,
            TMP_FontAsset font, Sprite background)
        {
            _session = session;
            _hud = hud;
            if (_page != null) return;
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) return;
            _page = new GameObject("Status Effects Journal", typeof(RectTransform),
                typeof(Image));
            _page.transform.SetParent(canvas.transform, false);
            RectTransform full = (RectTransform)_page.transform;
            full.anchorMin = Vector2.zero;
            full.anchorMax = Vector2.one;
            full.offsetMin = full.offsetMax = Vector2.zero;
            Image paper = _page.GetComponent<Image>();
            paper.sprite = background;
            paper.preserveAspect = background != null;
            paper.color = background == null ? new Color(.94f, .87f, .74f) : Color.white;
            Label(_page.transform, "STATUS EFFECTS", font, 52f,
                new Vector2(0f, 335f), new Vector2(940f, 80f));
            _restIcon = Icon(_page.transform, new Vector2(-490f, 160f));
            _restIcon.sprite = Resources.Load<Sprite>("CampfireIcon");
            _restRow = Label(_page.transform, "", font, 31f,
                new Vector2(60f, 160f), new Vector2(950f, 145f));
            _restRow.alignment = TextAlignmentOptions.MidlineLeft;
            _foodIcon = Icon(_page.transform, new Vector2(-490f, -65f));
            _foodRow = Label(_page.transform, "", font, 31f,
                new Vector2(60f, -65f), new Vector2(950f, 145f));
            _foodRow.alignment = TextAlignmentOptions.MidlineLeft;
            _empty = Label(_page.transform, "No active effects.", font, 32f,
                new Vector2(0f, 40f), new Vector2(900f, 90f));
            _back = ButtonAt(_page.transform, "Back to Backpack", font,
                new Vector2(0f, -340f));
            _back.onClick.AddListener(() =>
            {
                Hide();
                _hud.ToggleInventoryPanel();
            });
            _page.SetActive(false);
            if (skillsTab != null)
            {
                Button tab = Instantiate(skillsTab, skillsTab.transform.parent);
                tab.name = "Status Effects Tab";
                tab.GetComponent<RectTransform>().anchoredPosition += new Vector2(380f, 0f);
                TMP_Text label = tab.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = "Status";
                tab.onClick.RemoveAllListeners();
                tab.onClick.AddListener(Show);
            }
        }

        public void Show()
        {
            if (_page == null || _session == null) return;
            _hud.ClosePanels();
            bool rested = _session.RestedHoursRemaining > 0d;
            bool fed = _session.FoodHoursRemaining > 0d;
            _restRow.gameObject.SetActive(rested);
            _restIcon.gameObject.SetActive(rested);
            _foodRow.gameObject.SetActive(fed);
            _foodIcon.gameObject.SetActive(fed);
            _empty.gameObject.SetActive(!rested && !fed);
            if (rested)
                _restRow.text = "Rested  •  " +
                    _session.RestedHoursRemaining.ToString("0.0") +
                    " World hours\nLarger stamina reserve (125 instead of 100).";
            if (fed)
            {
                string name = _session.FoodTier == 2 ? "Mushroom Stew" : "Red Berries";
                _foodRow.text = name + "  •  " +
                    _session.FoodHoursRemaining.ToString("0.0") +
                    " World hours\nStamina refills " +
                    (_session.FoodTier == 2 ? "50%" : "20%") + " faster.";
                _foodIcon.sprite = _session.ItemJournalIcon(_session.FoodTier == 2 ?
                    SurvivalRules.StewId : SurvivalRules.BerriesId);
            }
            _page.SetActive(true);
            _page.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(_back.gameObject);
        }

        public void Hide()
        {
            if (_page != null) _page.SetActive(false);
            if (EventSystem.current != null && _back != null &&
                EventSystem.current.currentSelectedGameObject == _back.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        static TMP_Text Label(Transform parent, string value, TMP_FontAsset font,
            float size, Vector2 position, Vector2 bounds)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = bounds;
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = new Color32(41, 31, 26, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static Image Icon(Transform parent, Vector2 position)
        {
            var obj = new GameObject("Effect Icon", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(82f, 82f);
            Image icon = obj.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        static Button ButtonAt(Transform parent, string value, TMP_FontAsset font,
            Vector2 position)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(Image),
                typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(330f, 70f);
            obj.GetComponent<Image>().color = new Color32(83, 53, 35, 245);
            Button button = obj.GetComponent<Button>();
            Label(obj.transform, value, font, 30f, Vector2.zero,
                new Vector2(320f, 65f)).color = Color.white;
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color32(239, 199, 132, 255);
            outline.enabled = false;
            obj.AddComponent<TopazFocusIndicator>();
            return button;
        }
    }
}
