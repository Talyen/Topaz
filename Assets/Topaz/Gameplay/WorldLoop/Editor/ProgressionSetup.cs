using System;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Creates progression definitions and adds the Skills page to the current scene.</summary>
    public static class ProgressionSetup
    {
        const string Root = "Assets/Topaz/Gameplay/WorldLoop/Definitions/";
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";

        [MenuItem("Topaz/Apply Skill Progression")]
        public static void Configure()
        {
            SkillDefinition[] skills = {
                Skill(SkillIds.Swords, "Swords", "Sword swings are 1% quicker per level. +1 damage at levels 5 and 10.", .01f, 1f, 1f,
                    Talent("swords.footwork", "Footwork", "Move at 55% speed during a sword windup and strike instead of 45%.", .55f),
                    Talent("swords.flow", "Flow", "A sword hit shortens that swing's recovery by 0.04 seconds.", .04f),
                    Talent("swords.wide-cut", "Wide Cut", "Sword swing arc increases by 10 degrees.", 10f),
                    Talent("swords.dodge-strike", "Dodge Strike", "The first sword swing within one second after dodging deals +1 damage.", 1f, 1f)),
                Skill(SkillIds.Axes, "Axes", "Axe recovery is 1% quicker per level. +1 damage at levels 5 and 10.", .01f, 1f, 1f,
                    Talent("axes.bleed", "Bleed", "Axe hits cause non-stacking bleed: one damage after 1.5 seconds. Bleed can finish enemies.", 1f, 2f, 1.5f),
                    Talent("axes.rage", "Rage", "Taking real enemy damage with the axe equipped grants +20% Axe damage for five seconds; further hits refresh it.", .2f, 5f),
                    Talent("axes.heavy-impact", "Heavy Impact", "Staggerable enemies recover 0.1 seconds later from an axe hit.", .1f),
                    Talent("axes.finishing-blow", "Finishing Blow", "Axe hits deal +1 damage to enemies already at half health or less.", 1f)),
                Skill(SkillIds.Shield, "Shield", "Move one percentage point faster while guarding per level. Block recovery improves at levels 5 and 10.", .01f, .05f, .05f,
                    Talent("shield.quick-raise", "Quick Raise", "Shield raises in 0.10 seconds instead of 0.12 seconds.", .02f),
                    Talent("shield.broad-guard", "Broad Guard", "Block angle increases from 120 to 130 degrees.", 10f),
                    Talent("shield.mobile-guard", "Mobile Guard", "Move five percentage points faster while guarding.", .05f),
                    Talent("shield.hold-line", "Hold Line", "A blocked enemy recovers 0.1 seconds later.", .1f)),
                Skill(SkillIds.Logging, "Logging", "Logging actions are 1.5% quicker per level. Trees yield +1 Wood at levels 5 and 10.", .015f, 1f, 1f,
                    Talent("logging.quick-chop", "Quick Chop", "Logging actions are 10% quicker.", .1f),
                    Talent("logging.deep-bite", "Deep Bite", "Each valid chop makes one extra unit of progress toward felling a tree.", 1f),
                    Talent("logging.clean-fell", "Clean Fell", "A completed tree drops one extra Wood.", 1f),
                    Talent("logging.stewardship", "Stewardship", "A tree you fell regrows 12 World hours sooner, but never in less than eight hours.", 12f)),
                Skill(SkillIds.Mining, "Mining", "Mining actions are 1.5% quicker per level. Gain +1 Stone at level 5 and +1 Iron at level 10.", .015f, 1f, 1f,
                    Talent("mining.quick-strike", "Quick Strike", "Mining actions are 10% quicker.", .1f),
                    Talent("mining.heavy-pick", "Heavy Pick", "Each valid strike makes one extra unit of progress toward finishing a deposit.", 1f),
                    Talent("mining.stone-lode", "Stone Lode", "A completed deposit drops one extra Stone.", 1f),
                    Talent("mining.iron-seeker", "Iron Seeker", "A completed deposit drops one extra Iron.", 1f)),
                Skill(SkillIds.Staff, "Staff", "Staff spell cooldown is 1% shorter per level. +1 spell damage at levels 5 and 10.", .01f, 1f, 1f,
                    Talent("staff.mobile-casting", "Mobile Casting", "Move at 55% speed while channeling instead of 30%.", .55f),
                    Talent("staff.far-sigil", "Far Sigil", "Place the spell one meter farther away.", 1f),
                    Talent("staff.wide-circle", "Wide Circle", "Spell radius increases by 0.25 meters.", .25f),
                    Talent("staff.dodge-focus", "Dodge Focus", "A spell begun within one second after dodging deals +1 damage.", 1f, 1f)),
                Skill(SkillIds.Crossbows, "Crossbows", "Reload 1% quicker per level. +1 bolt damage at levels 5 and 10.", .01f, 1f, 1f,
                    Talent("crossbows.pierce", "Pierce", "A damaging bolt has a 20% chance to pass through and hit one more enemy in line.", .2f),
                    Talent("crossbows.quick-reload", "Quick Reload", "Reload 0.18 seconds sooner.", .18f),
                    Talent("crossbows.quick-aim", "Quick Aim", "Release a bolt 0.07 seconds sooner.", .07f),
                    Talent("crossbows.long-sight", "Long Sight", "Bolts travel one meter farther.", 1f))
            };
            MiningDefinition mining = AssetDatabase.LoadAssetAtPath<MiningDefinition>(
                Root + "StarterMining.asset");
            if (mining == null)
            {
                mining = ScriptableObject.CreateInstance<MiningDefinition>();
                AssetDatabase.CreateAsset(mining, Root + "StarterMining.asset");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(Root + "StarterMining.asset",
                ImportAssetOptions.ForceSynchronousImport);
            mining = AssetDatabase.LoadAssetAtPath<MiningDefinition>(Root + "StarterMining.asset");
            if (mining == null) throw new InvalidOperationException("Starter Mining definition did not import.");
            HarvestDefinition tree = AssetDatabase.LoadAssetAtPath<HarvestDefinition>(Root + "Tree.asset");
            SetInt(tree, "sourceLevel", 1);
            SetInt(tree, "completionExperience", 10);
            foreach (string path in new[] {
                "Assets/Topaz/Gameplay/Combat/Definitions/PracticeEnemy.asset",
                "Assets/Topaz/World/Expedition/Definitions/ExpeditionScout.asset",
                "Assets/Topaz/World/Expedition/Definitions/ExpeditionGuardian.asset" })
                SetInt(AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path), "sourceLevel", 1);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Opening the scene can invalidate freshly imported asset instances.
            mining = AssetDatabase.LoadAssetAtPath<MiningDefinition>(Root + "StarterMining.asset");
            for (int i = 0; i < skills.Length; i++)
            {
                string id = SkillIds.All[i];
                skills[i] = AssetDatabase.LoadAssetAtPath<SkillDefinition>(
                    Root + char.ToUpperInvariant(id[0]) + id.Substring(1) + "Skill.asset");
            }
            if (mining == null || skills.Any(value => value == null))
                throw new InvalidOperationException("Progression definitions did not survive scene load.");
            WorldSession session = UnityEngine.Object.FindAnyObjectByType<WorldSession>();
            LoopHud hud = UnityEngine.Object.FindAnyObjectByType<LoopHud>();
            if (session == null || hud == null) throw new InvalidOperationException("Bootstrap gameplay and Canvas are required.");
            var data = new SerializedObject(session);
            SerializedProperty definitions = data.FindProperty("skillDefinitions");
            definitions.arraySize = skills.Length;
            for (int i = 0; i < skills.Length; i++)
                definitions.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            int configuredRocks = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MiningRock rock in root.GetComponentsInChildren<MiningRock>(true))
                {
                    var rockData = new SerializedObject(rock);
                    rockData.FindProperty("definition").objectReferenceValue = mining;
                    rockData.ApplyModifiedProperties();
                    EditorUtility.SetDirty(rock);
                    configuredRocks++;
                }
            SkillsUiSetup.ApplyToCanvas(hud.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Topaz] Progression definitions and Skills journal applied; " +
                $"{configuredRocks} rocks, mining asset {AssetDatabase.GetAssetPath(mining)}.");
        }

        static TalentDefinition Talent(string id, string label, string description, float amount,
            float duration = 0f, float interval = 0f) => new TalentDefinition {
                id = id, label = label, description = description, amount = amount,
                durationSeconds = duration, intervalSeconds = interval };

        static SkillDefinition Skill(string id, string label, string benefit, float handling,
            float atFive, float atTen, params TalentDefinition[] talents)
        {
            string path = Root + char.ToUpperInvariant(id[0]) + id.Substring(1) + "Skill.asset";
            SkillDefinition asset = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.Configure(id, label, benefit, handling, atFive, atTen, talents);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void SetInt(UnityEngine.Object asset, string property, int value)
        {
            if (asset == null) throw new InvalidOperationException("Progression source asset is missing.");
            var data = new SerializedObject(asset);
            data.FindProperty(property).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
