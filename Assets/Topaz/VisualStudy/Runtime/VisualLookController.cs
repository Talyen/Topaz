using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.VisualStudy
{
    /// <summary>Live review settings over native Unity Volumes, camera AA, and lights.</summary>
    public sealed class VisualLookController : MonoBehaviour
    {
        public static readonly string[] SettingNames = {
            "Color temperature", "Exposure", "Contrast", "Saturation",
            "Bloom strength", "Bloom threshold", "Vignette", "Focus blur start",
            "Focus blur end", "Focus blur radius", "Fog end distance", "Home light",
            "Shadow strength", "Home warmth"
        };
        public static readonly float[] Minimum = {
            -40f, -1f, -30f, -40f, 0f, .5f, 0f, 14f, 24f, .5f, 45f, 0f, .2f, -10f
        };
        public static readonly float[] Maximum = {
            40f, 1f, 30f, 40f, 1f, 2f, .4f, 36f, 60f, 1.5f, 100f, 16f, 1f, 35f
        };

        [SerializeField] InputActionAsset controls;
        [SerializeField] UniversalAdditionalCameraData cameraData;
        [SerializeField] Volume painterlyVolume;
        [SerializeField] Volume homeVolume;
        [SerializeField] Volume focusVolume;
        [SerializeField] ScriptableRendererFeature ambientOcclusionFeature;
        [SerializeField] ScriptableRendererFeature groundDecalFeature;
        [SerializeField] GameObject groundDetailRoot;
        [SerializeField] Light homeLight;
        [SerializeField] Light sun;
        [SerializeField] TMP_Text lookLabel;
        [SerializeField] VisualOptionsMenu optionsMenu;

        InputAction _cycleLook;
        InputAction _toggleAa;
        InputAction _options;
        VolumeProfile _baseProfile;
        VolumeProfile _homeProfile;
        VolumeProfile _focusProfile;
        VisualStudySettings _settings = new VisualStudySettings();
        string _settingsPath;
        bool _ready;

        public int CurrentLook => _settings.look;
        public int CurrentAa => _settings.antiAliasing;
        public int CurrentFocusMode => _settings.focusMode;
        public bool AmbientOcclusionEnabled => _settings.ambientOcclusion;
        public bool GroundDetailEnabled => _settings.groundDetail;
        public string SettingsPath => _settingsPath;

        void Awake()
        {
            if (controls == null || cameraData == null || painterlyVolume == null ||
                homeVolume == null || focusVolume == null || ambientOcclusionFeature == null ||
                groundDecalFeature == null ||
                groundDetailRoot == null || homeLight == null || sun == null ||
                lookLabel == null || optionsMenu == null)
            {
                Debug.LogError("Visual look study is missing a required reference.", this);
                enabled = false;
                return;
            }
            string directory = Application.isEditor
                ? Path.Combine(Application.temporaryCachePath, "TopazVisual-" + Guid.NewGuid().ToString("N"))
                : Application.persistentDataPath;
            _settingsPath = Path.Combine(directory, "visual-study-settings.json");
            InputActionMap map = controls.FindActionMap("Player", true);
            _cycleLook = map.FindAction("CycleLook", true);
            _toggleAa = map.FindAction("ToggleAA", true);
            _options = map.FindAction("VisualOptions", true);
        }

        void OnEnable()
        {
            if (_cycleLook != null) _cycleLook.performed += OnCycleLook;
            if (_toggleAa != null) _toggleAa.performed += OnToggleAa;
            if (_options != null) _options.performed += OnOptions;
        }

        void OnDisable()
        {
            if (_cycleLook != null) _cycleLook.performed -= OnCycleLook;
            if (_toggleAa != null) _toggleAa.performed -= OnToggleAa;
            if (_options != null) _options.performed -= OnOptions;
        }

        void Start()
        {
            // Volume.profile clones each authored asset for safe runtime editing.
            _baseProfile = painterlyVolume.profile;
            _homeProfile = homeVolume.profile;
            _focusProfile = focusVolume.profile;
            LoadSelection();
            Apply();
            optionsMenu.Bind(this);
            _ready = true;
        }

        void OnCycleLook(InputAction.CallbackContext context) => SetLook((_settings.look + 1) % 3);
        void OnToggleAa(InputAction.CallbackContext context) => SetAa((_settings.antiAliasing + 1) % 4);
        void OnOptions(InputAction.CallbackContext context) => optionsMenu.Toggle();

        public void SetLook(int look)
        {
            _settings.look = Mathf.Clamp(look, 0, 2);
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public void SetAa(int aa)
        {
            _settings.antiAliasing = Mathf.Clamp(aa, 0, 3);
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public void ToggleDepthOfField() => SetLook(_settings.look == 2 ? 1 : 2);

        public void ToggleFocusMode()
        {
            _settings.focusMode = 1 - _settings.focusMode;
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public void ToggleAmbientOcclusion()
        {
            _settings.ambientOcclusion = !_settings.ambientOcclusion;
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public void ToggleGroundDetail()
        {
            _settings.groundDetail = !_settings.groundDetail;
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public string GetSettingName(int index) => _settings.focusMode == 1 ? index switch
        {
            7 => "Bokeh focus distance", 8 => "Bokeh aperture", 9 => "Bokeh focal length",
            _ => SettingNames[index]
        } : SettingNames[index];

        public float GetMinimum(int index) => _settings.focusMode == 1 ? index switch
        {
            7 => 10f, 8 => 1f, 9 => 35f, _ => Minimum[index]
        } : Minimum[index];

        public float GetMaximum(int index) => _settings.focusMode == 1 ? index switch
        {
            7 => 35f, 8 => 16f, 9 => 150f, _ => Maximum[index]
        } : Maximum[index];

        public bool UsesWholeNumbers(int index) => _settings.focusMode == 1 && (index == 7 || index == 9)
            || _settings.focusMode == 0 && (index == 0 || index == 2 || index == 3 ||
                index == 7 || index == 8 || index == 10 || index == 13);

        public float GetSetting(int index) => index switch
        {
            0 => _settings.temperature, 1 => _settings.exposure,
            2 => _settings.contrast, 3 => _settings.saturation,
            4 => _settings.bloomIntensity, 5 => _settings.bloomThreshold,
            6 => _settings.vignette, 7 => _settings.focusMode == 1 ? _settings.bokehFocusDistance : _settings.depthStart,
            8 => _settings.focusMode == 1 ? _settings.bokehAperture : _settings.depthEnd,
            9 => _settings.focusMode == 1 ? _settings.bokehFocalLength : _settings.depthRadius,
            10 => _settings.fogEnd, 11 => _settings.homeLight,
            12 => _settings.shadowStrength, 13 => _settings.homeWarmth,
            _ => 0f
        };

        public void SetSetting(int index, float value)
        {
            if (index < 0 || index >= SettingNames.Length) return;
            value = Mathf.Clamp(value, GetMinimum(index), GetMaximum(index));
            switch (index)
            {
                case 0: _settings.temperature = value; break;
                case 1: _settings.exposure = value; break;
                case 2: _settings.contrast = value; break;
                case 3: _settings.saturation = value; break;
                case 4: _settings.bloomIntensity = value; break;
                case 5: _settings.bloomThreshold = value; break;
                case 6: _settings.vignette = value; break;
                case 7: if (_settings.focusMode == 1) _settings.bokehFocusDistance = value;
                    else _settings.depthStart = value; break;
                case 8: if (_settings.focusMode == 1) _settings.bokehAperture = value;
                    else _settings.depthEnd = value; break;
                case 9: if (_settings.focusMode == 1) _settings.bokehFocalLength = value;
                    else _settings.depthRadius = value; break;
                case 10: _settings.fogEnd = value; break;
                case 11: _settings.homeLight = value; break;
                case 12: _settings.shadowStrength = value; break;
                case 13: _settings.homeWarmth = value; break;
            }
            if (_settings.focusMode == 0 && index == 7 && _settings.depthEnd <= _settings.depthStart)
                _settings.depthEnd = Mathf.Min(Maximum[8], _settings.depthStart + 1f);
            if (_settings.focusMode == 0 && index == 8 && _settings.depthEnd <= _settings.depthStart)
                _settings.depthStart = Mathf.Max(Minimum[7], _settings.depthEnd - 1f);
            Apply();
        }

        public string FormatSetting(int index) => index switch
        {
            7 or 8 or 9 when _settings.focusMode == 1 && (index == 7 || index == 9)
                => GetSetting(index).ToString("0"),
            7 or 8 or 9 when _settings.focusMode == 1 => GetSetting(index).ToString("0.00"),
            0 or 2 or 3 or 7 or 8 or 10 or 13 => GetSetting(index).ToString("0"),
            _ => GetSetting(index).ToString("0.00")
        };

        public void ResetSelection()
        {
            _settings = new VisualStudySettings();
            Apply();
        }

        public void SaveSelection()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            string temporary = SettingsPath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(_settings, true));
            if (File.Exists(SettingsPath)) File.Replace(temporary, SettingsPath, SettingsPath + ".bak");
            else File.Move(temporary, SettingsPath);
        }

        public void CopySelection() => GUIUtility.systemCopyBuffer = JsonUtility.ToJson(_settings, true);

        void LoadSelection()
        {
            if (!File.Exists(SettingsPath)) return;
            try
            {
                var loaded = new VisualStudySettings();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath), loaded);
                if (loaded.version < 1 || loaded.version > VisualStudySettings.CurrentVersion) return;
                loaded.version = VisualStudySettings.CurrentVersion;
                _settings = loaded;
                _settings.look = Mathf.Clamp(_settings.look, 0, 2);
                _settings.antiAliasing = Mathf.Clamp(_settings.antiAliasing, 0, 3);
                _settings.focusMode = Mathf.Clamp(_settings.focusMode, 0, 1);
                for (int i = 0; i < SettingNames.Length; i++)
                    SetSetting(i, GetSetting(i));
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Topaz] Visual Lab settings could not be loaded: {error.Message}");
            }
        }

        void Apply()
        {
            if (_baseProfile == null) return;
            painterlyVolume.enabled = _settings.look != 0;
            homeVolume.enabled = _settings.look != 0;
            focusVolume.enabled = _settings.look == 2;
            ambientOcclusionFeature.SetActive(_settings.ambientOcclusion);
            groundDecalFeature.SetActive(_settings.groundDetail);
            groundDetailRoot.SetActive(_settings.groundDetail);
            cameraData.antialiasing = _settings.antiAliasing switch
            {
                1 => AntialiasingMode.FastApproximateAntialiasing,
                2 => AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                3 => AntialiasingMode.TemporalAntiAliasing,
                _ => AntialiasingMode.None
            };
            Get<WhiteBalance>(_baseProfile).temperature.Override(_settings.temperature);
            Get<WhiteBalance>(_homeProfile).temperature.Override(_settings.temperature + _settings.homeWarmth);
            Get<ColorAdjustments>(_baseProfile).postExposure.Override(_settings.exposure);
            Get<ColorAdjustments>(_homeProfile).postExposure.Override(_settings.exposure + .06f);
            Get<ColorAdjustments>(_baseProfile).contrast.Override(_settings.contrast);
            Get<ColorAdjustments>(_baseProfile).saturation.Override(_settings.saturation);
            Get<Bloom>(_baseProfile).intensity.Override(_settings.bloomIntensity);
            Get<Bloom>(_baseProfile).threshold.Override(_settings.bloomThreshold);
            Get<Vignette>(_baseProfile).intensity.Override(_settings.vignette);
            DepthOfField depth = Get<DepthOfField>(_focusProfile);
            depth.mode.Override(_settings.focusMode == 1
                ? DepthOfFieldMode.Bokeh : DepthOfFieldMode.Gaussian);
            depth.gaussianStart.Override(_settings.depthStart);
            depth.gaussianEnd.Override(Mathf.Max(_settings.depthStart + 1f, _settings.depthEnd));
            depth.gaussianMaxRadius.Override(_settings.depthRadius);
            depth.focusDistance.Override(_settings.bokehFocusDistance);
            depth.aperture.Override(_settings.bokehAperture);
            depth.focalLength.Override(_settings.bokehFocalLength);
            RenderSettings.fogEndDistance = _settings.fogEnd;
            homeLight.intensity = _settings.homeLight;
            sun.shadowStrength = _settings.shadowStrength;
            UpdateLabel();
            optionsMenu?.Refresh();
        }

        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out T component)) return component;
            throw new InvalidOperationException("Visual Study profile is missing " + typeof(T).Name);
        }

        void UpdateLabel()
        {
            string name = _settings.look == 0 ? "Lighting only" :
                _settings.look == 1 ? "Painterly" :
                _settings.focusMode == 1 ? "Bokeh preview" : "Focus preview";
            string aa = _settings.antiAliasing switch
            {
                1 => "FXAA", 2 => "SMAA", 3 => "TAA", _ => "Off"
            };
            lookLabel.text = $"LOOK  {name}  •  AA {aa}\nF5 look   F6 AA   F7 options";
        }
    }
}
