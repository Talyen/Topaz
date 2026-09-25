using Topaz.CombatStudy;
using UnityEngine;

namespace Topaz.LoopStudy
{
    public enum EquipmentSlot { None, Weapon, Tool, Offhand, Head, Body, Hands, Boots }

    [CreateAssetMenu(menuName = "Topaz/Loop/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "material.wood";
        [SerializeField] string displayName = "Wood";
        [SerializeField, Min(1)] int maxStack = 20;
        [SerializeField] Sprite journalIcon;
        [SerializeField] EquipmentSlot equipmentSlot;
        [SerializeField] WeaponDefinition weapon;
        [SerializeField, Min(0)] int attack;
        [SerializeField, Min(0)] int attackSpeed;
        [SerializeField, Min(0)] int armor;
        [SerializeField, Min(0)] int moveSpeed;
        [SerializeField, Min(0)] int dodge;
        [SerializeField, Min(0)] int logging;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public int MaxStack => maxStack;
        public Sprite JournalIcon => journalIcon;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public WeaponDefinition Weapon => equipmentSlot == EquipmentSlot.Weapon ? weapon : null;
        public EquipmentStats Stats => new EquipmentStats(attack, attackSpeed, armor,
            moveSpeed, dodge, logging);
    }
}
