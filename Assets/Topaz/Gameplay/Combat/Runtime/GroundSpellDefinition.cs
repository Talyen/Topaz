using UnityEngine;

namespace Topaz.CombatStudy
{
    [CreateAssetMenu(menuName = "Topaz/Combat/Ground Spell")]
    public sealed class GroundSpellDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "spell.crypt-circle";
        [SerializeField, Min(.1f)] float radius = 1.35f;
        [SerializeField, Min(.1f)] float range = 5f;
        [SerializeField, Min(.01f)] float warningSeconds = .9f;
        [SerializeField, Min(.01f)] float recoverySeconds = .8f;
        [SerializeField, Min(.01f)] float cooldownSeconds = 3f;

        public string StableId => stableId;
        public float Radius => radius;
        public float Range => range;
        public float WarningSeconds => warningSeconds;
        public float RecoverySeconds => recoverySeconds;
        public float CooldownSeconds => cooldownSeconds;
    }
}
