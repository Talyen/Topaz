using Topaz.LoopStudy;
using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>One authored area spell, shared by the Mage and an equipped player staff.</summary>
    public sealed class GroundSpellAbility : MonoBehaviour
    {
        [SerializeField] GroundSpellDefinition definition;
        [SerializeField] Material ringMaterial;
        [SerializeField] Material runeMaterial;
        [SerializeField] Material particleMaterial;
        [SerializeField] AudioClip castClip;
        [SerializeField] AudioClip impactClip;

        static readonly Color EnemyRim = new Color(1f, .24f, .25f, 1f);
        static readonly Color PlayerRim = new Color(.35f, .85f, 1f, 1f);
        static readonly Color Rune = new Color(.72f, .36f, 1f, 1f);
        const int Segments = 48;

        LineRenderer _rim;
        LineRenderer _rune;
        ParticleSystem _burst;
        Light _flash;
        AudioSource _audio;
        Vector3 _center;
        float _radius;
        float _impactAt;
        float _recoverAt;
        float _readyAt;
        int _damage;
        bool _hostile;
        bool _casting;
        bool _recovering;

        public GroundSpellDefinition Definition => definition;
        public bool IsCasting => _casting;
        public bool IsRecovering => _recovering;
        public bool CanCast => definition != null && !_casting && !_recovering &&
            Time.time >= _readyAt;

        void Awake()
        {
            _rim = CreateLine("Spell Warning Rim", ringMaterial, .12f);
            _rune = CreateLine("Spell Rune", runeMaterial, .055f);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = GetComponent<PlayerCombat>() == null ? .7f : 0f;
            _audio.maxDistance = 22f;
            var burst = new GameObject("Spell Impact", typeof(ParticleSystem));
            burst.transform.SetParent(transform, false);
            _burst = burst.GetComponent<ParticleSystem>();
            var main = _burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = .38f;
            main.startSpeed = 2.4f;
            main.startSize = .16f;
            main.startColor = Rune;
            main.maxParticles = 36;
            var emission = _burst.emission;
            emission.enabled = false;
            var shape = _burst.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .45f;
            var renderer = _burst.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            _flash = burst.AddComponent<Light>();
            _flash.type = LightType.Point;
            _flash.color = Rune;
            _flash.range = 3f;
            _flash.intensity = 0f;
            _flash.shadows = LightShadows.None;
        }

        LineRenderer CreateLine(string name, Material material, float width)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            go.transform.SetParent(transform, false);
            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = Segments;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        public bool Begin(Vector3 center, bool hostile, int damage, float bonusRadius = 0f)
        {
            if (!CanCast || damage < 1) return false;
            _center = center;
            _center.y = .085f;
            _hostile = hostile;
            _damage = damage;
            _radius = definition.Radius + Mathf.Max(0f, bonusRadius);
            _impactAt = Time.time + definition.WarningSeconds;
            _casting = true;
            _rim.enabled = true;
            _rune.enabled = true;
            Draw();
            if (castClip != null) _audio.PlayOneShot(castClip);
            return true;
        }

        void Update()
        {
            if (_casting)
            {
                if (Time.time >= _impactAt) Impact();
                else Draw();
            }
            if (_recovering && Time.time >= _recoverAt) _recovering = false;
            if (_flash != null && _flash.intensity > 0f)
                _flash.intensity = Mathf.MoveTowards(_flash.intensity, 0f, 18f * Time.deltaTime);
        }

        void Draw()
        {
            float progress = 1f - Mathf.Clamp01((_impactAt - Time.time) /
                definition.WarningSeconds);
            float pulse = 1f + .035f * Mathf.Sin(Time.time * 18f);
            _rim.startColor = _rim.endColor = _hostile ? EnemyRim : PlayerRim;
            _rune.startColor = _rune.endColor = Rune * Mathf.Lerp(.55f, 1f, progress);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                _rim.SetPosition(i, _center + radial * _radius * pulse);
                float runeRadius = i % 6 == 0 ? _radius * .80f : _radius * .66f;
                _rune.SetPosition(i, _center + radial * runeRadius);
            }
        }

        void Impact()
        {
            _casting = false;
            _recovering = true;
            _recoverAt = Time.time + definition.RecoverySeconds;
            _readyAt = Time.time + definition.CooldownSeconds *
                (GetComponent<PlayerCombat>() != null
                    ? 1f - .01f * (GetComponent<WorldSession>()?.SkillLevel(SkillIds.Staff) - 1 ?? 0)
                    : 1f);
            _rim.enabled = false;
            _rune.enabled = false;
            _burst.transform.position = _center;
            _burst.Emit(28);
            _flash.intensity = 1.2f;
            if (impactClip != null) _audio.PlayOneShot(impactClip);

            if (_hostile)
            {
                PlayerVitality player = FindAnyObjectByType<PlayerVitality>();
                if (player != null && HorizontalDistance(player.transform.position, _center) <= _radius)
                    player.TryTakeDamage(_damage); // Ground damage cannot be shield-blocked.
            }
            else
            {
                WorldSession session = GetComponent<WorldSession>();
                foreach (EnemyCombatant enemy in FindObjectsByType<EnemyCombatant>())
                {
                    if (enemy == null || !enemy.IsAlive ||
                        HorizontalDistance(enemy.transform.position, _center) > _radius) continue;
                    int before = enemy.CurrentHealth;
                    enemy.TakeDirectedDamage(_damage, transform.position);
                    session?.RecordStaffHit(before - enemy.CurrentHealth, enemy.SourceLevel);
                }
            }
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public void Cancel()
        {
            _casting = false;
            _recovering = false;
            if (_rim != null) _rim.enabled = false;
            if (_rune != null) _rune.enabled = false;
            if (_flash != null) _flash.intensity = 0f;
        }

        void OnDisable() => Cancel();
    }
}
