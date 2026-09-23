using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>Shared authored values for a melee attack; runtime phase lives on the attacker.</summary>
    [CreateAssetMenu(menuName = "Topaz/Combat/Melee Attack")]
    public sealed class MeleeAttackDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "sword.basic";
        [SerializeField, Min(0.01f)] float windupSeconds = 0.18f;
        [SerializeField, Min(0.01f)] float activeSeconds = 0.14f;
        [SerializeField, Min(0.01f)] float recoverySeconds = 0.30f;
        [SerializeField, Min(0.1f)] float range = 2f;
        [SerializeField, Range(1f, 180f)] float arcDegrees = 105f;
        [SerializeField, Min(1)] int damage = 1;

        public string StableId => stableId;
        public float WindupSeconds => windupSeconds;
        public float ActiveSeconds => activeSeconds;
        public float RecoverySeconds => recoverySeconds;
        public float Range => range;
        public float ArcDegrees => arcDegrees;
        public int Damage => damage;
    }
}
