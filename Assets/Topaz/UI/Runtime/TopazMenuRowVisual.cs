using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topaz.UI
{
    /// <summary>Quiet at rest, tangible on hover, and unmistakable at keyboard/gamepad focus.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class TopazMenuRowVisual : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image background;
        [SerializeField] Color rest = new Color(0.18f, 0.14f, 0.12f, .03f);
        [SerializeField] Color hover = new Color(0.18f, 0.14f, 0.12f, .68f);
        [SerializeField] Color focus = new Color(0.18f, 0.14f, 0.12f, .92f);

        bool _hovered;
        bool _selected;
        bool _wasInteractable = true;
        Button _button;
        TMP_Text _label;
        Color _labelColor;

        void Awake()
        {
            if (background == null) background = GetComponent<Image>();
            _button = GetComponent<Button>();
            _label = GetComponentInChildren<TMP_Text>(true);
            if (_label != null) _labelColor = _label.color;
        }

        void OnEnable()
        {
            _selected = EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == gameObject;
            Refresh();
        }

        void Update()
        {
            if (_button != null && _button.interactable != _wasInteractable) Refresh();
        }

        public void OnSelect(BaseEventData eventData) { _selected = true; Refresh(); }
        public void OnDeselect(BaseEventData eventData) { _selected = false; Refresh(); }
        public void OnPointerEnter(PointerEventData eventData) { _hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; Refresh(); }

        void Refresh()
        {
            if (background == null) return;
            bool interactable = _button == null || _button.interactable;
            _wasInteractable = interactable;
            if (!interactable)
            {
                background.color = new Color(rest.r, rest.g, rest.b,
                    Mathf.Min(rest.a, .28f));
                if (_label != null) _label.color = new Color(_labelColor.r,
                    _labelColor.g, _labelColor.b, .50f);
                return;
            }
            if (_label != null) _label.color = _labelColor;
            background.color = _selected ? focus : _hovered ? hover : rest;
        }

#if UNITY_EDITOR
        public void SetPalette(Image target, Color resting, Color hovered, Color focused)
        {
            background = target;
            rest = resting;
            hover = hovered;
            focus = focused;
            Refresh();
        }
#endif
    }
}
