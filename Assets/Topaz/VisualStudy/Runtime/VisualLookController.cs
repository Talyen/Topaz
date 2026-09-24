using System;
using System.IO;
using UnityEngine;
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

        [SerializeField] UniversalAdditionalCameraData cameraData;
        [SerializeField] Volume painterlyVolume;
        [SerializeField] Volume homeVolume;
        [SerializeField] Volume focusVolume;
        [SerializeField] ScriptableRendererFeature ambientOcclusionFeature;
        [SerializeField] Light homeLight;
        [SerializeField] Light sun;
        [SerializeField] VisualOptionsMenu optionsMenu;

        VolumeProfile _baseProfile;
        VolumeProfile _homeProfile;
        VolumeProfile _focusProfile;
        UniversalRenderPipelineAsset _urp;
        int _originalMsaa;
        VisualStudySettings _settings = new VisualStudySettings();
        string _settingsPath;
        bool _ready;

        public int CurrentLook => _settings.look;
        public int CurrentAa => _settings.antiAliasing;
        public int CurrentDepthMode => _settings.look == 2
            ? (_settings.focusMode == 1 ? 2 : 1) : 0;
        public int CurrentFocusMode => _settings.focusMode;
        public bool AmbientOcclusionEnabled => _settings.ambientOcclusion;
        public bool BloomEnabled => _settings.bloomEnabled;
        public string SettingsPath => _settingsPath;

        void Awake()
        {
            if (cameraData == null || painterlyVolume == null || homeVolume == null ||
                focusVolume == null || ambientOcclusionFeature == null ||
                homeLight == null || sun == null || optionsMenu == null)
            {
                Debug.LogError("Visual look study is missing a required reference.", this);
                enabled = false;
                return;
            }
            string directory = Application.isEditor
                ? Path.Combine(Application.temporaryCachePath, "TopazVisual-" + Guid.NewGuid().ToString("N"))
                : Application.persistentDataPath;
            _settingsPath = Path.Combine(directory, "visual-study-settings.json");
        }

        void Start()
        {
            // Volume.profile clones each authored asset for safe runtime editing.
            _baseProfile = painterlyVolume.profile;
            _homeProfile = homeVolume.profile;
            _focusProfile = focusVolume.profile;
            _urp = UniversalRenderPipeline.asset;
            _originalMsaa = _urp != null ? _urp.msaaSampleCount : 1;
            LoadSelection();
            gameObject.AddComponent<VisualEffectsComparison>().Initialize();
            Apply();
            optionsMenu.Bind(this);
            _ready = true;
        }

        public void SetLook(int look)
        {
            _settings.look = Mathf.Clamp(look, 0, 2);
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        void OnDestroy()
        {
            if (_urp != null) _urp.msaaSampleCount = _originalMsaa;
        }

        public void SetAa(int aa)
        {
            _settings.antiAliasing = Mathf.Clamp(aa, 0, 6);
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public void SetDepthMode(int mode)
        {
            mode = Mathf.Clamp(mode, 0, 2);
            _settings.look = mode == 0 ? 1 : 2;
            _settings.focusMode = mode == 2 ? 1 : 0;
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public void SetAmbientOcclusion(bool enabled)
        {
            _settings.ambientOcclusion = enabled;
            Apply();
            if (_ready) optionsMenu.MarkUnsaved();
        }

        public void SetBloom(bool enabled)
        {
            _settings.bloomEnabled = enabled;
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
                if (loaded.version < 4 && loaded.focusMode == 0 &&
                    Mathf.Approximately(loaded.depthStart, 24f) &&
                    Mathf.Approximately(loaded.depthEnd, 30f) &&
                    Mathf.Approximately(loaded.depthRadius, 1f))
                {
                    loaded.depthStart = 30f;
                    loaded.depthEnd = 40f;
                    loaded.depthRadius = .5f;
                }
                if (loaded.version < 5 &&
                    Mathf.Approximately(loaded.bokehAperture, 1.25f) &&
                    Mathf.Approximately(loaded.bokehFocalLength, 145f))
                {
                    loaded.bokehAperture = 2.8f;
                    loaded.bokehFocalLength = 120f;
                }
                loaded.version = VisualStudySettings.CurrentVersion;
                _settings = loaded;
                _settings.look = _settings.look == 2 ? 2 : 1;
                _settings.antiAliasing = Mathf.Clamp(_settings.antiAliasing, 0, 6);
                _settings.focusMode = Mathf.Clamp(_settings.focusMode, 0, 1);
                for (int i = 0; i < SettingNames.Length; i++)
                    SetSetting(i, GetSetting(i));
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Topaz] Graphics settings could not be loaded: {error.Message}");
            }
        }

        void Apply()
        {
            if (_baseProfile == null) return;
            painterlyVolume.enabled = true;
            homeVolume.enabled = true;
            focusVolume.enabled = _settings.look == 2;
            ambientOcclusionFeature.SetActive(_settings.ambientOcclusion);
            int msaa = _settings.antiAliasing switch
            {
                4 => 2, 5 => 4, 6 => 8, _ => 1
            };
            if (_urp != null) _urp.msaaSampleCount = msaa;
            Camera camera = cameraData.GetComponent<Camera>();
            if (camera != null) camera.allowMSAA = msaa > 1;
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
            Get<Bloom>(_baseProfile).intensity.Override(
                _settings.bloomEnabled ? _settings.bloomIntensity : 0f);
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
            depth.bladeCount.Override(6);
            RenderSettings.fogEndDistance = _settings.fogEnd;
            homeLight.intensity = _settings.homeLight;
            sun.shadowStrength = _settings.shadowStrength;
            optionsMenu?.Refresh();
        }

        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out T component)) return component;
            throw new InvalidOperationException("Visual Study profile is missing " + typeof(T).Name);
        }

    }
}
