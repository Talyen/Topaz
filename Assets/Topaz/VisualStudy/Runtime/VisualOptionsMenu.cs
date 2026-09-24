using TMPro;
using Topaz.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace Topaz.VisualStudy
{
    /// <summary>Compact Graphics menu over authored URP settings.</summary>
    public sealed class VisualOptionsMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Dropdown cameraZoomDropdown;
        [SerializeField] TMP_Dropdown antiAliasingDropdown;
        [SerializeField] TMP_Dropdown depthOfFieldDropdown;
        [SerializeField] Toggle bloomToggle;
        [SerializeField] Toggle ambientOcclusionToggle;
        [SerializeField] Button resetButton;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text status;
        [SerializeField] Topaz.LoopStudy.LoopHud loopHud;

        VisualLookController _controller;
        GameMenus _menus;
        bool _bound;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Bind(VisualLookController controller)
        {
            if (_bound) return;
            _controller = controller;
            _menus = GetComponent<GameMenus>();
            if (panel == null || cameraZoomDropdown == null || antiAliasingDropdown == null ||
                depthOfFieldDropdown == null || bloomToggle == null ||
                ambientOcclusionToggle == null || resetButton == null || closeButton == null ||
                status == null || loopHud == null || _menus == null)
            {
                Debug.LogError("Graphics menu is missing a required reference.", this);
                enabled = false;
                return;
            }
            SetOptions(cameraZoomDropdown, "Close", "Balanced", "Far");
            SetOptions(antiAliasingDropdown, "Off", "FXAA", "SMAA", "TAA",
                "MSAA 2×", "MSAA 4×", "MSAA 8×");
            SetOptions(depthOfFieldDropdown, "Off", "Gaussian", "Bokeh");
            cameraZoomDropdown.onValueChanged.AddListener(OnCameraZoomChanged);
            antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
            depthOfFieldDropdown.onValueChanged.AddListener(OnDepthOfFieldChanged);
            bloomToggle.onValueChanged.AddListener(OnBloomChanged);
            ambientOcclusionToggle.onValueChanged.AddListener(OnAmbientOcclusionChanged);
            resetButton.onClick.AddListener(ResetToDefaults);
            closeButton.onClick.AddListener(Close);
            _bound = true;
            Refresh();
        }

        void OnDestroy()
        {
            if (!_bound) return;
            cameraZoomDropdown.onValueChanged.RemoveListener(OnCameraZoomChanged);
            antiAliasingDropdown.onValueChanged.RemoveListener(OnAntiAliasingChanged);
            depthOfFieldDropdown.onValueChanged.RemoveListener(OnDepthOfFieldChanged);
            bloomToggle.onValueChanged.RemoveListener(OnBloomChanged);
            ambientOcclusionToggle.onValueChanged.RemoveListener(OnAmbientOcclusionChanged);
            resetButton.onClick.RemoveListener(ResetToDefaults);
            closeButton.onClick.RemoveListener(Close);
        }

        static void SetOptions(TMP_Dropdown dropdown, params string[] labels)
        {
            dropdown.ClearOptions();
            foreach (string label in labels)
                dropdown.options.Add(new TMP_Dropdown.OptionData(label));
            dropdown.RefreshShownValue();
        }

        void OnCameraZoomChanged(int value) => Change(() => _menus.SetCameraZoomIndex(value));
        void OnAntiAliasingChanged(int value) => Change(() => _controller.SetAa(value));
        void OnDepthOfFieldChanged(int value) => Change(() => _controller.SetDepthMode(value));
        void OnBloomChanged(bool value) => Change(() => _controller.SetBloom(value));
        void OnAmbientOcclusionChanged(bool value) =>
            Change(() => _controller.SetAmbientOcclusion(value));

        void Change(System.Action apply)
        {
            try
            {
                apply();
                _controller.SaveSelection();
                SetStatus("Saved automatically.");
                Refresh();
            }
            catch (System.Exception error)
            {
                SetStatus("Could not save settings: " + error.Message);
                Debug.LogWarning("[Topaz] Graphics preference save failed: " + error.Message, this);
            }
        }

        void ResetToDefaults()
        {
            try
            {
                _controller.ResetSelection();
                _menus.SetCameraZoomIndex(1);
                _controller.SaveSelection();
                Refresh();
                SetStatus("Defaults restored and saved.");
            }
            catch (System.Exception error)
            {
                SetStatus("Could not save defaults: " + error.Message);
                Debug.LogWarning("[Topaz] Graphics reset failed: " + error.Message, this);
            }
        }

        public void Toggle()
        {
            bool open = !IsOpen;
            loopHud.ClosePanels();
            panel.SetActive(open);
            if (open)
            {
                _menus?.OnVisualLabOpening();
                panel.transform.SetAsLastSibling();
                Refresh();
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    cameraZoomDropdown.gameObject);
            }
            else _menus?.OnVisualLabClosed();
        }

        public void Close()
        {
            panel.SetActive(false);
            _menus?.OnVisualLabClosed();
        }

        public void MarkUnsaved() => SetStatus("Applying…");

        public void Refresh()
        {
            if (_controller == null || _menus == null || !_bound) return;
            cameraZoomDropdown.SetValueWithoutNotify(_menus.CurrentCameraZoomIndex);
            antiAliasingDropdown.SetValueWithoutNotify(_controller.CurrentAa);
            depthOfFieldDropdown.SetValueWithoutNotify(_controller.CurrentDepthMode);
            bloomToggle.SetIsOnWithoutNotify(_controller.BloomEnabled);
            ambientOcclusionToggle.SetIsOnWithoutNotify(_controller.AmbientOcclusionEnabled);
        }

        void SetStatus(string message)
        {
            if (status != null) status.text = message;
        }
    }
}
