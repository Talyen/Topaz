using Topaz.CombatStudy;
using Topaz.FeelStudy;
using UnityEngine;

namespace Topaz.VisualStudy
{
    /// <summary>Gameplay feedback authored for the fixed-angle camera.</summary>
    public sealed class VisualEffectsComparison : MonoBehaviour
    {
        FeelStudyPlayer _player;
        PlayerCombat _combat;
        Material _dustMaterial;
        Material _sparkMaterial;
        Material _trailMaterial;
        Texture2D _dustTexture;
        Texture2D _sparkTexture;
        ParticleSystem _particles;
        ParticleSystem _sparks;
        TrailRenderer _swordTrail;
        TrailRenderer _axeTrail;
        TrailRenderer _combatAxeTrail;
        Transform _swordPivot;
        Transform _axePivot;
        bool _wasDodging;
        bool _wasAttacking;
        bool _wasSwinging;

        public void Initialize()
        {
            _player = FindFirstObjectByType<FeelStudyPlayer>();
            _combat = _player != null ? _player.GetComponent<PlayerCombat>() : null;
            if (_combat != null) _combat.WeaponHit += OnWeaponHit;
            CreateParticles();
            _particles?.Play();
            _sparks?.Play();
        }

        void Update()
        {
            if (_player != null)
            {
                bool dodging = _player.IsDodging;
                if (dodging && !_wasDodging)
                    BurstDust(_player.transform.position, 15);
                _wasDodging = dodging;
            }
            if (_combat != null && _player != null)
            {
                bool attacking = _combat.IsAttackLocked;
                if (attacking && !_wasAttacking)
                {
                    _swordTrail?.Clear();
                    _axeTrail?.Clear();
                    _combatAxeTrail?.Clear();
                }
                bool axe = _combat.EquippedToolId == "axe";
                bool combatAxe = _combat.EquippedToolId == "combat-axe";
                Transform pivot = axe ? _axePivot : _swordPivot;
                bool swinging = _combat.IsStrikeActive && pivot != null &&
                    pivot.gameObject.activeInHierarchy;
                if (swinging && !_wasSwinging)
                    BurstSparks(pivot.position + pivot.forward * 1.1f, pivot.forward, 5,
                        axe || combatAxe ? new Color(1f, .70f, .38f, 1f) :
                            new Color(.68f, .93f, 1f, 1f));
                if (_swordTrail != null)
                    _swordTrail.emitting = swinging && !axe;
                if (_axeTrail != null)
                    _axeTrail.emitting = swinging && axe;
                if (_combatAxeTrail != null)
                    _combatAxeTrail.emitting = swinging && combatAxe;
                _wasSwinging = swinging;
                _wasAttacking = attacking;
            }
        }

        void OnWeaponHit(EnemyCombatant target, WeaponDefinition weapon)
        {
            if (_player == null || target == null) return;
            bool heavy = weapon?.TwoHanded == true;
            BurstSparks(target.transform.position + Vector3.up,
                (target.transform.position - _player.transform.position).normalized,
                heavy ? 11 : 7, heavy ? new Color(1f, .71f, .43f, 1f) :
                    new Color(.86f, .88f, .76f, 1f));
        }

        void BurstDust(Vector3 position, int count)
        {
            if (_particles == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = (i + Random.Range(-.22f, .22f)) * Mathf.PI * 2f / count;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var particle = new ParticleSystem.EmitParams
                {
                    position = position + outward * .22f + Vector3.up * .12f,
                    velocity = outward * Random.Range(1.2f, 2.1f) + Vector3.up * .32f,
                    startColor = new Color(.75f, .77f, .69f, .62f),
                    startSize = Random.Range(.32f, .58f),
                    startLifetime = Random.Range(.48f, .72f)
                };
                _particles.Emit(particle, 1);
            }
        }

        void BurstSparks(Vector3 position, Vector3 direction, int count, Color color)
        {
            if (_sparks == null) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
            direction.Normalize();
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(-65f, 65f, (i + .5f) / count);
                Vector3 outward = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                var particle = new ParticleSystem.EmitParams
                {
                    position = position + outward * Random.Range(0f, .22f),
                    velocity = outward * Random.Range(1.6f, 3.1f) +
                        Vector3.up * Random.Range(-.2f, .8f),
                    startColor = color,
                    startSize = Random.Range(.16f, .28f),
                    startLifetime = Random.Range(.25f, .45f)
                };
                _sparks.Emit(particle, 1);
            }
        }

        void CreateParticles()
        {
            Material source = Resources.Load<Material>("TopazEffectsParticles");
            if (source == null)
            {
                Debug.LogError("[Topaz] Effects comparison particle material is missing.", this);
                return;
            }
            _dustTexture = MakeParticleTexture(false);
            _sparkTexture = MakeParticleTexture(true);
            _dustMaterial = new Material(source) { name = "Topaz Dodge Dust" };
            _sparkMaterial = new Material(source) { name = "Topaz Combat Glints" };
            _trailMaterial = new Material(source) { name = "Topaz Weapon Trail" };
            _dustMaterial.SetTexture("_BaseMap", _dustTexture);
            _sparkMaterial.SetTexture("_BaseMap", _sparkTexture);
            _trailMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture);
            _particles = CreateSystem("Effects Comparison Particles", _dustMaterial, false);
            _sparks = CreateSystem("Effects Comparison Sparks", _sparkMaterial, true);
            if (_player == null) return;
            foreach (Transform child in _player.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Sword Pivot")
                {
                    _swordPivot = child;
                    _swordTrail = CreateTrail(child, "Sword Slash Trail",
                        new Color(.48f, .90f, 1f, .8f));
                    _combatAxeTrail = CreateTrail(child, "Combat Axe Slash Trail",
                        new Color(1f, .67f, .35f, .72f));
                }
                else if (child.name == "Axe Pivot")
                {
                    _axePivot = child;
                    _axeTrail = CreateTrail(child, "Axe Slash Trail",
                        new Color(1f, .67f, .35f, .8f));
                }
            }
        }

        ParticleSystem CreateSystem(string name, Material material, bool streaks)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            ParticleSystem particles = go.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 90;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = .5f;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, .12f),
                    new GradientAlphaKey(.8f, .5f),
                    new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, .35f),
                    new Keyframe(.22f, 1f), new Keyframe(1f, .15f)));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = streaks
                ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (streaks)
            {
                renderer.velocityScale = .28f;
                renderer.lengthScale = 1.7f;
            }
            return particles;
        }

        TrailRenderer CreateTrail(Transform pivot, string name, Color tint)
        {
            var tip = new GameObject(name, typeof(TrailRenderer));
            tip.transform.SetParent(pivot, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            TrailRenderer trail = tip.GetComponent<TrailRenderer>();
            trail.sharedMaterial = _trailMaterial;
            trail.time = .22f;
            trail.minVertexDistance = .025f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, .32f),
                new Keyframe(.45f, .20f), new Keyframe(1f, 0f));
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(.9f, 0f),
                    new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.emitting = false;
            return trail;
        }

        static Texture2D MakeParticleTexture(bool spark)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = spark ? "Topaz Spark Shape" : "Topaz Dust Shape",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float u = (x + .5f) / 32f - 1f;
                float v = (y + .5f) / 32f - 1f;
                float radius = Mathf.Sqrt(u * u + v * v);
                float alpha;
                if (spark)
                {
                    float cross = Mathf.Max(
                        Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u)), 2f) *
                            Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v) * 7f), 2f),
                        Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v)), 2f) *
                            Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u) * 7f), 2f));
                    alpha = Mathf.Clamp01(cross + Mathf.Pow(Mathf.Clamp01(1f - radius), 4f));
                }
                else
                {
                    float irregular = .85f + .15f * Mathf.PerlinNoise(x * .16f, y * .16f);
                    alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 2f) * irregular;
                }
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            return texture;
        }

        void OnDestroy()
        {
            if (_combat != null) _combat.WeaponHit -= OnWeaponHit;
            if (_dustTexture != null) Destroy(_dustTexture);
            if (_sparkTexture != null) Destroy(_sparkTexture);
            if (_dustMaterial != null) Destroy(_dustMaterial);
            if (_sparkMaterial != null) Destroy(_sparkMaterial);
            if (_trailMaterial != null) Destroy(_trailMaterial);
            if (_swordTrail != null) Destroy(_swordTrail.gameObject);
            if (_axeTrail != null) Destroy(_axeTrail.gameObject);
        }
    }
}
