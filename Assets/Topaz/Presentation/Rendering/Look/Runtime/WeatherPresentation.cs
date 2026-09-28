using Topaz.Gameplay;
using Topaz.Audio;
using UnityEngine;

namespace Topaz.Rendering
{
    /// <summary>Single fog-compatible rain emitter and ambience; driven only by active World time.</summary>
    public sealed class WeatherPresentation : MonoBehaviour
    {
        const float TransitionSeconds = 30f;
        const float RainRate=180f;
        Transform _follow;
        ParticleSystem _rainParticles;
        Material _rainMaterial;
        Texture2D _rainTexture;
        VisualLookController _look;
        AudioSource _rainAudio;
        TopazAudioOutput _rainOutput;
        AudioClip _rainClip;
        float _cloudiness;
        float _rain;
        float _targetCloudiness;
        float _targetRain;

        public float Cloudiness => _cloudiness;
        public float Rain => _rain;
        public void SetActiveWorld(bool active)
        {
            if(_rainParticles==null)return;
            if(active && _rainParticles.isPaused)_rainParticles.Play();
            else if(!active && _rainParticles.isPlaying)_rainParticles.Pause();
            if(!active)_look?.FreezePresentation();
        }

        public void Initialize(VisualLookController look, Transform follow)
        {
            if (_look != null) return;
            _look = look;
            _follow=follow;CreateRain();
            CreateAudio();
            _look.SetWeather(0f, 0f);
        }

        public void SetCondition(string condition, bool immediate = false)
        {
            _targetCloudiness = condition == WeatherSchedule.Clear ? 0f : 1f;
            _targetRain = condition == WeatherSchedule.Rain ? 1f : 0f;
            if (immediate)
            {
                _cloudiness = _targetCloudiness;
                _rain = _targetRain;
                Apply();
            }
        }

        public void Tick(float activeSeconds)
        {
            if (_look == null || activeSeconds <= 0f) return;
            float step = activeSeconds / TransitionSeconds;
            float clouds = Mathf.MoveTowards(_cloudiness, _targetCloudiness, step);
            float rain = Mathf.MoveTowards(_rain, _targetRain, step);
            _cloudiness = clouds;
            _rain = rain;
            _look.AdvancePresentation(activeSeconds, rain);
            Apply();
        }

        void LateUpdate()
        {
            if(_follow!=null && _rainParticles!=null)_rainParticles.transform.position=_follow.position+Vector3.up*15;
        }
        void Apply()
        {
            _look.SetWeather(_cloudiness, _rain);
            if(_rainParticles!=null){var emission=_rainParticles.emission;emission.rateOverTime=RainRate*_rain;}
            if (_rainOutput != null) _rainOutput.SetBaseVolume(.18f * _rain);
        }

        void CreateRain()
        {
            Material source = Resources.Load<Material>("TopazOutdoorRain");
            if (source == null)
            {
                Debug.LogError("[Topaz] Weather particle material is missing.", this);
                return;
            }
            _rainTexture = new Texture2D(2, 16, TextureFormat.RGBA32, false)
            {
                name = "Topaz Rain Streak",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < 16; y++)
            {
                float alpha = Mathf.Sin((y + .5f) * Mathf.PI / 16f) * .48f;
                for (int x = 0; x < 2; x++)
                    _rainTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            _rainTexture.Apply();
            _rainMaterial = new Material(source) { name = "Topaz Rain" };
            _rainMaterial.SetTexture("_BaseMap", _rainTexture);

            var go = new GameObject("Weather Rain", typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            _rainParticles = go.GetComponent<ParticleSystem>();
            var main = _rainParticles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.2f;
            main.startSpeed = 0f;
            main.startSize = .055f;
            main.startColor = new Color(.48f, .59f, .67f, .28f);
            main.maxParticles = 500;
            var emission = _rainParticles.emission;
            emission.rateOverTime = 0f;
            var shape = _rainParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(38f, .2f, 38f);
            var velocity = _rainParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 1.2f;
            velocity.y = -18f;
            var collision = _rainParticles.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.High;
            collision.enableDynamicColliders = true;
            collision.lifetimeLoss = 1f;
            collision.collidesWith = ~0;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _rainMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1f;
            renderer.velocityScale = .015f;
            _rainParticles.Play();
        }

        void CreateAudio()
        {
            // Original generated noise bed keeps the public prototype free of unlicensed audio.
            const int sampleRate = 22050;
            float[] samples = new float[sampleRate * 4];
            uint state = 0x6d2b79f5u;
            float fast = 0f;
            float slow = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                state = state * 1664525u + 1013904223u;
                float noise = ((state >> 8) / 16777216f) * 2f - 1f;
                fast = fast * .78f + noise * .22f;
                slow = slow * .985f + noise * .015f;
                // A filtered wash avoids the white-noise hiss and sharp random clicks.
                samples[i] = Mathf.Clamp((fast - slow) * .58f + slow * .35f,
                    -.8f, .8f);
            }
            for (int i = 0; i < 256; i++)
            {
                float fade = i / 256f;
                samples[i] *= fade;
                samples[samples.Length - 1 - i] *= fade;
            }
            _rainClip = AudioClip.Create("Topaz Rain Ambience", samples.Length, 1,
                sampleRate, false);
            _rainClip.SetData(samples, 0);
            _rainAudio = gameObject.AddComponent<AudioSource>();
            _rainAudio.clip = _rainClip;
            _rainAudio.loop = true;
            _rainAudio.playOnAwake = false;
            _rainAudio.spatialBlend = 0f;
            _rainAudio.volume = 0f;
            _rainOutput = gameObject.AddComponent<TopazAudioOutput>();
            _rainOutput.Configure(TopazAudioOutput.Category.Ambience, 0f);
            _rainAudio.Play();
        }

        void OnDestroy()
        {
            if(_rainMaterial!=null)Destroy(_rainMaterial);
            if(_rainTexture!=null)Destroy(_rainTexture);
            if (_rainClip != null) Destroy(_rainClip);
        }
    }
}
