using UnityEngine;

namespace Topaz.Audio
{
    /// <summary>Local audio preferences shared by the title, outdoor scenes, and crypt.</summary>
    public sealed class TopazAudioSettings : MonoBehaviour
    {
        const string Prefix = "Topaz.Audio.";
        public static TopazAudioSettings Instance { get; private set; }

        [SerializeField, Range(0f, 1f)] float master = 1f;
        [SerializeField, Range(0f, 1f)] float music = 1f;
        [SerializeField, Range(0f, 1f)] float ambience = 1f;
        [SerializeField, Range(0f, 1f)] float effects = 1f;
        [SerializeField] bool muteInBackground = true;

        bool _focused = true;
        bool _paused;
        float _saveAt;

        public float Master => master;
        public float Music => music;
        public float Ambience => ambience;
        public float Effects => effects;
        public bool MuteInBackground => muteInBackground;
        public float EffectsGain => effects;

        void Awake()
        {
            Instance = this;
            master = PlayerPrefs.GetFloat(Prefix + "Master", 1f);
            music = PlayerPrefs.GetFloat(Prefix + "Music", 1f);
            ambience = PlayerPrefs.GetFloat(Prefix + "Ambience", 1f);
            effects = PlayerPrefs.GetFloat(Prefix + "Effects", 1f);
            muteInBackground = PlayerPrefs.GetInt(Prefix + "MuteInBackground", 1) != 0;
        }

        void Start() => Apply();

        void Update()
        {
            if (_saveAt <= 0f || Time.unscaledTime < _saveAt) return;
            PlayerPrefs.Save();
            _saveAt = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                if (_saveAt > 0f) PlayerPrefs.Save();
                Instance = null;
                AudioListener.volume = 1f;
            }
        }

        void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused && _saveAt > 0f) FlushPreferences();
            ApplyMaster();
        }

        void OnApplicationPause(bool paused)
        {
            _paused = paused;
            if (paused && _saveAt > 0f) FlushPreferences();
            ApplyMaster();
        }

        void FlushPreferences()
        {
            PlayerPrefs.Save();
            _saveAt = 0f;
        }

        public float Gain(TopazAudioOutput.Category category) => category switch
        {
            TopazAudioOutput.Category.Music => music,
            TopazAudioOutput.Category.Ambience => ambience,
            _ => effects
        };

        public void SetMaster(float value) { master = Save("Master", value); ApplyMaster(); }
        public void SetMusic(float value) { music = Save("Music", value); ApplyOutputs(); }
        public void SetAmbience(float value) { ambience = Save("Ambience", value); ApplyOutputs(); }
        public void SetEffects(float value) { effects = Save("Effects", value); ApplyOutputs(); }

        public void SetMuteInBackground(bool value)
        {
            muteInBackground = value;
            PlayerPrefs.SetInt(Prefix + "MuteInBackground", value ? 1 : 0);
            _saveAt = Time.unscaledTime + .4f;
            ApplyMaster();
        }

        public void ResetDefaults()
        {
            master = Save("Master", 1f);
            music = Save("Music", 1f);
            ambience = Save("Ambience", 1f);
            effects = Save("Effects", 1f);
            SetMuteInBackground(true);
            Apply();
        }

        float Save(string key, float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Prefix + key, value);
            _saveAt = Time.unscaledTime + .4f;
            return value;
        }

        void Apply()
        {
            ApplyMaster();
            ApplyOutputs();
        }

        void ApplyMaster() => AudioListener.volume =
            muteInBackground && (!_focused || _paused) ? 0f : master;

        void ApplyOutputs()
        {
            foreach (TopazAudioOutput output in FindObjectsByType<TopazAudioOutput>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                output.Apply();
        }
    }
}
