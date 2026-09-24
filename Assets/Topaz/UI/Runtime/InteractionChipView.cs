using TMPro;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.UI
{
    /// <summary>One short in-world action cue, driven by WorldSession's target choice.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InteractionChipView : MonoBehaviour
    {
        [SerializeField] WorldSession session;
        [SerializeField] Camera worldCamera;
        [SerializeField] RectTransform canvasRect;
        [SerializeField] TMP_Text bindingLabel;
        [SerializeField] TMP_Text verbLabel;
        [SerializeField] string keyboardBindingDisplay;
        [SerializeField] string gamepadBindingDisplay;

        CanvasGroup _group;
        RectTransform _rect;
        bool _gamepadMode;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _rect = GetComponent<RectTransform>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;
        }

        void LateUpdate()
        {
            if (session == null || worldCamera == null || canvasRect == null ||
                bindingLabel == null || verbLabel == null ||
                !session.TryGetInteraction(out Transform target, out string verb))
            {
                _group.alpha = 0f;
                return;
            }

            UpdateDeviceMode();
            Vector3 world = target.name == "Interaction Anchor" ? target.position :
                target.position + Vector3.up * 1.25f;
            Vector3 screen = worldCamera.WorldToScreenPoint(world);
            if (screen.z <= 0f || !worldCamera.pixelRect.Contains(screen) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screen, null, out Vector2 local))
            {
                _group.alpha = 0f;
                return;
            }

            string binding = CurrentBindingDisplay();
            if (bindingLabel.text != binding || verbLabel.text != verb)
            {
                bindingLabel.text = binding;
                verbLabel.text = verb;
                ResizeChip(binding, verb);
            }
            Rect area = canvasRect.rect;
            float halfWidth = _rect.rect.width * .5f;
            local.x = Mathf.Clamp(local.x, area.xMin + halfWidth + 24f,
                area.xMax - halfWidth - 24f);
            local.y = Mathf.Clamp(local.y + 16f, area.yMin + 50f, area.yMax - 50f);
            _rect.anchoredPosition = local;
            _group.alpha = 1f;
        }

        string CurrentBindingDisplay()
        {
            return _gamepadMode ? gamepadBindingDisplay : keyboardBindingDisplay;
        }

        void ResizeChip(string binding, string verb)
        {
            float keyWidth = Mathf.Clamp(bindingLabel.GetPreferredValues(binding).x + 18f, 42f, 160f);
            float verbWidth = Mathf.Clamp(verbLabel.GetPreferredValues(verb).x + 8f, 100f, 230f);
            RectTransform keycap = bindingLabel.rectTransform.parent as RectTransform;
            RectTransform verbRect = verbLabel.rectTransform;
            keycap.sizeDelta = new Vector2(keyWidth, keycap.sizeDelta.y);
            verbRect.anchoredPosition = new Vector2(keyWidth + 23f, verbRect.anchoredPosition.y);
            verbRect.sizeDelta = new Vector2(verbWidth, verbRect.sizeDelta.y);
            _rect.sizeDelta = new Vector2(keyWidth + verbWidth + 33f, _rect.sizeDelta.y);
        }

        void UpdateDeviceMode()
        {
            Gamepad pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > .15f ||
                                pad.rightStick.ReadValue().sqrMagnitude > .15f ||
                                pad.dpad.ReadValue().sqrMagnitude > .1f ||
                                pad.buttonWest.wasPressedThisFrame ||
                                pad.buttonSouth.wasPressedThisFrame ||
                                pad.buttonEast.wasPressedThisFrame ||
                                pad.buttonNorth.wasPressedThisFrame ||
                                pad.rightTrigger.wasPressedThisFrame ||
                                pad.startButton.wasPressedThisFrame))
                _gamepadMode = true;
            if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 9f))
                _gamepadMode = false;
        }

    }
}
