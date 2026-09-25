using UnityEngine;

namespace Topaz.Audio
{
    /// <summary>Routes one Unity AudioSource through a player-controlled category gain.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class TopazAudioOutput : MonoBehaviour
    {
        public enum Category { Music, Ambience, Effects }

        [SerializeField] Category category = Category.Effects;
        [SerializeField, Range(0f, 1f)] float baseVolume = 1f;
        AudioSource _source;

        public Category OutputCategory => category;
        public float BaseVolume => baseVolume;

        void Awake() => _source = GetComponent<AudioSource>();
        void OnEnable() => Apply();

        public void Configure(Category outputCategory, float volume)
        {
            category = outputCategory;
            SetBaseVolume(volume);
        }

        public void SetBaseVolume(float volume)
        {
            baseVolume = Mathf.Clamp01(volume);
            Apply();
        }

        public void Apply()
        {
            if (_source == null) _source = GetComponent<AudioSource>();
            if (_source != null)
                _source.volume = baseVolume * (TopazAudioSettings.Instance?.Gain(category) ?? 1f);
        }
    }
}
