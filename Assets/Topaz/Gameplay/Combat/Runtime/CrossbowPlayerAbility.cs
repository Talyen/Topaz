using Topaz.FeelStudy;
using Topaz.LoopStudy;
using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>The player's aimed release and automatic crossbow reload.</summary>
    public sealed class CrossbowPlayerAbility : MonoBehaviour
    {
        FeelStudyPlayer _movement;
        WorldSession _session;
        AudioSource _audio;
        CrossbowAttackDefinition _attack;
        bool _staminaEnhanced;
        float _releaseAt;
        float _reloadAt;
        float _reloadStarted;
        bool _aiming;
        bool _readyCuePending;

        public bool IsAiming => _aiming;
        public bool IsReloading => !_aiming && Time.time < _reloadAt;
        public bool IsBusy => _aiming || IsReloading;
        public float AimSeconds => _attack == null ? 0f : _attack.WindupSeconds -
            (_session?.TalentAmount(SkillIds.Crossbows, "crossbows.quick-aim") ?? 0f);
        public float ReloadSeconds => _attack == null ? 0f : ReloadDuration();
        public float ReloadProgress => !IsReloading ? 1f :
            Mathf.Clamp01((Time.time - _reloadStarted) / Mathf.Max(.01f, _reloadAt - _reloadStarted));

        void Awake()
        {
            _movement = GetComponent<FeelStudyPlayer>();
            _session = GetComponent<WorldSession>();
            _audio = GetComponent<AudioSource>();
        }

        public bool TryAim(CrossbowAttackDefinition attack)
        {
            if (attack == null || attack.BoltPrefab == null || IsBusy ||
                _session == null || _movement == null) return false;
            _attack = attack;
            _aiming = true;
            _releaseAt = Time.time + Mathf.Max(.05f, AimSeconds);
            return true;
        }

        void Update()
        {
            if (!_aiming)
            {
                if (_readyCuePending && Time.time >= _reloadAt)
                {
                    _readyCuePending = false;
                    if (_attack?.ReadyClip != null) _audio?.PlayOneShot(_attack.ReadyClip);
                }
                return;
            }
            if (Time.time < _releaseAt) return;
            _aiming = false;
            if (_session.CurrentWeapon?.CrossbowAttack != _attack ||
                _session.SuppressAttack) return;
            Vector3 direction = _movement.AimDirection;
            if (_movement.UsingStickAim) direction = AssistedDirection(direction);
            Vector3 origin = transform.position + Vector3.up + direction * .5f;
            int damage = Mathf.Max(1, _session.Stats.Attack) +
                Mathf.RoundToInt(_session.SkillOutputBonus(SkillIds.Crossbows));
            CrossbowBolt bolt = Instantiate(_attack.BoltPrefab);
            bolt.Launch(_attack, origin, direction, damage, playerOwner: _session,
                pierceEnabled: _session.HasTalent(SkillIds.Crossbows, "crossbows.pierce"),
                bonusRange: _session.TalentAmount(SkillIds.Crossbows,
                    "crossbows.long-sight"));
            if (_attack.FireClip != null) _audio?.PlayOneShot(_attack.FireClip);
            _staminaEnhanced = _session.TryExert(25f);
            _reloadStarted = Time.time;
            _reloadAt = Time.time + ReloadDuration();
            _readyCuePending = true;
        }

        float ReloadDuration() => Mathf.Max(.2f,
            _attack.RecoverySeconds *
            (1f - (_session.SkillLevel(SkillIds.Crossbows) - 1) *
                _session.SkillHandling(SkillIds.Crossbows)) -
            _session.TalentAmount(SkillIds.Crossbows, "crossbows.quick-reload")) *
            (_staminaEnhanced ? SurvivalRules.RecoveryMultiplier : 1f);

        Vector3 AssistedDirection(Vector3 raw)
        {
            EnemyCombatant nearest = null;
            float bestAngle = 8f;
            foreach (EnemyCombatant enemy in FindObjectsByType<EnemyCombatant>(
                         FindObjectsInactive.Exclude))
            {
                if (!enemy.IsAlive) continue;
                Vector3 toEnemy = Vector3.ProjectOnPlane(enemy.transform.position -
                    transform.position, Vector3.up);
                float range = _attack.Range +
                    _session.TalentAmount(SkillIds.Crossbows, "crossbows.long-sight");
                if (toEnemy.sqrMagnitude > range * range) continue;
                float angle = Vector3.Angle(raw, toEnemy);
                if (angle >= bestAngle) continue;
                Vector3 origin = transform.position + Vector3.up;
                Vector3 target = enemy.transform.position + Vector3.up;
                if (Physics.Linecast(origin, target, out RaycastHit hit,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<EnemyCombatant>() != enemy) continue;
                nearest = enemy;
                bestAngle = angle;
            }
            return nearest == null ? raw :
                Vector3.ProjectOnPlane(nearest.transform.position - transform.position,
                    Vector3.up).normalized;
        }

        public void OnDodgeStarted()
        {
            if (!IsReloading) return;
            _reloadStarted = Time.time + _movement.DodgeSeconds;
            _reloadAt = _reloadStarted + ReloadDuration();
        }

        public void Cancel()
        {
            _aiming = false;
            _reloadAt = 0f;
            _readyCuePending = false;
            _staminaEnhanced = false;
        }
    }
}
