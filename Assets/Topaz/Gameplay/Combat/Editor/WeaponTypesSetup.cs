using System;
using System.Linq;
using Topaz.AnimationStudy.Editor;
using Topaz.LoopStudy;
using Topaz.Editor;
using UnityEditor;
using UnityEngine;

namespace Topaz.CombatStudy.Editor
{
    /// <summary>Idempotently authors the sword and combat axe profiles.</summary>
    public static class WeaponTypesSetup
    {
        const string Definitions = "Assets/Topaz/Gameplay/Combat/Definitions";
        const string Clips = "Assets/Topaz/Characters/Animation/Clips";

        [MenuItem("Topaz/Apply Weapon Types")]
        public static void Apply()
        {
            AnimationStudySetup.Configure();
            EquipmentSetup.Apply();
        }

        public static void Configure(ItemDefinition[] items)
        {
            MeleeAttackDefinition axeAttack = Asset<MeleeAttackDefinition>(
                Definitions + "/TwoHandedAxeAttack.asset");
            var attackData = new SerializedObject(axeAttack);
            attackData.FindProperty("stableId").stringValue = "axe.twohanded.basic";
            attackData.FindProperty("windupSeconds").floatValue = 0.32f;
            attackData.FindProperty("activeSeconds").floatValue = 0.18f;
            attackData.FindProperty("recoverySeconds").floatValue = 0.44f;
            attackData.FindProperty("range").floatValue = 2.1f;
            attackData.FindProperty("arcDegrees").floatValue = 75f;
            attackData.FindProperty("damage").intValue = 6;
            attackData.ApplyModifiedPropertiesWithoutUndo();

            WeaponDefinition sword = Weapon("SwordWeapon", "weapon.sword.onehanded",
                AssetDatabase.LoadAssetAtPath<MeleeAttackDefinition>(Definitions + "/PracticeSword.asset"),
                WeaponSkill.Swords, false, "Sword", "FantasyWeapons/Models/sword_A.fbx",
                "RpgAudio/knifeSlice.ogg", "ImpactSounds/impactGeneric_light_000.ogg", 0f);
            WeaponDefinition axe = Weapon("TwoHandedAxeWeapon", "weapon.axe.twohanded",
                axeAttack, WeaponSkill.Axes, true, "Combat Axe",
                "Adventurers/Models/axe_2handed.fbx", "RpgAudio/chop.ogg",
                "ImpactSounds/impactPunch_heavy_000.ogg", 0.3f);
            Assign(items, EquipmentState.Sword, sword);
            Assign(items, EquipmentState.TwoHandedAxe, axe);

            EnemyDefinition guardian = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                "Assets/Topaz/World/Expedition/Definitions/ExpeditionGuardian.asset");
            if (guardian == null) throw new InvalidOperationException("Expedition Guardian is missing.");
            var guardianData = new SerializedObject(guardian);
            guardianData.FindProperty("staggerImmune").boolValue = true;
            guardianData.ApplyModifiedPropertiesWithoutUndo();
        }

        static WeaponDefinition Weapon(string assetName, string id, MeleeAttackDefinition attack,
            WeaponSkill skill, bool twoHanded, string clipName, string modelPath,
            string swingPath, string impactPath, float stagger)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                Clips + "/" + clipName + ".anim");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/KayKit/" + modelPath);
            AudioClip swing = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/ThirdParty/Kenney/" + swingPath);
            AudioClip impact = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/ThirdParty/Kenney/" + impactPath);
            if (attack == null || clip == null || model == null || swing == null || impact == null)
                throw new InvalidOperationException("Weapon asset is missing: " + id);
            WeaponDefinition weapon = Asset<WeaponDefinition>(Definitions + "/" + assetName + ".asset");
            var data = new SerializedObject(weapon);
            data.FindProperty("stableId").stringValue = id;
            data.FindProperty("attack").objectReferenceValue = attack;
            data.FindProperty("skill").enumValueIndex = (int)skill;
            data.FindProperty("twoHanded").boolValue = twoHanded;
            data.FindProperty("attackClip").objectReferenceValue = clip;
            data.FindProperty("heldModel").objectReferenceValue = model;
            data.FindProperty("swingClip").objectReferenceValue = swing;
            data.FindProperty("impactClip").objectReferenceValue = impact;
            data.FindProperty("staggerSeconds").floatValue = stagger;
            data.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        static void Assign(ItemDefinition[] items, string id, WeaponDefinition weapon)
        {
            ItemDefinition item = items.FirstOrDefault(candidate => candidate.StableId == id);
            if (item == null) throw new InvalidOperationException("Weapon item is missing: " + id);
            var data = new SerializedObject(item);
            data.FindProperty("weapon").objectReferenceValue = weapon;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
