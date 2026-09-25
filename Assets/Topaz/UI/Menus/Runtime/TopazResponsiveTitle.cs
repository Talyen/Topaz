using System.Collections.Generic;
using UnityEngine;

namespace Topaz.Menus
{
    /// <summary>Keeps the title's action group visible as the Canvas reference size changes.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopazResponsiveTitle : MonoBehaviour
    {
        const float ContentHeight = 720f;
        const float PanelWidth = 700f;

        readonly List<RectTransform> _content = new List<RectTransform>();
        readonly List<float> _baseTop = new List<float>();
        RectTransform _canvas;
        RectTransform _panel;

        void Awake()
        {
            _panel = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvas = canvas == null ? null : canvas.GetComponent<RectTransform>();
            foreach (RectTransform child in _panel)
            {
                if (Mathf.Approximately(child.anchorMin.y, 1f) &&
                    Mathf.Approximately(child.anchorMax.y, 1f))
                {
                    _content.Add(child);
                    _baseTop.Add(child.anchoredPosition.y);
                }
            }
        }

        void OnEnable() => Refresh();

        void OnRectTransformDimensionsChange() => Refresh();

        public void Refresh()
        {
            if (_canvas == null || _panel == null || _content.Count == 0) return;
            float width = Mathf.Min(PanelWidth, _canvas.rect.width * .58f);
            if (!Mathf.Approximately(_panel.sizeDelta.x, width) ||
                !Mathf.Approximately(_panel.sizeDelta.y, 0f))
                _panel.sizeDelta = new Vector2(width, 0f);
            float offset = Mathf.Max(0f, (_canvas.rect.height - ContentHeight) * .5f);
            for (int i = 0; i < _content.Count; i++)
            {
                RectTransform child = _content[i];
                Vector2 position = child.anchoredPosition;
                float top = _baseTop[i] - offset;
                if (!Mathf.Approximately(position.y, top))
                    child.anchoredPosition = new Vector2(position.x, top);
            }
        }
    }
}
