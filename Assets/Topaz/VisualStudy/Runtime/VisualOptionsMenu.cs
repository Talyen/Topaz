using TMPro;
using UnityEngine;

namespace Topaz.VisualStudy
{
    /// <summary>Temporary uGUI controls for choosing art-study defaults in a player.</summary>
    public sealed class VisualOptionsMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Slider[] sliders;
        [SerializeField] TMP_Text[] values;
        [SerializeField] UnityEngine.UI.Button lookButton;
        [SerializeField] UnityEngine.UI.Button aaButton;
        [SerializeField] UnityEngine.UI.Button depthButton;
        [SerializeField] UnityEngine.UI.Button resetButton;
        [SerializeField] UnityEngine.UI.Button saveButton;
        [SerializeField] UnityEngine.UI.Button copyButton;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] TMP_Text status;
        [SerializeField] Topaz.LoopStudy.LoopHud loopHud;

        VisualLookController _controller;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Bind(VisualLookController controller)
        {
            _controller = controller;
            for (int i = 0; i < sliders.Length; i++)
            {
                int index = i;
                sliders[i].onValueChanged.AddListener(value =>
                {
                    _controller.SetSetting(index, value);
                    SetStatus("Unsaved changes");
                });
            }
            lookButton.onClick.AddListener(() => _controller.SetLook((_controller.CurrentLook + 1) % 3));
            aaButton.onClick.AddListener(() => _controller.SetAa((_controller.CurrentAa + 1) % 4));
            depthButton.onClick.AddListener(_controller.ToggleDepthOfField);
            resetButton.onClick.AddListener(() =>
            {
                _controller.ResetSelection();
                SetStatus("Project defaults restored; save to keep them.");
            });
            saveButton.onClick.AddListener(() =>
            {
                try { _controller.SaveSelection(); SetStatus("Saved. These values load next launch."); }
                catch (System.Exception error) { SetStatus("Save failed: " + error.Message); }
            });
            copyButton.onClick.AddListener(() =>
            {
                _controller.CopySelection();
                SetStatus("Settings copied to clipboard.");
            });
            closeButton.onClick.AddListener(Close);
            Refresh();
        }

        public void Toggle()
        {
            bool open = !IsOpen;
            loopHud.ClosePanels();
            panel.SetActive(open);
            if (open) Refresh();
        }

        public void Close() => panel.SetActive(false);

        public void MarkUnsaved() => SetStatus("Unsaved changes");

        public void Refresh()
        {
            if (_controller == null) return;
            for (int i = 0; i < sliders.Length; i++)
            {
                sliders[i].SetValueWithoutNotify(_controller.GetSetting(i));
                values[i].text = _controller.FormatSetting(i);
            }
            SetButton(lookButton, _controller.CurrentLook switch
            {
                0 => "Look: Lighting only", 1 => "Look: Painterly", _ => "Look: Focus preview"
            });
            SetButton(aaButton, _controller.CurrentAa switch
            {
                1 => "AA: FXAA", 2 => "AA: SMAA", 3 => "AA: TAA", _ => "AA: Off"
            });
            SetButton(depthButton, _controller.CurrentLook == 2
                ? "Depth of field: On" : "Depth of field: Off");
        }

        void SetStatus(string message) => status.text = message;

        static void SetButton(UnityEngine.UI.Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
        }
    }
}
