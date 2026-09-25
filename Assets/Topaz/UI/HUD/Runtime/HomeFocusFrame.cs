using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topaz.LoopStudy
{
    /// <summary>A shape cue for keyboard, gamepad, and pointer focus on Home actions.</summary>
    public sealed class HomeFocusFrame : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        Outline _outline;
        bool _hovered;

        void Awake()
        {
            _outline = GetComponent<Outline>();
            if (_outline != null) _outline.enabled = false;
        }

        public void OnSelect(BaseEventData eventData) => UpdateFrame(true);
        public void OnDeselect(BaseEventData eventData) => UpdateFrame(_hovered);
        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            UpdateFrame(true);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            UpdateFrame(EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == gameObject);
        }

        void UpdateFrame(bool show)
        {
            if (_outline != null) _outline.enabled = show;
        }
    }
}
