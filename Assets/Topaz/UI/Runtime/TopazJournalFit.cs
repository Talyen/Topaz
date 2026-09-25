using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Topaz.UI
{
    /// <summary>Keeps the large illustrated journal visible when resolution or UI scale changes.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopazJournalFit : MonoBehaviour
    {
        [SerializeField] Vector2 bookSize = new Vector2(1650f, 930f);
        [SerializeField] Vector2 minimumMargin = new Vector2(32f, 24f);
        [SerializeField, Range(1f, 1.5f)] float maxTextScale = 1.5f;

        RectTransform _book;
        RectTransform _canvas;
        CanvasScaler _scaler;
        TMP_Text[] _text;
        float[] _baseFontSizes;
        Vector2 _lastCanvasSize;

        void Awake()
        {
            _book = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvas = canvas == null ? null : canvas.GetComponent<RectTransform>();
            _scaler = canvas == null ? null : canvas.GetComponent<CanvasScaler>();
            _text = GetComponentsInChildren<TMP_Text>(true);
            _baseFontSizes = new float[_text.Length];
            for (int i = 0; i < _text.Length; i++)
                _baseFontSizes[i] = _text[i].fontSize;
        }

        void OnEnable() => Refresh();

        void LateUpdate()
        {
            if (_canvas != null && _canvas.rect.size != _lastCanvasSize) Refresh();
        }

        public void Refresh()
        {
            if (_canvas == null || _book == null) return;
            _lastCanvasSize = _canvas.rect.size;
            float fitX = Mathf.Max(.1f, (_lastCanvasSize.x - minimumMargin.x) / bookSize.x);
            float fitY = Mathf.Max(.1f, (_lastCanvasSize.y - minimumMargin.y) / bookSize.y);
            float fit = Mathf.Min(1f, fitX, fitY);
            _book.localScale = Vector3.one * fit;
            float requestedScale = _scaler == null ? 1f :
                Mathf.Clamp(1920f / _scaler.referenceResolution.x, 1f, maxTextScale);
            for (int i = 0; i < _text.Length; i++)
            {
                TMP_Text label = _text[i];
                if (label == null || _baseFontSizes[i] >= 50f) continue;
                label.fontSize = _baseFontSizes[i] * requestedScale;
                label.enableAutoSizing = requestedScale > 1f;
                label.fontSizeMin = _baseFontSizes[i];
                label.fontSizeMax = _baseFontSizes[i] * requestedScale;
            }
        }

#if UNITY_EDITOR
        public void SetMaxTextScale(float value) => maxTextScale = Mathf.Clamp(value, 1f, 1.5f);
#endif
    }
}
