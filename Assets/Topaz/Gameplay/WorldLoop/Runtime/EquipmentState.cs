using System;
using System.Collections.Generic;

namespace Topaz.LoopStudy
{
    public readonly struct EquipmentStats
    {
        public readonly int Attack;
        public readonly int AttackSpeed;
        public readonly int Armor;
        public readonly int MoveSpeed;
        public readonly int Dodge;
        public readonly int Logging;

        public EquipmentStats(int attack, int attackSpeed, int armor, int moveSpeed,
            int dodge, int logging)
        {
            Attack = attack;
            AttackSpeed = attackSpeed;
            Armor = armor;
            MoveSpeed = moveSpeed;
            Dodge = dodge;
            Logging = logging;
        }

        public static EquipmentStats operator +(EquipmentStats a, EquipmentStats b) =>
            new EquipmentStats(a.Attack + b.Attack, a.AttackSpeed + b.AttackSpeed,
                a.Armor + b.Armor, a.MoveSpeed + b.MoveSpeed, a.Dodge + b.Dodge,
                a.Logging + b.Logging);

        public static EquipmentStats operator -(EquipmentStats a, EquipmentStats b) =>
            new EquipmentStats(a.Attack - b.Attack, a.AttackSpeed - b.AttackSpeed,
                a.Armor - b.Armor, a.MoveSpeed - b.MoveSpeed, a.Dodge - b.Dodge,
                a.Logging - b.Logging);
    }

    [Serializable]
    public sealed class EquipmentState
    {
        public const string Sword = "gear.sword.starter";
        public const string TwoHandedAxe = "gear.axe.twohanded.starter";
        public const string Axe = "gear.axe.starter";
        public const string Shield = "gear.shield.starter";
        public const string Helm = "gear.helm.starter";
        public const string Body = "gear.body.starter";
        public const string Gloves = "gear.gloves.starter";
        public const string Boots = "gear.boots.starter";
        public const string SwiftGloves = "gear.gloves.swift";
        public const string AgileBody = "gear.body.agile";
        public const string AgileBoots = "gear.boots.agile";

        public string weaponId = Sword;
        public string toolId = Axe;
        public string offhandId = Shield;
        public string headId = Helm;
        public string bodyId = Body;
        public string handsId = Gloves;
        public string bootsId = Boots;

        public static readonly EquipmentSlot[] Slots = {
            EquipmentSlot.Weapon, EquipmentSlot.Tool, EquipmentSlot.Offhand,
            EquipmentSlot.Head, EquipmentSlot.Body, EquipmentSlot.Hands, EquipmentSlot.Boots
        };

        public string Get(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => weaponId,
            EquipmentSlot.Tool => toolId,
            EquipmentSlot.Offhand => offhandId,
            EquipmentSlot.Head => headId,
            EquipmentSlot.Body => bodyId,
            EquipmentSlot.Hands => handsId,
            EquipmentSlot.Boots => bootsId,
            _ => null
        };

        public void Set(EquipmentSlot slot, string itemId)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: weaponId = itemId; break;
                case EquipmentSlot.Tool: toolId = itemId; break;
                case EquipmentSlot.Offhand: offhandId = itemId; break;
                case EquipmentSlot.Head: headId = itemId; break;
                case EquipmentSlot.Body: bodyId = itemId; break;
                case EquipmentSlot.Hands: handsId = itemId; break;
                case EquipmentSlot.Boots: bootsId = itemId; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public EquipmentStats Total(Func<string, ItemDefinition> resolve)
        {
            var result = new EquipmentStats(3, 0, 0, 0, 0, 0);
            foreach (EquipmentSlot slot in Slots)
            {
                ItemDefinition item = resolve(Get(slot));
                if (item != null && item.EquipmentSlot == slot)
                    result += EffectiveStats(item);
            }
            return result;
        }

        public static EquipmentStats EffectiveStats(ItemDefinition item)
        {
            if (item == null) return default;
            return item.EquipmentSlot == EquipmentSlot.Tool
                ? new EquipmentStats(0, 0, 0, 0, 0, item.Stats.Logging)
                : item.Stats;
        }
    }
}
