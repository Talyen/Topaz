using UnityEngine;
using UnityEngine.EventSystems;

namespace Topaz.UI
{
    /// <summary>Shows a persistent outline while a uGUI control has keyboard or gamepad focus.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public sealed class TopazFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] UnityEngine.UI.Outline focusOutline;
        [SerializeField] UnityEngine.UI.Graphic focusMarker;
        [SerializeField] UnityEngine.UI.Graphic[] focusBorders;
        [SerializeField] bool useOutline = true;

        void Awake()
        {
            if (useOutline && focusOutline == null)
            {
                UnityEngine.UI.Selectable selectable = GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null && selectable.targetGraphic != null)
                    focusOutline = selectable.targetGraphic.GetComponent<UnityEngine.UI.Outline>();
            }
            Sync();
        }

        void OnEnable() => Sync();

        void OnDisable()
        {
            SetFocused(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetFocused(false);
        }

        void Sync()
        {
            SetFocused(EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == gameObject);
        }

        void SetFocused(bool focused)
        {
            if (focusOutline != null) focusOutline.enabled = useOutline && focused;
            if (focusMarker != null) focusMarker.enabled = focused;
            if (focusBorders != null)
                foreach (UnityEngine.UI.Graphic border in focusBorders)
                    if (border != null) border.enabled = focused;
        }

#if UNITY_EDITOR
        public void SetOutline(UnityEngine.UI.Outline value) => focusOutline = value;
        public void SetMarker(UnityEngine.UI.Graphic value, bool keepOutline = false)
        {
            focusMarker = value;
            useOutline = keepOutline;
            SetFocused(false);
        }

        public void SetBorders(UnityEngine.UI.Graphic[] borders)
        {
            focusBorders = borders;
            useOutline = false;
            SetFocused(false);
        }
#endif
    }
}
