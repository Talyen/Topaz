using System;
using System.Linq;
using Topaz.Characters;
using Topaz.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    /// <summary>Reuses Topaz combat roots with distinct purchased character visuals.</summary>
    public static class SyntyEnemySetup
    {
        const string Root="Assets/Topaz/Presentation/Art/Characters/Enemies/";
        const string SourceEnemy="Assets/Topaz/Presentation/Art/Characters/Wilderness Skeleton 0.prefab";
        const string Goblin="Assets/Synty/SidekickCharacters/Characters/GoblinFighters/";
        const string WarCamp="Assets/Synty/PolygonGoblinWarCamp/Prefabs/Characters/";
        const string Viking="Assets/Synty/PolygonVikingRealm/Prefabs/Characters/";
        static void EnsureReadable(string source)
        {
            foreach(var path in AssetDatabase.GetDependencies(source,true))
            {
                if(!path.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))continue;
                if(AssetImporter.GetAtPath(path) is not ModelImporter importer || importer.isReadable)continue;
                importer.isReadable=true;
                importer.SaveAndReimport();
            }
        }

        static EnemyDefinition Definition(string name,string source,EnemySpecies species,int health,int damage,float speed,float tell,bool shield=false)
        {
            string path=Root+name+".asset";
            var definition=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if(definition==null)
            {
                var template=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(source);
                if(template==null)throw new InvalidOperationException("Missing enemy definition: "+source);
                definition=Object.Instantiate(template);definition.name=name;AssetDatabase.CreateAsset(definition,path);
            }
            var data=new SerializedObject(definition);
            data.FindProperty("stableId").stringValue="enemy.world."+name.ToLowerInvariant().Replace(' ','.');
            data.FindProperty("species").enumValueIndex=(int)species;
            data.FindProperty("health").intValue=health;
            data.FindProperty("damage").intValue=damage;
            data.FindProperty("travelSpeed").floatValue=speed;
            data.FindProperty("telegraphSeconds").floatValue=tell;
            data.FindProperty("shielded").boolValue=shield;
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(definition);
            return definition;
        }

        static EnemyCombatant Actor(string name,string source,EnemyDefinition definition,float height,SkeletonLootRole lootRole)
        {
            EnsureReadable(source);
            var sourceEnemy=AssetDatabase.LoadAssetAtPath<GameObject>(SourceEnemy);
            var sourceVisual=AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if(sourceEnemy==null||sourceVisual==null)throw new InvalidOperationException("Missing enemy source for "+name);
            var root=Object.Instantiate(sourceEnemy);root.name=name;
            var enemy=root.GetComponent<EnemyCombatant>();
            var enemyData=new SerializedObject(enemy);
            var visualRoot=(Transform)enemyData.FindProperty("visualRoot").objectReferenceValue;
            var oldVisual=visualRoot.GetComponentInChildren<CharacterVisual>(true);
            var oldAnimator=oldVisual.Animator;
            var controller=oldAnimator.runtimeAnimatorController;
            if(PrefabUtility.IsPartOfPrefabInstance(oldVisual.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(oldVisual.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            Object.DestroyImmediate(oldVisual.gameObject);

            var model=Object.Instantiate(sourceVisual,visualRoot,false);model.name=name+" Model";
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(renderers.Length==0)throw new InvalidOperationException("Enemy lacks a skinned body: "+name);
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            float scale=height/Mathf.Max(.01f,bounds.size.y);
            model.transform.localScale*=scale;
            model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            var animator=model.GetComponentInChildren<Animator>(true);
            if(animator==null||animator.avatar==null||!animator.avatar.isHuman)
                throw new InvalidOperationException("Enemy lacks a Humanoid avatar: "+name);
            animator.runtimeAnimatorController=controller;
            var visual=model.AddComponent<CharacterVisual>();
            var visualData=new SerializedObject(visual);
            visualData.FindProperty("animator").objectReferenceValue=animator;
            visualData.FindProperty("bodyRenderer").objectReferenceValue=renderers.OrderByDescending(r=>r.bounds.size.sqrMagnitude).First();
            visualData.ApplyModifiedPropertiesWithoutUndo();
            enemyData.Update();
            enemyData.FindProperty("definition").objectReferenceValue=definition;
            enemyData.FindProperty("lootRole").enumValueIndex=(int)lootRole;
            enemyData.FindProperty("normalBodyTint").colorValue=Color.white;
            enemyData.ApplyModifiedPropertiesWithoutUndo();
            var capsule=root.GetComponent<CapsuleCollider>();
            if(capsule!=null){capsule.height=height*.9f;capsule.center=Vector3.up*capsule.height*.5f;capsule.radius=height>2.8f?.7f:.36f;}
            var agent=root.GetComponent<NavMeshAgent>();
            if(agent!=null){agent.height=height*.9f;agent.radius=height>2.8f?.7f:.36f;agent.baseOffset=0;}
            var saved=PrefabUtility.SaveAsPrefabAsset(root,Root+name+".prefab");Object.DestroyImmediate(root);
            if(saved==null)throw new InvalidOperationException("Could not save enemy "+name);
            return saved.GetComponent<EnemyCombatant>();
        }

        [MenuItem("Topaz/Generation/Author Synty Wilderness Enemies")]
        public static void Apply()
        {
            if(!AssetDatabase.IsValidFolder(Root.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/Topaz/Presentation/Art/Characters","Enemies");
            string minion="Assets/Topaz/Gameplay/Combat/Definitions/Weapons/Minion.asset";
            string rogue="Assets/Topaz/Gameplay/Combat/Definitions/Weapons/Rogue.asset";
            string mage="Assets/Topaz/Gameplay/Combat/Definitions/Weapons/Mage.asset";
            string warrior="Assets/Topaz/Gameplay/Combat/Definitions/Weapons/Warrior.asset";
            var scout=Definition("Goblin Scout",minion,EnemySpecies.Goblin,4,1,3.5f,.65f);
            var archer=Definition("Goblin Archer",rogue,EnemySpecies.Goblin,4,1,3.1f,.75f);
            var shaman=Definition("Goblin Shaman",mage,EnemySpecies.Goblin,6,1,2.8f,.9f);
            var raider=Definition("Raider",warrior,EnemySpecies.Raider,7,2,3.2f,.7f);
            var captain=Definition("Raider Captain",warrior,EnemySpecies.Raider,11,2,3,.85f,true);
            var troll=Definition("Troll",warrior,EnemySpecies.Troll,20,3,2.2f,1.35f);
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            preset.goblins=new[]
            {
                Actor("Goblin Fighter 1",Goblin+"GoblinFighter_01/GoblinFighter_01.prefab",scout,1.5f,SkeletonLootRole.Minion),
                Actor("Goblin Fighter 2",Goblin+"GoblinFighter_02/GoblinFighter_02.prefab",scout,1.5f,SkeletonLootRole.Minion),
                Actor("Goblin Archer",WarCamp+"SM_Chr_Archer_Male_01.prefab",archer,1.7f,SkeletonLootRole.Rogue),
                Actor("Goblin Shaman",WarCamp+"SM_Chr_Shaman_01.prefab",shaman,1.7f,SkeletonLootRole.Mage)
            };
            preset.raiders=new[]
            {
                Actor("Raider Warrior",Viking+"SM_Chr_Warrior_Male_01.prefab",raider,1.8f,SkeletonLootRole.Minion),
                Actor("Raider Shieldmaiden",Viking+"SM_Chr_Warrior_Female_01.prefab",raider,1.8f,SkeletonLootRole.Minion),
                Actor("Raider Captain",Viking+"SM_Chr_Leader_Male_01.prefab",captain,1.85f,SkeletonLootRole.Warrior)
            };
            preset.troll=Actor("War Camp Troll",WarCamp+"SM_Chr_Troll_01.prefab",troll,3.2f,SkeletonLootRole.Minion);
            EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();
            Debug.Log("[Topaz/Generation] Authored goblin, raider and troll enemy wrappers.");
        }
    }
}
