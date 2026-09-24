using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>Static values for the single combat practice enemy.</summary>
    [CreateAssetMenu(menuName = "Topaz/Combat/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "enemy.practice";
        [SerializeField, Min(1)] int health = 3;
        [SerializeField, Min(0.1f)] float detectionRange = 9f;
        [SerializeField, Min(0.1f)] float travelSpeed = 3.1f;
        [SerializeField, Min(0.1f)] float strikeRange = 1.8f;
        [SerializeField, Range(1f, 180f)] float strikeArcDegrees = 80f;
        [SerializeField, Min(0.01f)] float telegraphSeconds = 0.65f;
        [SerializeField, Min(0.01f)] float recoverySeconds = 0.9f;
        [SerializeField, Min(1)] int damage = 1;

        public string StableId => stableId;
        public int Health => health;
        public float DetectionRange => detectionRange;
        public float TravelSpeed => travelSpeed;
        public float StrikeRange => strikeRange;
        public float StrikeArcDegrees => strikeArcDegrees;
        public float TelegraphSeconds => telegraphSeconds;
        public float RecoverySeconds => recoverySeconds;
        public int Damage => damage;
    }
}
