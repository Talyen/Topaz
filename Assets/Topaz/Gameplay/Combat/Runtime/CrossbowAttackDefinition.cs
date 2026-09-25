using UnityEngine;

namespace Topaz.CombatStudy
{
    [CreateAssetMenu(menuName = "Topaz/Combat/Crossbow Attack")]
    public sealed class CrossbowAttackDefinition : ScriptableObject
    {
        [SerializeField, Min(.1f)] float range = 7f;
        [SerializeField, Min(.1f)] float boltSpeed = 14f;
        [SerializeField, Min(.01f)] float windupSeconds = .28f;
        [SerializeField, Min(.01f)] float recoverySeconds = 1.2f;
        [SerializeField, Min(1)] int damage = 3;
        [SerializeField] CrossbowBolt boltPrefab;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip readyClip;
        [SerializeField] AudioClip impactClip;

        public float Range => range;
        public float BoltSpeed => boltSpeed;
        public float WindupSeconds => windupSeconds;
        public float RecoverySeconds => recoverySeconds;
        public int Damage => damage;
        public CrossbowBolt BoltPrefab => boltPrefab;
        public AudioClip FireClip => fireClip;
        public AudioClip ReadyClip => readyClip;
        public AudioClip ImpactClip => impactClip;
    }
}
