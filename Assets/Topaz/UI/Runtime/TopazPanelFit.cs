using UnityEngine;

namespace Topaz.UI
{
    /// <summary>Fits a fixed authored menu panel inside the available Canvas rectangle.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopazPanelFit : MonoBehaviour
    {
        [SerializeField] Vector2 authoredSize = new Vector2(850f, 880f);
        [SerializeField] Vector2 minimumMargin = new Vector2(32f, 24f);

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
            if (_panel == null || _canvas == null) return;
            _lastSize = _canvas.rect.size;
            float fitX = Mathf.Max(.1f, (_lastSize.x - minimumMargin.x) / authoredSize.x);
            float fitY = Mathf.Max(.1f, (_lastSize.y - minimumMargin.y) / authoredSize.y);
            _panel.localScale = Vector3.one * Mathf.Min(1f, fitX, fitY);
        }

#if UNITY_EDITOR
        public void Configure(Vector2 designSize)
        {
            authoredSize = designSize;
            _panel = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvas = canvas == null ? null : canvas.GetComponent<RectTransform>();
            Refresh();
        }
#endif
    }
}
