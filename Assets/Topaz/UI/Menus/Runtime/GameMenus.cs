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
        enum ScreenState { Title, Selection, Game, Pause, OptionsFromTitle, OptionsFromPause }

        [SerializeField] LoopHud loopHud;
        [SerializeField] VisualOptionsMenu visualLab;
        [SerializeField] FeelStudyCamera cameraRig;
        [SerializeField] WorldSession session;
        [SerializeField] CharacterWorldMenu selection;
        [SerializeField] MainMenuStage menuStage;
        [SerializeField] GameObject titlePanel;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject optionsPanel;
        [SerializeField] UnityEngine.UI.Button continueButton;
        [SerializeField] UnityEngine.UI.Button playButton;
        [SerializeField] UnityEngine.UI.Button titleOptionsButton;
        [SerializeField] UnityEngine.UI.Button titleQuitButton;
        [SerializeField] UnityEngine.UI.Button resumeButton;
        [SerializeField] UnityEngine.UI.Button pauseBackpackButton;
        [SerializeField] UnityEngine.UI.Button pauseOptionsButton;
        [SerializeField] UnityEngine.UI.Button mainMenuButton;
        [SerializeField] UnityEngine.UI.Button pauseQuitButton;
        [SerializeField] UnityEngine.UI.Button displayModeButton;
        [SerializeField] UnityEngine.UI.Button windowSizeButton;
        [SerializeField] UnityEngine.UI.Button uiScaleButton;
        [SerializeField] UnityEngine.UI.Button visualLabButton;
        [SerializeField] UnityEngine.UI.Button optionsBackButton;
        [SerializeField] TMP_Text displayInfo;
        [SerializeField] TMP_Text titleStatus;
        [SerializeField] TMP_Text continueDetail;

        static readonly int[] Widths = { 1280, 1600, 1920 };
        static readonly int[] Heights = { 720, 900, 1080 };
        static readonly float[] CameraSizes = { 5.8f, 7.2f, 9f };
        static readonly float[] UiScales = { 1f, 1.25f, 1.5f };
        static readonly Vector2 UiReferenceResolution = new Vector2(1920f, 1080f);

        ScreenState _state;
        int _windowIndex = 1;
        int _cameraIndex = 1;
        int _uiScaleIndex;
        bool _borderless = true;
        bool _inventoryFromPause;

        public bool BlockGameplay => _state != ScreenState.Game || _inventoryFromPause;
        public bool IsTitle => _state == ScreenState.Title;
        public bool IsPaused => _state == ScreenState.Pause;
        public int CurrentCameraZoomIndex => _cameraIndex;
        public int CurrentUiScaleIndex => _uiScaleIndex;

        void Awake()
        {
            if (loopHud == null || visualLab == null || cameraRig == null ||
                titlePanel == null || pausePanel == null || optionsPanel == null ||
                session == null || selection == null || menuStage == null || playButton == null)
            {
                Debug.LogError("Game menus are missing a required reference.", this);
                enabled = false;
                return;
            }
            continueButton.onClick.AddListener(Continue);
            playButton.onClick.AddListener(OpenPlay);
            titleOptionsButton.onClick.AddListener(() => SetState(ScreenState.OptionsFromTitle));
            titleQuitButton.onClick.AddListener(Quit);
            resumeButton.onClick.AddListener(Continue);
            pauseBackpackButton.onClick.AddListener(OpenPauseBackpack);
            pauseOptionsButton.onClick.AddListener(() => SetState(ScreenState.OptionsFromPause));
            mainMenuButton.onClick.AddListener(ReturnToTitle);
            pauseQuitButton.onClick.AddListener(Quit);
            displayModeButton.onClick.AddListener(ToggleDisplayMode);
            windowSizeButton.onClick.AddListener(CycleWindowSize);
            if (uiScaleButton != null) uiScaleButton.onClick.AddListener(CycleUiScale);
            visualLabButton.onClick.AddListener(() => visualLab.Toggle());
            optionsBackButton.onClick.AddListener(BackFromOptions);
        }

        void Start()
        {
            _uiScaleIndex = Application.isEditor ? 0 : Mathf.Clamp(
                PlayerPrefs.GetInt("Topaz.UiScalePreset", 0), 0, UiScales.Length - 1);
            ApplyUiScale();
            bool editorTest = Application.isEditor || Array.Exists(Environment.GetCommandLineArgs(),
                value => value.Equals("-runTests", StringComparison.OrdinalIgnoreCase));
            if (editorTest)
            {
                session.EnterEditorTestPair();
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
            if (_inventoryFromPause && !loopHud.MenuOpen)
            {
                _inventoryFromPause = false;
                SetState(ScreenState.Pause);
            }
            bool travelPause = loopHud.TravelOpen || session.IsFastTraveling;
            bool shouldPause = _state != ScreenState.Game || visualLab.IsOpen ||
                _inventoryFromPause || travelPause;
            float target = travelPause || (!Application.isEditor && shouldPause) ? 0f : 1f;
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
                if (_inventoryFromPause)
                {
                    _inventoryFromPause = false;
                    SetState(ScreenState.Pause);
                }
                return true;
            }
            switch (_state)
            {
                case ScreenState.Game: SetState(ScreenState.Pause); break;
                case ScreenState.Pause: SetState(ScreenState.Game); break;
                case ScreenState.OptionsFromTitle: SetState(ScreenState.Title); break;
                case ScreenState.OptionsFromPause: SetState(ScreenState.Pause); break;
                case ScreenState.Selection: selection.Back(); break;
            }
            return true;
        }

        public void Continue()
        {
            if (session.HasActivePair)
            {
                SetState(ScreenState.Game);
                return;
            }
            if (!session.HasLastPair)
            {
                OpenPlay();
                return;
            }
            continueButton.interactable = false;
            StartCoroutine(session.EnterPair(session.LastCharacterId, session.LastWorldId,
                null, false, success =>
                {
                    if (success) SetState(ScreenState.Game);
                    else
                    {
                        if (titleStatus != null) titleStatus.text = session.SaveProblem ??
                            "Could not enter this Character and World.";
                        continueButton.interactable = true;
                    }
                }));
        }

        void OpenPlay() => SetState(ScreenState.Selection);

        void ReturnToTitle()
        {
            session.Commit();
            session.FlushCurrent();
            SetState(ScreenState.Title);
        }

        public void EnterFromSelection() => SetState(ScreenState.Game);

        void OpenPauseBackpack()
        {
            SetState(ScreenState.Game);
            loopHud.ToggleInventoryPanel();
            _inventoryFromPause = true;
        }

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
            _inventoryFromPause = false;
            if (state != ScreenState.Selection) selection.Close();
            _state = state;
            bool showMenuScene = state == ScreenState.Title || state == ScreenState.Selection ||
                state == ScreenState.OptionsFromTitle;
            menuStage.SetVisible(showMenuScene);
            if (state == ScreenState.Title) menuStage.ShowLook(session.LastAppearanceId);
            titlePanel.SetActive(state == ScreenState.Title);
            pausePanel.SetActive(state == ScreenState.Pause);
            optionsPanel.SetActive(state == ScreenState.OptionsFromTitle ||
                state == ScreenState.OptionsFromPause);
            if (state == ScreenState.Selection) selection.Open();
            if (state != ScreenState.Game) loopHud.ClosePanels();
            UpdateLabels();
            if (state == ScreenState.Title) Select(continueButton.interactable
                ? continueButton : playButton);
            else if (state == ScreenState.Pause) Select(resumeButton);
            else if (state == ScreenState.OptionsFromTitle || state == ScreenState.OptionsFromPause)
                Select(displayModeButton);
            else if (state != ScreenState.Selection) Select(null);
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

        void CycleUiScale() => SetUiScaleIndex((_uiScaleIndex + 1) % UiScales.Length);

        public void SetUiScaleIndex(int index)
        {
            _uiScaleIndex = Mathf.Clamp(index, 0, UiScales.Length - 1);
            ApplyUiScale();
            SavePreferences();
            UpdateLabels();
            Select(uiScaleButton);
        }

        void ApplyUiScale()
        {
            var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiReferenceResolution / UiScales[_uiScaleIndex];
        }

        public void SetCameraZoomIndex(int index)
        {
            _cameraIndex = Mathf.Clamp(index, 0, CameraSizes.Length - 1);
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
            PlayerPrefs.SetInt("Topaz.UiScalePreset", _uiScaleIndex);
            PlayerPrefs.Save();
        }

        void UpdateLabels()
        {
            SetButton(displayModeButton, _borderless ? "Display: Borderless native" : "Display: Windowed");
            SetButton(windowSizeButton, $"Window size: {Widths[_windowIndex]} × {Heights[_windowIndex]}");
            if (uiScaleButton != null)
                SetButton(uiScaleButton, $"UI Scale: {Mathf.RoundToInt(UiScales[_uiScaleIndex] * 100f)}%");
            displayInfo.text = _borderless
                ? "Uses the display's native resolution."
                : "Window size applies immediately.";
            continueButton.interactable = session.HasActivePair || session.HasLastPair;
            if (continueDetail != null)
            {
                string characterName = null;
                string worldName = null;
                if (session.Characters != null)
                    foreach (var character in session.Characters)
                        if (character.id == session.LastCharacterId)
                        {
                            characterName = character.label;
                            break;
                        }
                if (session.Worlds != null)
                    foreach (var world in session.Worlds)
                        if (world.id == session.LastWorldId)
                        {
                            worldName = world.label;
                            break;
                        }
                continueDetail.text = characterName != null && worldName != null
                    ? characterName + "  ·  " + worldName : "Last journey";
            }
            playButton.interactable = session.SaveProblem == null;
            if (titleStatus != null) titleStatus.text = session.SaveProblem ?? "";
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
