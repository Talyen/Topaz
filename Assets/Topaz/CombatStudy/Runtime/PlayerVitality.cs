using Topaz.FeelStudy;
using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>Transient practice health. Save and death rules come in a later slice.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerVitality : MonoBehaviour
    {
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] SafeZone safeZone;
        [SerializeField, Min(1)] int maximumHealth = 3;

        CharacterController _controller;
        bool _wasAtHome;

        public int CurrentHealth { get; private set; }
        public int MaximumHealth => maximumHealth;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            CurrentHealth = maximumHealth;
            _wasAtHome = safeZone != null && safeZone.Contains(transform.position);
        }

        void Update()
        {
            bool atHome = safeZone != null && safeZone.Contains(transform.position);
            if (atHome && !_wasAtHome) CurrentHealth = maximumHealth;
            _wasAtHome = atHome;
        }

        public bool TryTakeDamage(int amount)
        {
            if (amount <= 0 || movement == null || movement.IsInvulnerable ||
                (safeZone != null && safeZone.Contains(transform.position))) return false;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            movement.ShowHit();
            if (CurrentHealth == 0) ReturnHome();
            return true;
        }

        void ReturnHome()
        {
            _controller.enabled = false;
            transform.position = safeZone != null ? safeZone.transform.position : Vector3.zero;
            _controller.enabled = true;
            movement.ResetMotion();
            CurrentHealth = maximumHealth;
            _wasAtHome = true;
        }
    }
}
