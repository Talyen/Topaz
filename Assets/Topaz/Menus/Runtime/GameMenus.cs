using System;
using TMPro;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using Topaz.VisualStudy;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Topaz.Menus
{
    /// <summary>Title, pause, display, and graphics navigation for the desktop study.</summary>
    public sealed class GameMenus : MonoBehaviour
    {
        enum ScreenState { Title, Game, Pause, OptionsFromTitle, OptionsFromPause }

        [SerializeField] LoopHud loopHud;
        [SerializeField] VisualOptionsMenu visualLab;
        [SerializeField] FeelStudyCamera cameraRig;
        [SerializeField] GameObject titlePanel;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject optionsPanel;
        [SerializeField] UnityEngine.UI.Button continueButton;
        [SerializeField] UnityEngine.UI.Button titleOptionsButton;
        [SerializeField] UnityEngine.UI.Button titleQuitButton;
        [SerializeField] UnityEngine.UI.Button resumeButton;
        [SerializeField] UnityEngine.UI.Button pauseOptionsButton;
        [SerializeField] UnityEngine.UI.Button mainMenuButton;
        [SerializeField] UnityEngine.UI.Button pauseQuitButton;
        [SerializeField] UnityEngine.UI.Button displayModeButton;
        [SerializeField] UnityEngine.UI.Button windowSizeButton;
        [SerializeField] UnityEngine.UI.Button cameraScaleButton;
        [SerializeField] UnityEngine.UI.Button visualLabButton;
        [SerializeField] UnityEngine.UI.Button optionsBackButton;
        [SerializeField] TMP_Text displayInfo;

        static readonly int[] Widths = { 1280, 1600, 1920 };
        static readonly int[] Heights = { 720, 900, 1080 };
        static readonly float[] CameraSizes = { 5.8f, 7.2f, 9f };
        static readonly string[] CameraNames = { "Close", "Balanced", "Wide" };

        ScreenState _state;
        int _windowIndex = 1;
        int _cameraIndex = 1;
        bool _borderless = true;

        public bool BlockGameplay => _state != ScreenState.Game;
        public bool IsTitle => _state == ScreenState.Title;
        public bool IsPaused => _state == ScreenState.Pause;

        void Awake()
        {
            if (loopHud == null || visualLab == null || cameraRig == null ||
                titlePanel == null || pausePanel == null || optionsPanel == null)
            {
                Debug.LogError("Game menus are missing a required reference.", this);
                enabled = false;
                return;
            }
            continueButton.onClick.AddListener(Continue);
            titleOptionsButton.onClick.AddListener(() => SetState(ScreenState.OptionsFromTitle));
            titleQuitButton.onClick.AddListener(Quit);
            resumeButton.onClick.AddListener(Continue);
            pauseOptionsButton.onClick.AddListener(() => SetState(ScreenState.OptionsFromPause));
            mainMenuButton.onClick.AddListener(() => SetState(ScreenState.Title));
            pauseQuitButton.onClick.AddListener(Quit);
            displayModeButton.onClick.AddListener(ToggleDisplayMode);
            windowSizeButton.onClick.AddListener(CycleWindowSize);
            cameraScaleButton.onClick.AddListener(CycleCameraScale);
            visualLabButton.onClick.AddListener(() => visualLab.Toggle());
            optionsBackButton.onClick.AddListener(BackFromOptions);
        }

        void Start()
        {
            bool editorTest = Application.isEditor || Array.Exists(Environment.GetCommandLineArgs(),
                value => value.Equals("-runTests", StringComparison.OrdinalIgnoreCase));
            if (editorTest)
            {
                SetState(ScreenState.Game);
                return;
            }
            _borderless = PlayerPrefs.GetInt("Topaz.Borderless", 1) == 1;
            _windowIndex = Mathf.Clamp(PlayerPrefs.GetInt("Topaz.WindowPreset", 1), 0, Widths.Length - 1);
            _cameraIndex = Mathf.Clamp(PlayerPrefs.GetInt("Topaz.CameraPreset", 1), 0, CameraSizes.Length - 1);
            cameraRig.SetZoom(CameraSizes[_cameraIndex]);
            ApplyDisplay();
            SetState(ScreenState.Title);
        }

        void Update()
        {
            bool shouldPause = _state != ScreenState.Game || visualLab.IsOpen;
            float target = Application.isEditor ? 1f : shouldPause ? 0f : 1f;
            if (Time.timeScale != target) Time.timeScale = target;
        }

        void OnDestroy() => Time.timeScale = 1f;

        public bool HandleEscape()
        {
            if (visualLab.IsOpen)
            {
                visualLab.Close();
                Select(_state == ScreenState.OptionsFromTitle ||
                    _state == ScreenState.OptionsFromPause ? visualLabButton : null);
                return true;
            }
            if (loopHud.MenuOpen)
            {
                loopHud.ClosePanels();
                return true;
            }
            switch (_state)
            {
                case ScreenState.Game: SetState(ScreenState.Pause); break;
                case ScreenState.Pause: SetState(ScreenState.Game); break;
                case ScreenState.OptionsFromTitle: SetState(ScreenState.Title); break;
                case ScreenState.OptionsFromPause: SetState(ScreenState.Pause); break;
            }
            return true;
        }

        public void Continue() => SetState(ScreenState.Game);

        public void ShowTitle() => SetState(ScreenState.Title);

        public void OnVisualLabOpening()
        {
            if (_state == ScreenState.OptionsFromTitle || _state == ScreenState.OptionsFromPause)
                optionsPanel.SetActive(false);
        }

        public void OnVisualLabClosed()
        {
            if (_state == ScreenState.OptionsFromTitle || _state == ScreenState.OptionsFromPause)
            {
                optionsPanel.SetActive(true);
                Select(visualLabButton);
            }
        }

        void BackFromOptions() => SetState(_state == ScreenState.OptionsFromTitle
            ? ScreenState.Title : ScreenState.Pause);

        void SetState(ScreenState state)
        {
            _state = state;
            titlePanel.SetActive(state == ScreenState.Title);
            pausePanel.SetActive(state == ScreenState.Pause);
            optionsPanel.SetActive(state == ScreenState.OptionsFromTitle ||
                state == ScreenState.OptionsFromPause);
            if (state != ScreenState.Game) loopHud.ClosePanels();
            UpdateLabels();
            if (state == ScreenState.Title) Select(continueButton);
            else if (state == ScreenState.Pause) Select(resumeButton);
            else if (state == ScreenState.OptionsFromTitle || state == ScreenState.OptionsFromPause)
                Select(displayModeButton);
            else Select(null);
            Time.timeScale = Application.isEditor || state == ScreenState.Game ? 1f : 0f;
        }

        void ToggleDisplayMode()
        {
            _borderless = !_borderless;
            ApplyDisplay();
            SavePreferences();
            UpdateLabels();
        }

        void CycleWindowSize()
        {
            _windowIndex = (_windowIndex + 1) % Widths.Length;
            _borderless = false;
            ApplyDisplay();
            SavePreferences();
            UpdateLabels();
        }

        void CycleCameraScale()
        {
            _cameraIndex = (_cameraIndex + 1) % CameraSizes.Length;
            cameraRig.SetZoom(CameraSizes[_cameraIndex]);
            SavePreferences();
            UpdateLabels();
        }

        void ApplyDisplay()
        {
            if (Application.isEditor) return;
            if (_borderless)
            {
                Resolution native = Screen.currentResolution;
                Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
            }
            else Screen.SetResolution(Widths[_windowIndex], Heights[_windowIndex],
                FullScreenMode.Windowed);
        }

        void SavePreferences()
        {
            if (Application.isEditor) return;
            PlayerPrefs.SetInt("Topaz.Borderless", _borderless ? 1 : 0);
            PlayerPrefs.SetInt("Topaz.WindowPreset", _windowIndex);
            PlayerPrefs.SetInt("Topaz.CameraPreset", _cameraIndex);
            PlayerPrefs.Save();
        }

        void UpdateLabels()
        {
            SetButton(displayModeButton, _borderless ? "Display: Borderless native" : "Display: Windowed");
            SetButton(windowSizeButton, $"Window size: {Widths[_windowIndex]} × {Heights[_windowIndex]}");
            SetButton(cameraScaleButton, $"Camera: {CameraNames[_cameraIndex]}");
            displayInfo.text = _borderless
                ? "Uses the display's native resolution. Choose Window size to switch to a window."
                : "Window size changes at the end of this frame. You can return to borderless anytime.";
        }

        static void SetButton(UnityEngine.UI.Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
        }

        static void Select(UnityEngine.UI.Button button) =>
            EventSystem.current?.SetSelectedGameObject(button == null ? null : button.gameObject);

        static void Quit()
        {
            if (!Application.isEditor) Application.Quit();
        }
    }
}
