using TMPro;
using Topaz.Menus;
using Topaz.Audio;
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
        [SerializeField] Button graphicsTabButton;
        [SerializeField] Button audioTabButton;
        [SerializeField] GameObject[] graphicsRows;
        [SerializeField] GameObject[] audioRows;
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider ambienceSlider;
        [SerializeField] Slider effectsSlider;
        [SerializeField] Toggle muteInBackgroundToggle;

        VisualLookController _controller;
        TopazAudioSettings _audio;
        GameMenus _menus;
        bool _bound;
        bool _audioTab;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Bind(VisualLookController controller)
        {
            if (_bound) return;
            _controller = controller;
            _menus = GetComponent<GameMenus>();
            _audio = GetComponent<TopazAudioSettings>();
            if (panel == null || cameraZoomDropdown == null || antiAliasingDropdown == null ||
                depthOfFieldDropdown == null || bloomToggle == null ||
                ambientOcclusionToggle == null || resetButton == null || closeButton == null ||
                status == null || loopHud == null || _menus == null || _audio == null ||
                graphicsTabButton == null || audioTabButton == null ||
                graphicsRows == null || graphicsRows.Length != 5 ||
                audioRows == null || audioRows.Length != 5 ||
                masterSlider == null || musicSlider == null || ambienceSlider == null ||
                effectsSlider == null || muteInBackgroundToggle == null)
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
            graphicsTabButton.onClick.AddListener(OpenGraphicsTab);
            audioTabButton.onClick.AddListener(OpenAudioTab);
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            ambienceSlider.onValueChanged.AddListener(OnAmbienceChanged);
            effectsSlider.onValueChanged.AddListener(OnEffectsChanged);
            muteInBackgroundToggle.onValueChanged.AddListener(OnMuteInBackgroundChanged);
            _bound = true;
            ShowTab(false);
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
            graphicsTabButton.onClick.RemoveListener(OpenGraphicsTab);
            audioTabButton.onClick.RemoveListener(OpenAudioTab);
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            ambienceSlider.onValueChanged.RemoveListener(OnAmbienceChanged);
            effectsSlider.onValueChanged.RemoveListener(OnEffectsChanged);
            muteInBackgroundToggle.onValueChanged.RemoveListener(OnMuteInBackgroundChanged);
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

        void OnMasterChanged(float value) => ChangeAudio(() => _audio.SetMaster(value));
        void OnMusicChanged(float value) => ChangeAudio(() => _audio.SetMusic(value));
        void OnAmbienceChanged(float value) => ChangeAudio(() => _audio.SetAmbience(value));
        void OnEffectsChanged(float value) => ChangeAudio(() => _audio.SetEffects(value));
        void OnMuteInBackgroundChanged(bool value) =>
            ChangeAudio(() => _audio.SetMuteInBackground(value));

        void ChangeAudio(System.Action apply)
        {
            apply();
            SetStatus("Audio settings saved automatically.");
        }

        void OpenGraphicsTab()
        {
            ShowTab(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                cameraZoomDropdown.gameObject);
        }

        void OpenAudioTab()
        {
            ShowTab(true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                masterSlider.gameObject);
        }

        void ShowTab(bool audio)
        {
            _audioTab = audio;
            foreach (GameObject row in graphicsRows) row.SetActive(!audio);
            foreach (GameObject row in audioRows) row.SetActive(audio);
            graphicsTabButton.GetComponent<Image>().color = audio
                ? new Color32(49, 37, 30, 255) : new Color32(239, 199, 132, 255);
            audioTabButton.GetComponent<Image>().color = audio
                ? new Color32(239, 199, 132, 255) : new Color32(49, 37, 30, 255);
            graphicsTabButton.GetComponentInChildren<TMP_Text>().color = audio
                ? new Color32(255, 241, 216, 255) : new Color32(23, 19, 18, 255);
            audioTabButton.GetComponentInChildren<TMP_Text>().color = audio
                ? new Color32(23, 19, 18, 255) : new Color32(255, 241, 216, 255);
        }

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
                if (_audioTab)
                {
                    _audio.ResetDefaults();
                    Refresh();
                    SetStatus("Audio defaults restored and saved.");
                    return;
                }
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
                ShowTab(false);
                Refresh();
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    graphicsTabButton.gameObject);
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
            masterSlider.SetValueWithoutNotify(_audio.Master);
            musicSlider.SetValueWithoutNotify(_audio.Music);
            ambienceSlider.SetValueWithoutNotify(_audio.Ambience);
            effectsSlider.SetValueWithoutNotify(_audio.Effects);
            muteInBackgroundToggle.SetIsOnWithoutNotify(_audio.MuteInBackground);
        }

        void SetStatus(string message)
        {
            if (status != null) status.text = message;
        }
    }
}
