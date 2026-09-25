using System;
using System.IO;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.VisualStudy
{
    /// <summary>Live review settings over native Unity Volumes, camera AA, and lights.</summary>
    public sealed class VisualLookController : MonoBehaviour
    {
        readonly struct TimeKey
        {
            public readonly float hour;
            public readonly float sunlight;
            public readonly Color sunlightColor;
            public readonly Color ambient;
            public readonly Color fog;
            public readonly float exposure;
            public readonly float homeLight;
            public readonly float shadow;
            public readonly float pitch;
            public readonly float yaw;

            public TimeKey(float hour, float sunlight, Color sunlightColor, Color ambient,
                Color fog, float exposure, float homeLight, float shadow, float pitch, float yaw)
            {
                this.hour = hour;
                this.sunlight = sunlight;
                this.sunlightColor = sunlightColor;
                this.ambient = ambient;
                this.fog = fog;
                this.exposure = exposure;
                this.homeLight = homeLight;
                this.shadow = shadow;
                this.pitch = pitch;
                this.yaw = yaw;
            }
        }

        // Stylized key light stays above the horizon so the fixed camera retains readable forms.
        static readonly TimeKey[] TimeKeys = {
            new TimeKey(0f, .18f, new Color(.62f, .75f, 1f),
                new Color(.045f, .055f, .08f), new Color(.012f, .018f, .03f), 0f, 1.12f, .55f, 45f, -30f),
            new TimeKey(4f, .18f, new Color(.72f, .80f, 1f),
                new Color(.045f, .055f, .08f), new Color(.015f, .021f, .034f), 0f, 1.08f, .55f, 30f, -60f),
            new TimeKey(7f, 1.35f, new Color(1f, .79f, .60f),
                new Color(.30f, .33f, .38f), new Color(.26f, .29f, .33f), 0f, 1f, .8f, 42f, -40f),
            new TimeKey(12f, 1.85f, new Color(.93f, .96f, 1f),
                new Color(.34f, .36f, .40f), new Color(.28f, .32f, .34f), 0f, 1f, 1f, 50f, -30f),
            new TimeKey(19f, .38f, new Color(1f, .69f, .43f),
                new Color(.10f, .11f, .14f), new Color(.04f, .045f, .065f), -.04f, 1.04f, .8f, 28f, 45f),
            new TimeKey(22f, .18f, new Color(.62f, .75f, 1f),
                new Color(.045f, .055f, .08f), new Color(.012f, .018f, .03f), 0f, 1.12f, .55f, 45f, -30f),
            new TimeKey(24f, .18f, new Color(.62f, .75f, 1f),
                new Color(.045f, .055f, .08f), new Color(.012f, .018f, .03f), 0f, 1.12f, .55f, 45f, -30f)
        };

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
        ColorAdjustments _baseColor;
        ColorAdjustments _homeColor;
        WhiteBalance _baseBalance;
        WhiteBalance _homeBalance;
        UniversalRenderPipelineAsset _urp;
        int _originalMsaa;
        VisualStudySettings _settings = new VisualStudySettings();
        string _settingsPath;
        bool _ready;
        double _worldHours = WorldClock.StartingHour;
        float _restFade;
        float _cloudiness;
        float _rain;
        bool _interior;

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
            _baseColor = Get<ColorAdjustments>(_baseProfile);
            _homeColor = Get<ColorAdjustments>(_homeProfile);
            _baseBalance = Get<WhiteBalance>(_baseProfile);
            _homeBalance = Get<WhiteBalance>(_homeProfile);
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

        public void SetWorldHours(double hours)
        {
            _worldHours = hours;
            if (_baseProfile != null) ApplyTimeOfDay();
        }

        public void SetInterior(bool interior)
        {
            _interior = interior;
            if (_baseProfile != null) ApplyTimeOfDay();
        }

        public void SetRestFade(float darkness)
        {
            _restFade = Mathf.Clamp01(darkness);
            if (_baseProfile != null) ApplyTimeOfDay();
        }

        public void SetWeather(float cloudiness, float rain)
        {
            _cloudiness = Mathf.Clamp01(cloudiness);
            _rain = Mathf.Clamp01(rain);
            if (_baseProfile != null) ApplyTimeOfDay();
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
            _baseColor.contrast.Override(_settings.contrast);
            _baseColor.saturation.Override(_settings.saturation);
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
            ApplyTimeOfDay();
            optionsMenu?.Refresh();
        }

        void ApplyTimeOfDay()
        {
            // Scene replacement can destroy lights before WorldSession.OnDisable resets its fade.
            if (sun == null || homeLight == null) return;
            float hour = (float)WorldClock.HourOfDay(_worldHours);
            int next = 1;
            while (next < TimeKeys.Length - 1 && hour > TimeKeys[next].hour) next++;
            TimeKey a = TimeKeys[next - 1];
            TimeKey b = TimeKeys[next];
            float blend = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(a.hour, b.hour, hour));
            float visible = 1f - _restFade;
            Color ambient = Color.Lerp(a.ambient, b.ambient, blend);
            ambient = Color.Lerp(ambient,
                ambient * new Color(.84f, .90f, 1f), _cloudiness) *
                (1f - .05f * _rain) * visible;
            if (_interior) ambient *= .45f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = ambient * .36f;
            RenderSettings.ambientGroundColor = ambient * .16f;
            Color fog = Color.Lerp(a.fog, b.fog, blend);
            float sunlight = Mathf.Lerp(a.sunlight, b.sunlight, blend);
            float night = Mathf.InverseLerp(1.1f, .18f, sunlight);
            RenderSettings.fog = !_interior;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(fog,
                fog * new Color(.78f, .87f, 1f), _cloudiness) * visible;
            RenderSettings.fogStartDistance = Mathf.Lerp(26f, 12f, night);
            RenderSettings.fogEndDistance = Mathf.Max(28f,
                Mathf.Lerp(_settings.fogEnd, Mathf.Min(_settings.fogEnd, 34f), night) -
                _rain * 8f);

            sun.color = Color.Lerp(a.sunlightColor, b.sunlightColor, blend);
            sun.intensity = Mathf.Lerp(a.sunlight, b.sunlight, blend) *
                (1f - .32f * _cloudiness - .08f * _rain) * visible *
                (_interior ? .16f : 1f);
            sun.shadowStrength = _settings.shadowStrength *
                Mathf.Lerp(a.shadow, b.shadow, blend) *
                (1f - .30f * _cloudiness) * visible * (_interior ? .35f : 1f);
            sun.transform.rotation = Quaternion.Euler(
                Mathf.Lerp(a.pitch, b.pitch, blend), Mathf.Lerp(a.yaw, b.yaw, blend), 0f);
            homeLight.intensity = _settings.homeLight *
                Mathf.Lerp(a.homeLight, b.homeLight, blend) * visible;

            float exposureOffset = Mathf.Lerp(a.exposure, b.exposure, blend) -
                _cloudiness * .03f - _rain * .03f - _restFade * 8f;
            _baseColor.postExposure.Override(_settings.exposure + exposureOffset);
            _homeColor.postExposure.Override(_settings.exposure + .06f + exposureOffset);
            _baseBalance.temperature.Override(_settings.temperature);
            _homeBalance.temperature.Override(_settings.temperature + _settings.homeWarmth);
        }

        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out T component)) return component;
            throw new InvalidOperationException("Visual Study profile is missing " + typeof(T).Name);
        }

    }
}
