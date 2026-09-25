using System;
using System.Linq;
using Topaz.AnimationStudy;
using Topaz.AnimationStudy.Editor;
using Topaz.CombatStudy;
using Topaz.Crypt;
using Topaz.LoopStudy;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Builds the six-space authored crypt without replacing existing home content.</summary>
    public static class CryptSetup
    {
        const string Root = "Assets/Topaz/World/Crypt";
        const string CryptScene = "Assets/Topaz/World/Scenes/Crypt.unity";
        const string HomeScene = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string KayKit = "Assets/ThirdParty/KayKit/";
        const string Audio = "Assets/ThirdParty/JaggedStone/CryptAudio/";

        [MenuItem("Topaz/Build Home Crypt")]
        public static void Configure()
        {
            Folder(Root + "/Definitions");
            Folder(Root + "/Materials");
            Folder(Root + "/Navigation");
            Folder(Root + "/Prefabs");
            ProgressionSetup.Configure();
            Material skeleton = Load<Material>("Assets/Topaz/Presentation/Art/Materials/Skeleton.mat");
            CrossbowBolt boltPrefab = BoltPrefab(skeleton);
            CrossbowAttackDefinition rogueShot = CrossbowAttack("CryptRogueShot", boltPrefab,
                7f, 12f, .8f, 1.1f, 3);
            CrossbowAttackDefinition playerShot = CrossbowAttack("PlayerCrossbowShot", boltPrefab,
                7f, 14f, .28f, 1.2f, 4);
            AnimationClip shootClip = AnimationStudySetup.Clip("Crossbow Shot",
                "Rig_Medium_CombatRanged.fbx", "Ranged_2H_Shoot", false);
            AnimationClip reloadClip = AnimationStudySetup.Clip("Crossbow Reload",
                "Rig_Medium_CombatRanged.fbx", "Ranged_2H_Reload", false);
            WeaponDefinition crossbowWeapon = Asset<WeaponDefinition>(
                Root + "/Definitions/CryptCrossbowWeapon.asset");
            Set(crossbowWeapon, "stableId", "weapon.crypt.crossbow");
            Ref(crossbowWeapon, "crossbowAttack", playerShot);
            Set(crossbowWeapon, "skill", (int)WeaponSkill.Crossbows);
            Set(crossbowWeapon, "twoHanded", true);
            Ref(crossbowWeapon, "heldModel", Load<GameObject>(
                KayKit + "Skeletons/Models/Skeleton_Crossbow.fbx"));
            Ref(crossbowWeapon, "attackClip", shootClip);
            Ref(crossbowWeapon, "reloadClip", reloadClip);
            ItemDefinition crossbowItem = Asset<ItemDefinition>(
                Root + "/Definitions/CryptCrossbow.asset");
            Set(crossbowItem, "stableId", "gear.crypt.crossbow");
            Set(crossbowItem, "displayName", "Skeleton Crossbow");
            Set(crossbowItem, "maxStack", 1);
            Set(crossbowItem, "equipmentSlot", (int)EquipmentSlot.Weapon);
            Set(crossbowItem, "attack", 0);
            Ref(crossbowItem, "weapon", crossbowWeapon);
            Ref(crossbowItem, "journalIcon", Load<Sprite>(
                EquipmentIconSetup.Path(EquipmentSlot.Weapon)));
            GroundSpellDefinition spell = Asset<GroundSpellDefinition>(Root + "/Definitions/CryptCircle.asset");
            Set(spell, "stableId", "spell.crypt-circle");
            Set(spell, "radius", 1.35f);
            Set(spell, "range", 5f);
            Set(spell, "warningSeconds", .9f);
            Set(spell, "recoverySeconds", .8f);
            Set(spell, "cooldownSeconds", 3f);
            WeaponDefinition staffWeapon = Asset<WeaponDefinition>(Root + "/Definitions/CryptStaffWeapon.asset");
            Set(staffWeapon, "stableId", "weapon.crypt.staff");
            Ref(staffWeapon, "groundSpell", spell);
            Set(staffWeapon, "skill", (int)WeaponSkill.Staff);
            Set(staffWeapon, "twoHanded", true);
            Ref(staffWeapon, "heldModel", Load<GameObject>(KayKit + "Skeletons/Models/Skeleton_Staff.fbx"));
            Ref(staffWeapon, "attackClip", Load<AnimationClip>(
                "Assets/Topaz/Characters/Animation/Clips/Combat Axe.anim"));
            ItemDefinition staff = Asset<ItemDefinition>(Root + "/Definitions/CryptStaff.asset");
            Set(staff, "stableId", "gear.crypt.staff");
            Set(staff, "displayName", "Crypt Staff");
            Set(staff, "maxStack", 1);
            Set(staff, "equipmentSlot", (int)EquipmentSlot.Weapon);
            Set(staff, "attack", 1);
            Ref(staff, "weapon", staffWeapon);
            Ref(staff, "journalIcon", Load<Sprite>(
                EquipmentIconSetup.Path(EquipmentSlot.Weapon)));
            Material dungeon = Load<Material>("Assets/Topaz/Presentation/Art/Materials/Dungeon.mat");
            Material forest = Load<Material>("Assets/Topaz/Presentation/Art/Materials/Forest.mat");
            Material halloween = AtlasMaterial("Crypt Halloween",
                KayKit + "Halloween/Textures/halloweenbits_texture.png");
            Material tell = Load<Material>("Assets/Topaz/Gameplay/Combat/Materials/EnemyTell.mat");
            Material rune = Material("Crypt Violet Rune", new Color(.72f, .36f, 1f));
            Material rim = Material("Crypt Warning Rim", Color.white);
            Material stone = Material("Crypt Stone", new Color(.24f, .25f, .29f));
            Material particle = Load<Material>("Assets/Topaz/Presentation/Effects/Resources/TopazEffectsParticles.mat");
            AudioClip cast = Load<AudioClip>(Audio + "magical_1.ogg");
            AudioClip impact = Load<AudioClip>(Audio + "magical_7.ogg");
            AudioClip ambience = Load<AudioClip>(Audio + "dungeon_ambient_1.ogg");
            EnemyDefinition minion = EnemyDefinition("CryptMinion", "enemy.crypt.minion", 5,
                1.65f, 70f, .55f, .8f, 2, false, null);
            EnemyDefinition warrior = EnemyDefinition("CryptWarrior", "enemy.crypt.warrior", 10,
                2.15f, 110f, .8f, 1f, 3, true, null);
            EnemyDefinition mage = EnemyDefinition("CryptMage", "enemy.crypt.mage", 18,
                1.8f, 100f, .65f, .8f, 4, false, spell);
            EnemyDefinition rogueDefinition = EnemyDefinition("CryptRogue",
                "enemy.crypt.rogue", 6, 1.6f, 70f, .8f, 1.1f, 3, false, null);
            Ref(rogueDefinition, "crossbowAttack", rogueShot);

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CryptScene) == null
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                : EditorSceneManager.OpenScene(CryptScene, OpenSceneMode.Single);
            GameObject previous = GameObject.Find("Home Crypt");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            Transform root = new GameObject("Home Crypt").transform;
            root.position = new Vector3(200f, 0f, 0f);
            Transform navigation = Child(root, "Navigation Geometry", Vector3.zero);
            Box(navigation, "Main Floor", new Vector3(0f, -.13f, -2f),
                new Vector3(18f, .26f, 60f), stone, true);
            Box(navigation, "Ossuary Floor", new Vector3(13f, -.13f, -4f),
                new Vector3(10f, .26f, 12f), stone, true);
            Box(navigation, "Shortcut Floor", new Vector3(-11f, -.13f, -10f),
                new Vector3(6f, .26f, 36f), stone, true);
            Wall(navigation, -9f, -30.5f, 3f, true, stone);
            Wall(navigation, -9f, -9.5f, 25f, true, stone);
            Wall(navigation, 9f, -20f, 24f, true, stone);
            Wall(navigation, 9f, 14f, 28f, true, stone);
            Wall(navigation, -14f, -10f, 36f, true, stone);
            Wall(navigation, 18f, -4f, 12f, true, stone);
            CollisionWall(navigation, "Entry Back Wall", new Vector3(0f, 1.2f, -32f),
                new Vector3(18f, 2.4f, .7f), stone, true);
            CollisionWall(navigation, "Shrine End Wall", new Vector3(0f, 1.2f, 28f),
                new Vector3(18f, 2.4f, .7f), stone, true);
            Partition(navigation, -20f, -9f, 2f, stone);
            Partition(navigation, -9f, -2f, 9f, stone);
            Partition(navigation, 2f, -9f, 2f, stone);
            Partition(navigation, 14f, -2f, 9f, stone);
            CollisionWall(navigation, "Ossuary North Wall", new Vector3(13f, .65f, 2f),
                new Vector3(10f, 1.3f, .6f), stone, true);
            CollisionWall(navigation, "Ossuary South Wall", new Vector3(13f, .65f, -10f),
                new Vector3(10f, 1.3f, .6f), stone, true);
            CollisionWall(navigation, "Gallery Bolt Cover", new Vector3(2f, .8f, -17f),
                new Vector3(1.4f, 1.6f, 1.4f), stone, true);
            GameObject barrier = CollisionWall(navigation, "Shortcut Gate Barrier",
                new Vector3(-11f, 1.25f, -19f), new Vector3(5.4f, 2.5f, .6f), stone, true);
            NavMeshSurface surface = navigation.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.agentTypeID = 0;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("Crypt NavMesh failed.");
            string navPath = Root + "/Navigation/CryptNavMesh.asset";
            NavMeshData oldNav = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if (oldNav == null) AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            else EditorUtility.CopySerialized(surface.navMeshData, oldNav);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);

            Transform art = Child(root, "KayKit Crypt Art", Vector3.zero);
            for (int z = -29; z <= 25; z += 4)
                for (int x = -6; x <= 6; x += 4)
                    Model(art, "Dungeon/Models/floor_tile_large.fbx", dungeon,
                        art.TransformPoint(new Vector3(x, .02f, z)), 4f, true);
            foreach (int z in new[] { -20, -17, -14, -11, -8, -5, -2, 1 })
            {
                GameObject wall = Model(art, "Dungeon/Models/wall_half.fbx", dungeon,
                    art.TransformPoint(new Vector3(-9f, 0f, z)), 1.25f);
                wall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            foreach (int z in new[] { -29, -26, -23, -20, -17, -14, -11, 2, 5, 8, 11, 14, 17, 20, 23, 26 })
            {
                GameObject wall = Model(art, "Dungeon/Models/wall.fbx", dungeon,
                    art.TransformPoint(new Vector3(9f, 0f, z)), 2.3f);
                wall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            foreach (int x in new[] { -7, -4, -1 })
            {
                Model(art, "Dungeon/Models/wall_half.fbx", dungeon,
                    art.TransformPoint(new Vector3(x, 0f, -20f)), 1.25f);
                Model(art, "Dungeon/Models/wall_half.fbx", dungeon,
                    art.TransformPoint(new Vector3(x, 0f, 2f)), 1.25f);
            }
            foreach (int x in new[] { 0, 3, 6 })
            {
                Model(art, "Dungeon/Models/wall_half.fbx", dungeon,
                    art.TransformPoint(new Vector3(x, 0f, -9f)), 1.25f);
                Model(art, "Dungeon/Models/wall_half.fbx", dungeon,
                    art.TransformPoint(new Vector3(x, 0f, 14f)), 1.25f);
            }
            Box(art, "Low Stone Gallery Wall", new Vector3(-3.5f, .38f, -20f),
                new Vector3(11f, .76f, .65f), stone, false);
            Box(art, "Low Stone Hall Wall", new Vector3(3.5f, .38f, -9f),
                new Vector3(11f, .76f, .65f), stone, false);
            Box(art, "Low Stone Hearth Wall", new Vector3(-3.5f, .38f, 2f),
                new Vector3(11f, .76f, .65f), stone, false);
            Box(art, "Low Stone Shrine Wall", new Vector3(3.5f, .38f, 14f),
                new Vector3(11f, .76f, .65f), stone, false);
            foreach (int x in new[] { -6, -3, 0, 3, 6 })
                Model(art, "Dungeon/Models/wall.fbx", dungeon,
                    art.TransformPoint(new Vector3(x, 0f, 28f)), 2.3f);
            foreach (int z in new[] { -27, -15, -4, 8, 21 })
            {
                Model(art, "Dungeon/Models/torch_mounted.fbx", dungeon,
                    art.TransformPoint(new Vector3(-8.5f, 0f, z)), 1.8f);
                LightAt(art, new Vector3(-7.8f, 2.1f, z),
                    new Color(1f, .57f, .25f), 8f, .8f);
            }
            Model(art, "Halloween/Models/bone_A.fbx", halloween,
                art.TransformPoint(new Vector3(3f, 0f, -14f)), .65f);
            Model(art, "Halloween/Models/coffin.fbx", halloween,
                art.TransformPoint(new Vector3(-5f, 0f, -13f)), 1.25f);
            Model(art, "Dungeon/Models/pillar_decorated.fbx", dungeon,
                art.TransformPoint(new Vector3(2f, 0f, -17f)), 1.7f);
            Model(art, "Dungeon/Models/barrel_large.fbx", dungeon,
                art.TransformPoint(new Vector3(5f, 0f, -4f)), 1f);
            Model(art, "Dungeon/Models/sword_shield_broken.fbx", dungeon,
                art.TransformPoint(new Vector3(6.5f, 0f, -6f)), 1.35f);
            Model(art, "Dungeon/Models/rubble_large.fbx", dungeon,
                art.TransformPoint(new Vector3(-6.5f, 0f, -5f)), .8f);
            Model(art, "Dungeon/Models/shelves.fbx", dungeon,
                art.TransformPoint(new Vector3(14f, 0f, -7f)), 2f);
            Model(art, "Halloween/Models/coffin_decorated.fbx", halloween,
                art.TransformPoint(new Vector3(16f, 0f, -7f)), 1.2f);
            Model(art, "Halloween/Models/shrine_candles.fbx", halloween,
                art.TransformPoint(new Vector3(0f, 0f, 24f)), 2f);
            Model(art, "Dungeon/Models/pillar_decorated.fbx", dungeon,
                art.TransformPoint(new Vector3(-6.5f, 0f, 20f)), 2.4f);
            Model(art, "Dungeon/Models/pillar_decorated.fbx", dungeon,
                art.TransformPoint(new Vector3(6.5f, 0f, 20f)), 2.4f);
            Light ritual = LightAt(art, new Vector3(0f, 2.4f, 23f),
                new Color(.55f, .25f, 1f), 5f, 1.2f);
            Model(barrier.transform, "Dungeon/Models/wall_gated.fbx", dungeon,
                barrier.transform.position, 2.4f);

            Transform arrival = Child(root, "Arrival", new Vector3(0f, 0f, -28f));
            Transform departure = Child(root, "Return Door", new Vector3(0f, 0f, -30f));
            Model(departure, "Dungeon/Models/stairs_narrow.fbx", dungeon,
                departure.position, 1.1f);
            Transform lever = Child(root, "Shortcut Lever", new Vector3(-7f, 0f, 8f));
            Box(lever, "Lever Base", new Vector3(0f, .55f, 0f),
                new Vector3(.4f, 1.1f, .35f), dungeon, false);
            Box(lever, "Lever Handle", new Vector3(0f, 1.15f, -.2f),
                new Vector3(.14f, .55f, .14f), dungeon, false).transform.localRotation =
                Quaternion.Euler(35f, 0f, 0f);
            Transform cache = Child(root, "Ossuary Material Cache", new Vector3(14f, 0f, -4f));
            Transform cacheArt = Child(cache, "Cache Art", Vector3.zero);
            Model(cacheArt, "Dungeon/Models/chest.fbx", dungeon,
                cache.position, .85f);
            Material resources = Load<Material>("Assets/Topaz/Presentation/Art/Materials/Resources.mat");
            Model(cacheArt, "ResourceBits/Models/Stone_Chunks_Large.fbx", resources,
                cache.position + Vector3.left * 1.1f, .45f);
            Model(cacheArt, "ResourceBits/Models/Iron_Nuggets.fbx", resources,
                cache.position + Vector3.right * 1.1f, .35f);
            GameObject cacheVisual = cacheArt.gameObject;
            Material wood = Load<Material>("Assets/Topaz/Gameplay/WorldLoop/Materials/Woodwork.mat");
            Material rock = Load<Material>("Assets/Topaz/World/Expedition/Materials/Clearing Rock.mat");
            Material ember = Load<Material>("Assets/Topaz/Presentation/Effects/Materials/Lantern Ember.mat");
            Campfire fire = CampfireSetup.Create("Crypt Campfire", root,
                new Vector3(200f, 0f, 7f), new Vector3(200f, 0f, 5.2f),
                Campfire.CryptId, TopazSaveData.CryptRegion, wood, rock, ember, particle);
            SafeZone hearthSafety = fire.gameObject.AddComponent<SafeZone>();
            Set(hearthSafety, "radius", 3.2f);
            EnemyCombatant[] ordinary = {
                Enemy(root, "Gallery Minion A", minion, new Vector3(-1f, 0f, -15f),
                    "Skeleton_Minion", skeleton, tell, null, rim, rune, particle, cast, impact),
                Enemy(root, "Gallery Rogue", rogueDefinition, new Vector3(4f, 0f, -13f),
                    "Skeleton_Rogue", skeleton, tell, null, rim, rune, particle, cast, impact),
                Enemy(root, "Hall Warrior", warrior, new Vector3(-1f, 0f, -4f),
                    "Skeleton_Warrior", skeleton, tell, null, rim, rune, particle, cast, impact),
                Enemy(root, "Hall Minion", minion, new Vector3(4f, 0f, -2f),
                    "Skeleton_Minion", skeleton, tell, null, rim, rune, particle, cast, impact),
                Enemy(root, "Ossuary Warrior", warrior, new Vector3(14f, 0f, -4f),
                    "Skeleton_Warrior", skeleton, tell, null, rim, rune, particle, cast, impact)
            };
            EnemyCombatant boss = Enemy(root, "Crypt Mage", mage,
                new Vector3(0f, 0f, 21f), "Skeleton_Mage", skeleton,
                tell, spell, rim, rune, particle, cast, impact);
            Transform[] firingPositions = {
                Child(root, "Rogue Firing Spot A", new Vector3(4f, 0f, -13f)),
                Child(root, "Rogue Firing Spot B", new Vector3(5f, 0f, -16f))
            };
            var rogueData = new SerializedObject(ordinary[1]);
            SerializedProperty positions = rogueData.FindProperty("rangedPositions");
            positions.arraySize = firingPositions.Length;
            for (int i = 0; i < firingPositions.Length; i++)
                positions.GetArrayElementAtIndex(i).objectReferenceValue = firingPositions[i];
            rogueData.ApplyModifiedPropertiesWithoutUndo();
            var context = root.gameObject.AddComponent<CryptSceneBootstrap>();
            Ref(context, "surface", surface);
            Ref(context, "arrival", arrival);
            Ref(context, "departure", departure);
            Ref(context, "lever", lever);
            Ref(context, "shortcutBarrier", barrier);
            Ref(context, "materialCache", cache);
            Ref(context, "cacheVisual", cacheVisual);
            Ref(context, "campfire", fire);
            Ref(context, "campfireSafeZone", hearthSafety);
            Ref(context, "mage", boss);
            Ref(context, "rogue", ordinary[1]);
            Ref(context, "ritualLight", ritual);
            var contextData = new SerializedObject(context);
            SerializedProperty enemies = contextData.FindProperty("ordinaryEnemies");
            enemies.arraySize = ordinary.Length;
            for (int i = 0; i < ordinary.Length; i++)
                enemies.GetArrayElementAtIndex(i).objectReferenceValue = ordinary[i];
            contextData.ApplyModifiedPropertiesWithoutUndo();
            AudioSource bed = root.gameObject.AddComponent<AudioSource>();
            bed.clip = ambience;
            bed.loop = true;
            bed.playOnAwake = true;
            bed.volume = .14f;
            bed.spatialBlend = 0f;
            EditorSceneManager.SaveScene(scene, CryptScene);

            Scene homeScene = EditorSceneManager.OpenScene(HomeScene, OpenSceneMode.Single);
            staff = Load<ItemDefinition>(Root + "/Definitions/CryptStaff.asset");
            crossbowItem = Load<ItemDefinition>(Root + "/Definitions/CryptCrossbow.asset");
            spell = Load<GroundSpellDefinition>(Root + "/Definitions/CryptCircle.asset");
            GameObject player = GameObject.Find("Player");
            if (player == null) throw new InvalidOperationException("Home player is missing.");
            Transform old = GameObject.Find("Home Crypt Entrance")?.transform;
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform entrance = new GameObject("Home Crypt Entrance").transform;
            entrance.position = new Vector3(-9f, 0f, -5f);
            Model(entrance, "Halloween/Models/arch_gate.fbx", halloween,
                entrance.position, 2.5f);
            for (int i = 0; i < 4; i++)
            {
                Model(entrance, "Halloween/Models/fence.fbx", halloween,
                    entrance.position + new Vector3(-3f, 0f, i * 2f - 4f), 1.2f);
                Model(entrance, "Halloween/Models/fence.fbx", halloween,
                    entrance.position + new Vector3(3f, 0f, i * 2f - 4f), 1.2f);
            }
            Model(entrance, "Halloween/Models/grave_A.fbx", halloween,
                entrance.position + new Vector3(-4f, 0f, 1f), 1.1f);
            Model(entrance, "Halloween/Models/gravestone.fbx", halloween,
                entrance.position + new Vector3(4f, 0f, -2f), 1.2f);
            Model(entrance, "Forest/Models/Tree_Bare_1_A_Color1.fbx", forest,
                entrance.position + new Vector3(-6f, 0f, 1f), 3f);
            Model(entrance, "Forest/Models/Rock_3_B_Color1.fbx", forest,
                entrance.position + new Vector3(6f, 0f, 0f), 1.4f);
            WorldSession session = player.GetComponent<WorldSession>();
            Ref(session, "homeCryptGate", entrance);
            Ref(session, "cryptStaff", staff);
            Ref(session, "cryptCrossbow", crossbowItem);
            Ref(session, "cryptStaffMaterial", skeleton);
            var sessionData = new SerializedObject(session);
            SerializedProperty items = sessionData.FindProperty("equipmentItems");
            UnityEngine.Object[] currentItems = Enumerable.Range(0, items.arraySize)
                .Select(i => items.GetArrayElementAtIndex(i).objectReferenceValue)
                .Where(value => value != null && value != staff && value != crossbowItem)
                .Distinct().ToArray();
            items.arraySize = currentItems.Length + 2;
            for (int i = 0; i < currentItems.Length; i++)
                items.GetArrayElementAtIndex(i).objectReferenceValue = currentItems[i];
            items.GetArrayElementAtIndex(currentItems.Length).objectReferenceValue = staff;
            items.GetArrayElementAtIndex(currentItems.Length + 1).objectReferenceValue = crossbowItem;
            sessionData.ApplyModifiedPropertiesWithoutUndo();
            GroundSpellAbility playerSpell = player.GetComponent<GroundSpellAbility>();
            if (playerSpell == null) playerSpell = player.AddComponent<GroundSpellAbility>();
            SpellRefs(playerSpell, spell, rim, rune, particle, cast, impact);
            if (player.GetComponent<CrossbowPlayerAbility>() == null)
                player.AddComponent<CrossbowPlayerAbility>();
            PlayerAppearance appearance = player.GetComponent<PlayerAppearance>();
            var appearanceData = new SerializedObject(appearance);
            GameObject rogue = appearanceData.FindProperty("rogueVisual").objectReferenceValue as GameObject;
            Transform hand = rogue?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "handslot.r");
            if (hand == null) throw new InvalidOperationException("Player hand socket is missing.");
            Transform held = hand.Find("Held Staff");
            if (held != null) UnityEngine.Object.DestroyImmediate(held.gameObject);
            GameObject heldStaff = Model(hand, "Skeletons/Models/Skeleton_Staff.fbx", skeleton,
                hand.position, .85f);
            heldStaff.name = "Held Staff";
            heldStaff.transform.localPosition = Vector3.zero;
            heldStaff.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            heldStaff.SetActive(false);
            CharacterAnimationDriver driver = rogue.GetComponent<CharacterAnimationDriver>();
            Ref(driver, "staffVisual", heldStaff);
            Transform previousCrossbow = hand.Find("Held Crossbow");
            if (previousCrossbow != null) UnityEngine.Object.DestroyImmediate(previousCrossbow.gameObject);
            GameObject heldCrossbow = Model(hand,
                "Skeletons/Models/Skeleton_Crossbow.fbx", skeleton, hand.position, .9f);
            heldCrossbow.name = "Held Crossbow";
            heldCrossbow.transform.localPosition = Vector3.zero;
            heldCrossbow.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            heldCrossbow.SetActive(false);
            Ref(driver, "crossbowVisual", heldCrossbow);
            AnimatorController controller = Load<AnimatorController>(
                "Assets/Topaz/Characters/Animation/Controllers/Player.controller");
            AnimatorState staffState = controller.layers[0].stateMachine.states
                .Select(s => s.state).FirstOrDefault(s => s.name == "Staff");
            if (staffState == null) staffState = controller.layers[0].stateMachine.AddState("Staff");
            staffState.motion = Load<AnimationClip>(
                "Assets/Topaz/Characters/Animation/Clips/Combat Axe.anim");
            foreach (var pair in new[] {
                (name: "Crossbow", clip: shootClip),
                (name: "CrossbowReload", clip: reloadClip) })
            {
                AnimatorState state = controller.layers[0].stateMachine.states
                    .Select(s => s.state).FirstOrDefault(s => s.name == pair.name);
                if (state == null) state = controller.layers[0].stateMachine.AddState(pair.name);
                state.motion = pair.clip;
            }
            LoopHud hud = GameObject.Find("Loop HUD")?.GetComponent<LoopHud>();
            if (hud == null) throw new InvalidOperationException("Loop HUD is missing.");
            Transform reload = hud.transform.Find("Crossbow Reload");
            if (reload == null)
            {
                GameObject label = new GameObject("Crossbow Reload",
                    typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                label.transform.SetParent(hud.transform, false);
                reload = label.transform;
            }
            RectTransform reloadRect = reload.GetComponent<RectTransform>();
            reloadRect.anchorMin = reloadRect.anchorMax = new Vector2(1f, 0f);
            reloadRect.pivot = new Vector2(1f, 0f);
            reloadRect.anchoredPosition = new Vector2(-36f, 28f);
            reloadRect.sizeDelta = new Vector2(380f, 44f);
            TMPro.TextMeshProUGUI reloadText = reload.GetComponent<TMPro.TextMeshProUGUI>();
            reloadText.text = "";
            reloadText.fontSize = 24;
            reloadText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            reloadText.raycastTarget = false;
            Ref(hud, "crossbowReloadLabel", reloadText);
            JournalPolishSetup.ApplyToCanvas(hud.transform);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(homeScene);
            EditorSceneManager.SaveScene(homeScene);
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (!buildScenes.Any(entry => entry.path == CryptScene))
                EditorBuildSettings.scenes = buildScenes.Append(
                    new EditorBuildSettingsScene(CryptScene, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz/Crypt] Six-space crypt, staff, skill, and home entrance built.");
        }

        static EnemyCombatant Enemy(Transform parent, string name, EnemyDefinition definition,
            Vector3 local, string modelName, Material skeleton, Material tell,
            GroundSpellDefinition spell, Material rim, Material rune, Material particle,
            AudioClip cast, AudioClip impact)
        {
            Transform root = Child(parent, name, local);
            NavMeshAgent agent = root.gameObject.AddComponent<NavMeshAgent>();
            agent.radius = modelName == "Skeleton_Mage" ? .42f : .38f;
            agent.height = 1.7f;
            agent.speed = modelName == "Skeleton_Mage" ? 2.4f : 3f;
            agent.angularSpeed = 540f;
            agent.acceleration = 12f;
            CapsuleCollider body = root.gameObject.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, .85f, 0f);
            body.height = 1.7f;
            body.radius = agent.radius;
            Transform visual = Child(root, "Enemy Visual", Vector3.zero);
            string modelPath = "Skeletons/Characters/" + modelName + ".fbx";
            GameObject model = Model(visual, modelPath, skeleton,
                visual.position, 1.7f);
            if (modelName != "Skeleton_Minion")
            {
                Transform hand = model.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "handslot.r");
                if (hand != null)
                    Model(hand, modelName == "Skeleton_Mage"
                            ? "Skeletons/Models/Skeleton_Staff.fbx"
                            : modelName == "Skeleton_Rogue"
                            ? "Skeletons/Models/Skeleton_Crossbow.fbx"
                            : "Skeletons/Models/Skeleton_Blade.fbx",
                        skeleton, hand.position, .8f);
                if (modelName == "Skeleton_Warrior")
                {
                    Transform left = model.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(t => t.name == "handslot.l");
                    if (left != null)
                        Model(left, "Skeletons/Models/Skeleton_Shield_Large_A.fbx",
                            skeleton, left.position, .85f);
                }
            }
            var lineObject = new GameObject(name + " Tell", typeof(LineRenderer));
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.GetComponent<LineRenderer>();
            line.sharedMaterial = tell;
            line.widthMultiplier = modelName == "Skeleton_Rogue" ? .075f : .11f;
            line.enabled = false;
            if (spell != null)
                SpellRefs(root.gameObject.AddComponent<GroundSpellAbility>(), spell,
                    rim, rune, particle, cast, impact);
            if (modelName == "Skeleton_Rogue")
            {
                AudioSource shotAudio = root.gameObject.AddComponent<AudioSource>();
                shotAudio.playOnAwake = false;
                shotAudio.spatialBlend = .7f;
                shotAudio.volume = .4f;
            }
            EnemyCombatant combatant = root.gameObject.AddComponent<EnemyCombatant>();
            Ref(combatant, "definition", definition);
            Ref(combatant, "visualRoot", visual);
            Ref(combatant, "bodyRenderer", model.GetComponentInChildren<Renderer>(true));
            Ref(combatant, "bodyCollider", body);
            Ref(combatant, "telegraph", line);
            Set(combatant, "keepVisualOnDefeat", true);
            Set(combatant, "respawns", false);
            AnimationStudySetup.BindExpeditionEnemy(model, KayKit + modelPath,
                combatant, agent, modelName != "Skeleton_Minion");
            root.gameObject.SetActive(false);
            return combatant;
        }

        static void SpellRefs(GroundSpellAbility ability, GroundSpellDefinition spell,
            Material rim, Material rune, Material particle, AudioClip cast, AudioClip impact)
        {
            Ref(ability, "definition", spell);
            Ref(ability, "ringMaterial", rim);
            Ref(ability, "runeMaterial", rune);
            Ref(ability, "particleMaterial", particle);
            Ref(ability, "castClip", cast);
            Ref(ability, "impactClip", impact);
        }

        static EnemyDefinition EnemyDefinition(string name, string id, int health,
            float range, float arc, float warning, float recovery, int damage,
            bool shielded, GroundSpellDefinition spell)
        {
            EnemyDefinition result = Asset<EnemyDefinition>(Root + "/Definitions/" + name + ".asset");
            Set(result, "stableId", id);
            Set(result, "sourceLevel", 2);
            Set(result, "health", health);
            Set(result, "detectionRange", 9f);
            Set(result, "travelSpeed", name == "CryptMage" ? 2.4f : 3f);
            Set(result, "strikeRange", range);
            Set(result, "strikeArcDegrees", arc);
            Set(result, "telegraphSeconds", warning);
            Set(result, "recoverySeconds", recovery);
            Set(result, "damage", damage);
            Set(result, "shielded", shielded);
            Ref(result, "groundSpell", spell);
            return result;
        }

        static CrossbowBolt BoltPrefab(Material skeleton)
        {
            const string path = Root + "/Prefabs/CrossbowBolt.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var root = new GameObject("Crossbow Bolt", typeof(CrossbowBolt));
                GameObject arrow = Model(root.transform,
                    "Skeletons/Models/Skeleton_Arrow.fbx", skeleton, Vector3.zero,
                    .4f, true);
                arrow.name = "Arrow Visual";
                arrow.transform.localPosition = Vector3.zero;
                prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                UnityEngine.Object.DestroyImmediate(root);
            }
            CrossbowBolt bolt = prefab.GetComponent<CrossbowBolt>();
            if (bolt == null) throw new InvalidOperationException("Crossbow bolt prefab is invalid.");
            return bolt;
        }

        static CrossbowAttackDefinition CrossbowAttack(string name, CrossbowBolt bolt,
            float range, float speed, float windup, float recovery, int damage)
        {
            CrossbowAttackDefinition attack = Asset<CrossbowAttackDefinition>(
                Root + "/Definitions/" + name + ".asset");
            Set(attack, "range", range);
            Set(attack, "boltSpeed", speed);
            Set(attack, "windupSeconds", windup);
            Set(attack, "recoverySeconds", recovery);
            Set(attack, "damage", damage);
            Ref(attack, "boltPrefab", bolt);
            Ref(attack, "fireClip", Load<AudioClip>(
                "Assets/ThirdParty/Kenney/RpgAudio/metalClick.ogg"));
            Ref(attack, "readyClip", Load<AudioClip>(
                "Assets/ThirdParty/Kenney/RpgAudio/metalLatch.ogg"));
            Ref(attack, "impactClip", Load<AudioClip>(
                "Assets/ThirdParty/Kenney/ImpactSounds/impactGeneric_light_000.ogg"));
            return attack;
        }

        static Transform Child(Transform parent, string name, Vector3 local)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = local;
            return child;
        }

        static void Wall(Transform parent, float x, float z, float length,
            bool near, Material material)
        {
            CollisionWall(parent, "Cutaway Wall", new Vector3(x, near ? .65f : 1.2f, z),
                new Vector3(.7f, near ? 1.3f : 2.4f, length), material, true);
        }

        static void Partition(Transform parent, float z, float from, float to, Material material)
        {
            CollisionWall(parent, "Crypt Partition", new Vector3((from + to) / 2f, 1f, z),
                new Vector3(to - from, 2f, .65f), material, true);
        }

        static GameObject CollisionWall(Transform parent, string name, Vector3 local,
            Vector3 scale, Material material, bool collider)
        {
            GameObject result = Box(parent, name, local, scale, material, collider);
            result.GetComponent<Renderer>().enabled = false;
            return result;
        }

        static GameObject Box(Transform parent, string name, Vector3 local,
            Vector3 scale, Material material, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = local;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        static GameObject Model(Transform parent, string path, Material material,
            Vector3 world, float size, bool longest = false)
        {
            GameObject source = Load<GameObject>(KayKit + path);
            GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
            model.name = System.IO.Path.GetFileNameWithoutExtension(path);
            model.transform.SetParent(parent, false);
            model.transform.position = world;
            model.transform.localRotation = Quaternion.identity;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No renderer: " + path);
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float measure = longest ? Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z)
                : bounds.size.y;
            if (measure > .001f) model.transform.localScale *= size / measure;
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            model.transform.position += Vector3.up * (world.y - bounds.min.y);
            foreach (Collider collision in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collision);
            return model;
        }

        static Light LightAt(Transform parent, Vector3 local, Color color,
            float range, float intensity)
        {
            Light light = Child(parent, "Crypt Light", local).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material asset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (asset == null)
            {
                asset = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static Material AtlasMaterial(string name, string texturePath)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material asset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (asset == null)
            {
                asset = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.SetTexture("_BaseMap", Load<Texture2D>(texturePath));
            asset.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Missing crypt asset: " + path);

        static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string field, string value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string field, int value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string field, float value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string field, bool value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).boolValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
