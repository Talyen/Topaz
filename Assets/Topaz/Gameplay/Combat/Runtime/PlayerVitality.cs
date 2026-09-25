using Topaz.FeelStudy;
using Topaz.LoopStudy;
using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>Player health and protection while recovering at a Campfire.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerVitality : MonoBehaviour
    {
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] SafeZone safeZone;
        [SerializeField, Min(1)] int maximumHealth = 6;

        CharacterController _controller;
        WorldSession _session;
        bool _wasAtHome;
        float _protectedUntil;
        bool _preserveHealthOnHomeArrival;

        public int CurrentHealth { get; private set; }
        public int MaximumHealth => maximumHealth;
        public float LastDamageTime { get; private set; }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _session = GetComponent<WorldSession>();
            CurrentHealth = maximumHealth;
            LastDamageTime = -1000f;
            _wasAtHome = _session?.IsAtHome == true;
        }

        void Update()
        {
            bool atHome = _session?.IsAtHome == true;
            if (atHome && !_wasAtHome && !_preserveHealthOnHomeArrival)
                CurrentHealth = maximumHealth;
            if (atHome) _preserveHealthOnHomeArrival = false;
            _wasAtHome = atHome;
        }

        public bool TryTakeDamage(int amount) => TryTakeDirectedDamage(amount, Vector3.zero, null);

        public bool TryTakeDirectedDamage(int amount, Vector3 attackerPosition,
            EnemyCombatant attacker) => TryTakeDirectedDamageCore(amount, attackerPosition,
                attacker, true);

        public bool TryTakeRangedDamage(int amount, Vector3 attackerPosition,
            EnemyCombatant attacker) => TryTakeDirectedDamageCore(amount, attackerPosition,
                attacker, false);

        bool TryTakeDirectedDamageCore(int amount, Vector3 attackerPosition,
            EnemyCombatant attacker, bool staggerAttackerOnBlock)
        {
            if (amount <= 0 || CurrentHealth == 0 || Time.time < _protectedUntil ||
                (_session?.IsFastTraveling == true || _session?.IsTravelMenuOpen == true ||
                 _session?.IsResting == true) ||
                movement == null || movement.IsInvulnerable ||
                _session?.IsAtHome == true) return false;

            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (attacker != null && combat != null && combat.TryBlock(attackerPosition))
            {
                WorldSession blockingSession = GetComponent<WorldSession>();
                float extraRecovery = blockingSession == null ? 0f :
                    blockingSession.SkillOutputBonus(SkillIds.Shield) +
                    blockingSession.TalentAmount(SkillIds.Shield, "shield.hold-line");
                if (staggerAttackerOnBlock) attacker.StaggerAfterBlock(extraRecovery);
                blockingSession?.RecordShieldBlock(attacker.SourceLevel);
                return false;
            }

            WorldSession session = GetComponent<WorldSession>();
            amount = Mathf.Max(1, amount - (session?.Stats.Armor ?? 0));

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (attacker != null) combat?.OnDamagedByEnemy();
            LastDamageTime = Time.unscaledTime;
            movement.ShowHit();
            if (CurrentHealth == 0)
            {
                if (session == null || !session.RecoverAfterDefeat(this)) ReturnHome();
            }
            return true;
        }

        public void RestoreAfterRecovery()
        {
            GetComponent<PlayerCombat>()?.ClearTemporaryProgression();
            CurrentHealth = maximumHealth;
            _wasAtHome = _session?.IsAtHome == true;
            _protectedUntil = Time.time + 1f;
        }

        public void RestoreHealthAfterRest()
        {
            CurrentHealth = maximumHealth;
            _protectedUntil = Time.time + 1f;
        }

        public void PreserveHealthOnHomeArrival() => _preserveHealthOnHomeArrival = true;

        void ReturnHome()
        {
            GetComponent<PlayerCombat>()?.CancelActiveAttack();
            _controller.enabled = false;
            transform.position = safeZone != null ? safeZone.transform.position : Vector3.zero;
            _controller.enabled = true;
            movement.ResetMotion();
            RestoreAfterRecovery();
        }
    }
}
