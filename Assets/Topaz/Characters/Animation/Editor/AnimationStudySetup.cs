using System;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.AnimationStudy.Editor
{
    /// <summary>Connects CC0 KayKit clips to the authored Rogue and Skeleton practice scene.</summary>
    public static class AnimationStudySetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string Root = "Assets/Topaz/Characters/Animation";
        const string ClipPath = Root + "/Clips";
        const string ControllerPath = Root + "/Controllers";
        const string Animations = "Assets/ThirdParty/KayKit/CharacterAnimations/Animations/";
        const string RoguePath = "Assets/ThirdParty/KayKit/Adventurers/Characters/Rogue.fbx";
        const string SkeletonPath = "Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Minion.fbx";


        [MenuItem("Topaz/Apply Character Animation Study")]
        public static void Configure()
        {
            EnsureFolder(ClipPath);
            EnsureFolder(ControllerPath);
            Avatar rogueAvatar = EnsureGenericAvatar(RoguePath);
            Avatar skeletonAvatar = EnsureGenericAvatar(SkeletonPath);
            AnimationClip idle = Clip("Idle", "Rig_Medium_General.fbx", "Idle_A", true);
            AnimationClip run = Clip("Run", "Rig_Medium_MovementBasic.fbx", "Running_A", true);
            AnimationClip strafeLeft = Clip("Strafe Left", "Rig_Medium_MovementAdvanced.fbx", "Running_Strafe_Left", true);
            AnimationClip strafeRight = Clip("Strafe Right", "Rig_Medium_MovementAdvanced.fbx", "Running_Strafe_Right", true);
            AnimationClip backpedal = Clip("Backpedal", "Rig_Medium_MovementAdvanced.fbx", "Walking_Backwards", true);
            AnimationClip dodgeForward = Clip("Dodge", "Rig_Medium_MovementAdvanced.fbx", "Dodge_Forward", false);
            AnimationClip dodgeBackward = Clip("Dodge Backward", "Rig_Medium_MovementAdvanced.fbx", "Dodge_Backward", false);
            AnimationClip dodgeLeft = Clip("Dodge Left", "Rig_Medium_MovementAdvanced.fbx", "Dodge_Left", false);
            AnimationClip dodgeRight = Clip("Dodge Right", "Rig_Medium_MovementAdvanced.fbx", "Dodge_Right", false);
            AnimationClip jump = Clip("Jump", "Rig_Medium_MovementBasic.fbx", "Jump_Full_Short", false);
            AnimationClip sword = Clip("Sword", "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Slice_Horizontal", false);
            AnimationClip axe = Clip("Axe", "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Chop", false);
            AnimationClip combatAxe = Clip("Combat Axe", "Rig_Medium_CombatMelee.fbx",
                "Melee_2H_Attack_Chop", false);
            AnimationClip hit = Clip("Hit", "Rig_Medium_General.fbx", "Hit_A", false);
            AnimationClip skeletonIdle = Clip("Skeleton Idle", "Rig_Medium_Special.fbx", "Skeletons_Idle", true);
            AnimationClip skeletonWalk = Clip("Skeleton Walk", "Rig_Medium_Special.fbx", "Skeletons_Walking", true);
            AnimationClip skeletonAttack = Clip("Skeleton Attack", "Rig_Medium_CombatMelee.fbx", "Melee_Unarmed_Attack_Punch_A", false);
            AnimationClip skeletonDeath = Clip("Skeleton Death", "Rig_Medium_Special.fbx", "Skeletons_Death", false);

            AnimatorController playerController = Controller("Player", idle, run,
                ("DodgeForward", dodgeForward), ("DodgeBackward", dodgeBackward),
                ("DodgeLeft", dodgeLeft), ("DodgeRight", dodgeRight),
                ("Jump", jump),
                ("Sword", sword), ("Axe", axe), ("CombatAxe", combatAxe),
                ("Hit", hit));
            ConfigurePlayerDirectionalBlend(playerController, idle, run, strafeLeft, strafeRight, backpedal);
            AnimatorController enemyController = Controller("Skeleton", skeletonIdle, skeletonWalk,
                ("Attack", skeletonAttack), ("Hit", hit), ("Death", skeletonDeath));

            var scene = EditorSceneManager.OpenScene(ScenePath);
            GameObject player = GameObject.Find("Player");
            EnemyCombatant enemyComponent = UnityEngine.Object.FindFirstObjectByType<EnemyCombatant>(FindObjectsInactive.Include);
            GameObject enemy = enemyComponent != null ? enemyComponent.gameObject : null;
            FeelStudyPlayer mover = player != null ? player.GetComponent<FeelStudyPlayer>() : null;
            Transform facing = player != null ? player.transform.Find("Facing Visual") : null;
            Transform enemyVisual = enemy != null ? enemy.transform.Find("Enemy Visual") : null;
            Transform rogue = ModelWithRenderer(facing, mover, "KayKit Rogue");
            Transform skeleton = ModelWithRenderer(enemyVisual, enemyComponent, "KayKit Skeleton");
            if (rogue == null || skeleton == null)
                throw new InvalidOperationException("Apply the KayKit art study before character animation.");

            RemoveDuplicateModels(facing, rogue, "KayKit Rogue");
            RemoveDuplicateModels(enemyVisual, skeleton, "KayKit Skeleton");

            Transform hand = rogue.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "handslot.r");
            if (hand == null) throw new InvalidOperationException("Rogue right-hand socket is missing.");
            GameObject swordVisual = AttachWeapon(hand, "Held Sword",
                "Assets/ThirdParty/KayKit/FantasyWeapons/Models/sword_A.fbx",
                "Assets/Topaz/Presentation/Art/Materials/Weapons.mat", 0.82f);
            GameObject axeVisual = AttachWeapon(hand, "Held Axe",
                "Assets/ThirdParty/KayKit/RPGTools/Models/axe.fbx",
                "Assets/Topaz/Presentation/Art/Materials/Tools.mat", 0.75f);
            GameObject combatAxeVisual = AttachWeapon(hand, "Held Combat Axe",
                "Assets/ThirdParty/KayKit/Adventurers/Models/axe_2handed.fbx",
                "Assets/Topaz/Presentation/Art/Materials/Weapons.mat", 1.25f);
            HidePivotRenderers(facing.Find("Sword Pivot"));
            HidePivotRenderers(facing.Find("Axe Pivot"));

            Bind(rogue.gameObject, playerController, rogueAvatar, player.GetComponent<FeelStudyPlayer>(),
                player.GetComponent<PlayerCombat>(), null, null,
                new[] { dodgeForward, dodgeBackward, dodgeLeft, dodgeRight },
                jump, sword, axe, hit, null,
                swordVisual, axeVisual, combatAxeVisual);
            Bind(skeleton.gameObject, enemyController, skeletonAvatar, null, null,
                enemyComponent, enemy.GetComponent<NavMeshAgent>(),
                null, null, null, null, hit, skeletonAttack, null, null, null);
            SetReference(enemyComponent, "keepVisualOnDefeat", true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] KayKit character animation study is ready.");
        }

        public static void BindExpeditionEnemy(GameObject model, string modelPath,
            EnemyCombatant enemy, NavMeshAgent agent, bool guardian)
        {
            EnsureFolder(ClipPath);
            EnsureFolder(ControllerPath);
            Avatar avatar = EnsureGenericAvatar(modelPath);
            AnimationClip idle = Clip("Skeleton Idle", "Rig_Medium_Special.fbx", "Skeletons_Idle", true);
            AnimationClip walk = Clip("Skeleton Walk", "Rig_Medium_Special.fbx", "Skeletons_Walking", true);
            AnimationClip hit = Clip("Hit", "Rig_Medium_General.fbx", "Hit_A", false);
            AnimationClip death = Clip("Skeleton Death", "Rig_Medium_Special.fbx", "Skeletons_Death", false);
            bool crossbow = modelPath.EndsWith("Skeleton_Rogue.fbx", StringComparison.Ordinal);
            AnimationClip attack = crossbow
                ? Clip("Skeleton Crossbow Shot", "Rig_Medium_CombatRanged.fbx",
                    "Ranged_2H_Shoot", false) : guardian
                ? Clip("Guardian Swing", "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Slice_Horizontal", false)
                : Clip("Skeleton Attack", "Rig_Medium_CombatMelee.fbx", "Melee_Unarmed_Attack_Punch_A", false);
            AnimatorController controller = Controller(crossbow ? "Skeleton Crossbow" :
                guardian ? "Guardian" : "Skeleton",
                idle, walk, ("Attack", attack), ("Hit", hit), ("Death", death));
            Bind(model, controller, avatar, null, null, enemy, agent,
                null, null, null, null, hit, attack, null, null, null);
        }

        static GameObject AttachWeapon(Transform hand, string name, string modelPath,
            string materialPath, float height)
        {
            Transform existing = hand.Find(name);
            if (existing != null) return existing.gameObject;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (prefab == null || material == null)
                throw new InvalidOperationException("KayKit held weapon or material is missing: " + modelPath);
            GameObject weapon = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (weapon == null) throw new InvalidOperationException("Could not instantiate held weapon: " + modelPath);
            weapon.name = name;
            weapon.transform.SetParent(hand, false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = Vector3.one;
            Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (longest > 0.001f)
                weapon.transform.localScale *= height / longest;
            weapon.SetActive(false);
            return weapon;
        }

        static void HidePivotRenderers(Transform pivot)
        {
            if (pivot == null) return;
            foreach (Renderer renderer in pivot.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        internal static Avatar EnsureGenericAvatar(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("KayKit model importer missing: " + path);
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                importer.motionNodeName != "Rig_Medium/root")
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.motionNodeName = "Rig_Medium/root";
                importer.SaveAndReimport();
            }
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid)
                throw new InvalidOperationException("Could not create a valid Generic Avatar: " + path);
            return avatar;
        }

        static Transform ModelWithRenderer(Transform parent, UnityEngine.Object owner, string name)
        {
            if (parent == null || owner == null) return null;
            var property = new SerializedObject(owner).FindProperty("bodyRenderer");
            Renderer renderer = property?.objectReferenceValue as Renderer;
            Transform model = renderer != null ? renderer.transform : null;
            while (model != null && model.parent != parent) model = model.parent;
            if (model != null && model.name == name) return model;
            return parent.Cast<Transform>().LastOrDefault(t => t.name == name);
        }

        static void RemoveDuplicateModels(Transform parent, Transform selected, string name)
        {
            foreach (Transform child in parent.Cast<Transform>().Where(t => t.name == name && t != selected).ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        public static AnimationClip Clip(string name, string file, string sourceName, bool loop)
        {
            string path = $"{ClipPath}/{name}.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) return existing;
            string sourcePath = Animations + file;
            AnimationClip source = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                .OfType<AnimationClip>().FirstOrDefault(c => c.name == sourceName);
            if (source == null) throw new InvalidOperationException($"KayKit clip missing: {sourcePath}/{sourceName}");
            AnimationClip copy = UnityEngine.Object.Instantiate(source);
            copy.name = name;
            copy.hideFlags = HideFlags.None;
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        static AnimatorController Controller(string name, AnimationClip idle, AnimationClip travel,
            params (string state, AnimationClip clip)[] actions)
        {
            string path = $"{ControllerPath}/{name}.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            if (!controller.parameters.Any(p => p.name == "Move"))
                controller.AddParameter("Move", AnimatorControllerParameterType.Float);
            var stateMachine = controller.layers[0].stateMachine;
            BlendTree tree = AssetDatabase.LoadAllAssetsAtPath(path).OfType<BlendTree>()
                .FirstOrDefault(t => t.name == "Locomotion Blend");
            if (tree == null)
            {
                tree = new BlendTree
                {
                    name = "Locomotion Blend",
                    blendParameter = "Move",
                    useAutomaticThresholds = false
                };
                AssetDatabase.AddObjectToAsset(tree, controller);
                tree.AddChild(idle, 0f);
                tree.AddChild(travel, 1f);
            }
            var children = tree.children;
            children[0].motion = idle;
            children[0].threshold = 0f;
            children[1].motion = travel;
            children[1].threshold = 1f;
            tree.children = children;
            EditorUtility.SetDirty(tree);
            AnimatorState locomotion = State(stateMachine, "Locomotion", tree);
            stateMachine.defaultState = locomotion;
            foreach (var action in actions) State(stateMachine, action.state, action.clip);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void ConfigurePlayerDirectionalBlend(AnimatorController controller,
            AnimationClip idle, AnimationClip forward, AnimationClip left,
            AnimationClip right, AnimationClip backward)
        {
            if (!controller.parameters.Any(p => p.name == "MoveX"))
                controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            if (!controller.parameters.Any(p => p.name == "MoveY"))
                controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            var tree = controller.layers[0].stateMachine.defaultState.motion as BlendTree;
            if (tree == null) throw new InvalidOperationException("Player locomotion blend tree is missing.");
            tree.blendType = BlendTreeType.SimpleDirectional2D;
            tree.blendParameter = "MoveX";
            tree.blendParameterY = "MoveY";
            Motion[] motions = { idle, forward, left, right, backward };
            Vector2[] positions =
            {
                Vector2.zero, Vector2.up, Vector2.left, Vector2.right, Vector2.down
            };
            while (tree.children.Length < motions.Length)
            {
                int index = tree.children.Length;
                tree.AddChild(motions[index], positions[index]);
            }
            var children = tree.children;
            for (int i = 0; i < motions.Length; i++)
            {
                children[i].motion = motions[i];
                children[i].position = positions[i];
                children[i].timeScale = i == 4 ? backward.length / forward.length : 1f;
            }
            tree.children = children;
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(controller);
        }

        static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion)
        {
            AnimatorState state = machine.states.Select(s => s.state)
                .FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
            state.motion = motion;
            return state;
        }

        static void Bind(GameObject model, AnimatorController controller, Avatar avatar,
            FeelStudyPlayer player, PlayerCombat combat, EnemyCombatant enemy, NavMeshAgent agent,
            AnimationClip[] dodges, AnimationClip jump, AnimationClip sword, AnimationClip axe,
            AnimationClip hit, AnimationClip enemyAttack,
            GameObject swordVisual, GameObject axeVisual, GameObject combatAxeVisual)
        {
            Animator[] animators = model.GetComponents<Animator>();
            Animator animator = animators.FirstOrDefault(a => PrefabUtility.GetCorrespondingObjectFromSource(a) != null)
                ?? animators.FirstOrDefault();
            if (animator == null) animator = model.AddComponent<Animator>();
            foreach (Animator extra in animators)
                if (extra != animator) UnityEngine.Object.DestroyImmediate(extra);
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            CharacterAnimationDriver driver = model.GetComponent<CharacterAnimationDriver>();
            if (driver == null) driver = model.AddComponent<CharacterAnimationDriver>();
            SetReference(driver, "player", player);
            SetReference(driver, "playerCombat", combat);
            SetReference(driver, "enemy", enemy);
            SetReference(driver, "enemyAgent", agent);
            SetReference(driver, "dodgeForwardClip", dodges != null ? dodges[0] : null);
            SetReference(driver, "dodgeBackwardClip", dodges != null ? dodges[1] : null);
            SetReference(driver, "dodgeLeftClip", dodges != null ? dodges[2] : null);
            SetReference(driver, "dodgeRightClip", dodges != null ? dodges[3] : null);
            SetReference(driver, "jumpClip", jump);
            SetReference(driver, "swordClip", sword);
            SetReference(driver, "axeClip", axe);
            SetReference(driver, "hitClip", hit);
            SetReference(driver, "enemyAttackClip", enemyAttack);
            SetReference(driver, "swordVisual", swordVisual);
            SetReference(driver, "axeVisual", axeVisual);
            SetReference(driver, "combatAxeVisual", combatAxeVisual);
        }

        static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            var field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException($"Missing field {name} on {target.name}");
            field.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetReference(UnityEngine.Object target, string name, bool value)
        {
            var data = new SerializedObject(target);
            var field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException($"Missing field {name} on {target.name}");
            field.boolValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
