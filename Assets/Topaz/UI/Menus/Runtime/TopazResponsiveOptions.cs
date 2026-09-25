using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Topaz.Menus
{
    /// <summary>Reflows the Options rows when the player changes UI scale.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopazResponsiveOptions : MonoBehaviour
    {
        [SerializeField] TMP_Text heading;
        [SerializeField] RectTransform rule;
        [SerializeField] Button displayMode;
        [SerializeField] Button windowSize;
        [SerializeField] Button uiScale;
        [SerializeField] Button graphics;
        [SerializeField] TMP_Text info;
        [SerializeField] Button back;

        RectTransform _panel;
        RectTransform _canvas;
        Vector2 _lastSize;

        void Awake()
        {
            _panel = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvas = canvas == null ? null : canvas.GetComponent<RectTransform>();
        }

        void OnEnable() => Refresh();

        void LateUpdate()
        {
            if (_canvas != null && _canvas.rect.size != _lastSize) Refresh();
        }

        public void Refresh()
        {
            if (_canvas == null || _panel == null || heading == null || back == null) return;
            _lastSize = _canvas.rect.size;
            float t = Mathf.InverseLerp(720f, 1080f, _lastSize.y);
            float width = Mathf.Min(720f, _lastSize.x * .58f);
            _panel.anchorMin = new Vector2(0f, 0f);
            _panel.anchorMax = new Vector2(0f, 1f);
            _panel.pivot = new Vector2(0f, .5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = new Vector2(width, 0f);
            Place(heading.rectTransform, Mathf.Lerp(-34f, -70f, t), 550f, 96f);
            Place(rule, Mathf.Lerp(-139f, -174f, t), 550f, 2f);
            Place(displayMode.GetComponent<RectTransform>(), Mathf.Lerp(-163f, -220f, t), 560f, 70f);
            Place(windowSize.GetComponent<RectTransform>(), Mathf.Lerp(-243f, -320f, t), 560f, 70f);
            Place(uiScale.GetComponent<RectTransform>(), Mathf.Lerp(-323f, -420f, t), 560f, 70f);
            Place(graphics.GetComponent<RectTransform>(), Mathf.Lerp(-403f, -520f, t), 560f, 70f);
            Place(info.rectTransform, Mathf.Lerp(-493f, -625f, t), 560f, 70f);
            Place(back.GetComponent<RectTransform>(), Mathf.Lerp(-583f, -735f, t), 560f, 70f);
            heading.alignment = TextAlignmentOptions.Left;
            info.alignment = TextAlignmentOptions.Left;
        }

        static void Place(RectTransform rect, float top, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(86f, top);
            rect.sizeDelta = new Vector2(width, height);
        }

#if UNITY_EDITOR
        public void Configure(TMP_Text title, RectTransform ruleRect, Button display,
            Button window, Button scale, Button graphicsButton, TMP_Text displayInfo, Button backButton)
        {
            heading = title;
            rule = ruleRect;
            displayMode = display;
            windowSize = window;
            uiScale = scale;
            graphics = graphicsButton;
            info = displayInfo;
            back = backButton;
            _panel = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvas = canvas == null ? null : canvas.GetComponent<RectTransform>();
            Refresh();
        }
#endif
    }
}
