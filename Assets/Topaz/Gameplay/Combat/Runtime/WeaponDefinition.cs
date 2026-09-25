using UnityEngine;

namespace Topaz.CombatStudy
{
    public enum WeaponSkill { Swords, Axes, Staff, Crossbows }

    /// <summary>Shared weapon rules and presentation references; equipment stores only the item ID.</summary>
    [CreateAssetMenu(menuName = "Topaz/Combat/Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] string stableId;
        [SerializeField] MeleeAttackDefinition attack;
        [SerializeField] GroundSpellDefinition groundSpell;
        [SerializeField] CrossbowAttackDefinition crossbowAttack;
        [SerializeField] WeaponSkill skill;
        [SerializeField] bool twoHanded;
        [SerializeField] AnimationClip attackClip;
        [SerializeField] AnimationClip reloadClip;
        [SerializeField] GameObject heldModel;
        [SerializeField] AudioClip swingClip;
        [SerializeField] AudioClip impactClip;
        [SerializeField, Min(0f)] float staggerSeconds;

        public string StableId => stableId;
        public MeleeAttackDefinition Attack => attack;
        public GroundSpellDefinition GroundSpell => groundSpell;
        public CrossbowAttackDefinition CrossbowAttack => crossbowAttack;
        public WeaponSkill Skill => skill;
        public bool TwoHanded => twoHanded;
        public AnimationClip AttackClip => attackClip;
        public AnimationClip ReloadClip => reloadClip;
        public GameObject HeldModel => heldModel;
        public AudioClip SwingClip => swingClip;
        public AudioClip ImpactClip => impactClip;
        public float StaggerSeconds => staggerSeconds;
    }
}
