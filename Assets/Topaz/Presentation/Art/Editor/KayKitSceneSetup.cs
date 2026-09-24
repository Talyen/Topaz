using System;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Uses selected CC0 KayKit models in the existing authored study scene.</summary>
    public static class KayKitSceneSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string ArtRoot = "Assets/Topaz/Presentation/Art";
        const string ThirdParty = "Assets/ThirdParty/KayKit";

        [MenuItem("Topaz/Apply KayKit Art Study")]
        public static void Configure()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ArtRoot + "/Materials");
            EnsureFolder(ArtRoot + "/Prefabs");
            Material rogue = Mat("Rogue", "Adventurers/Textures/rogue_texture.png");
            Material skeleton = Mat("Skeleton", "Skeletons/Textures/skeleton_texture.png");
            Material forest = Mat("Forest", "Forest/Textures/forest_texture.png");
            Material dungeon = Mat("Dungeon", "Dungeon/Textures/dungeon_texture.png");
            Material resources = Mat("Resources", "ResourceBits/Textures/resource_bits_texture.png");
            Material tools = Mat("Tools", "RPGTools/Textures/tools_bits_texture.png");
            Material weapons = Mat("Weapons", "FantasyWeapons/Textures/weapons_bits_texture.png");
            Material ground = ColorMat("Moss Ground", new Color(.29f,.34f,.29f));
            HarvestDefinition harvestValues = AssetDatabase.LoadAssetAtPath<HarvestDefinition>(
                "Assets/Topaz/Gameplay/WorldLoop/Definitions/Tree.asset");
            var harvestData = new SerializedObject(harvestValues);
            harvestData.FindProperty("loggingExperiencePerChop").intValue = 1;
            harvestData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(harvestValues);

            GameObject pickupPrefab = CreatePickupPrefab(resources);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject feel = GameObject.Find("Feel Study");
            GameObject player = GameObject.Find("Player");
            GameObject world = GameObject.Find("World Loop Study");
            GameObject combat = GameObject.Find("Combat Study");
            if (feel == null || player == null || world == null || combat == null)
                throw new InvalidOperationException("The existing Topaz studies must be present.");
            Transform previous = feel.transform.Find("KayKit Art Study");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var art = new GameObject("KayKit Art Study");
            art.transform.SetParent(feel.transform, false);

            Transform facing = player.transform.Find("Facing Visual");
            Hide(facing, "Body", "Facing Notch");
            Renderer playerRenderer = Attach(facing, "Adventurers/Characters/Rogue.fbx",
                rogue, 1.75f, "KayKit Rogue");
            FeelStudyPlayer mover = player.GetComponent<FeelStudyPlayer>();
            Ref(mover, "bodyRenderer", playerRenderer);
            Color(mover, "normalBodyTint", UnityEngine.Color.white);
            Color(mover, "dodgeBodyTint", new Color(.70f,.95f,1f));
            Color(mover, "hitBodyTint", new Color(1f,.42f,.35f));

            Transform enemy = combat.transform.Find("Enemy");
            Transform enemyVisual = enemy.Find("Enemy Visual");
            Hide(enemyVisual, "Enemy Body", "Enemy Facing");
            Renderer enemyRenderer = Attach(enemyVisual,
                "Skeletons/Characters/Skeleton_Minion.fbx", skeleton, 1.55f,
                "KayKit Skeleton");
            Ref(enemy.GetComponent<EnemyCombatant>(), "bodyRenderer", enemyRenderer);

            Transform tree = world.transform.Find("Authored Tree 01");
            Transform treeVisual = tree.Find("Tree Visual");
            Hide(treeVisual, "Trunk", "Canopy");
            Renderer treeRenderer = Attach(treeVisual,
                "Forest/Models/Tree_4_A_Color1.fbx", forest, 3.5f,
                "KayKit Harvest Tree");
            HarvestTree harvest = tree.GetComponent<HarvestTree>();
            Ref(harvest, "trunkRenderer", treeRenderer);
            Color(harvest, "idleTint", UnityEngine.Color.white);

            Transform bench = world.transform.Find("Workbench");
            Hide(bench, "Table", "Tool Block");
            Attach(bench, "Dungeon/Models/table_small.fbx", dungeon, .75f, "KayKit Workbench");
            Transform rest = world.transform.Find("Rest Point");
            Hide(rest, "Bedroll");
            Attach(rest, "Dungeon/Models/bed_floor.fbx", dungeon, .23f, "KayKit Bedroll");
            Transform chest = world.transform.Find("Storage Chest");
            bool wasActive = chest.gameObject.activeSelf;
            chest.gameObject.SetActive(true);
            Hide(chest, "Body", "Lid");
            Renderer chestRenderer = Attach(chest, "Dungeon/Models/chest.fbx",
                dungeon, .75f, "KayKit Chest");
            Ref(chest.GetComponent<StorageChest>(), "chestRenderer", chestRenderer);
            chest.gameObject.SetActive(wasActive);

            Transform axe = facing.Find("Axe Pivot");
            Hide(axe, "Handle", "Head");
            bool axeActive = axe.gameObject.activeSelf;
            axe.gameObject.SetActive(true);
            Attach(axe, "RPGTools/Models/axe.fbx", tools, 1.22f, "KayKit Axe");
            axe.gameObject.SetActive(axeActive);
            Transform sword = facing.Find("Sword Pivot");
            Hide(sword, "Practice Blade");
            bool swordActive = sword.gameObject.activeSelf;
            sword.gameObject.SetActive(true);
            Attach(sword, "FantasyWeapons/Models/sword_A.fbx", weapons, 1.25f, "KayKit Sword");
            sword.gameObject.SetActive(swordActive);

            AddDecoration(art.transform, "Forest/Models/Tree_4_B_Color1.fbx", forest,
                new Vector3(-8f,0f,5f), 3.4f);
            AddDecoration(art.transform, "Forest/Models/Tree_4_C_Color1.fbx", forest,
                new Vector3(-8f,0f,-6f), 3.8f);
            AddDecoration(art.transform, "Forest/Models/Tree_4_A_Color1.fbx", forest,
                new Vector3(8f,0f,-6f), 3.5f);
            AddDecoration(art.transform, "Forest/Models/Bush_1_A_Color1.fbx", forest,
                new Vector3(-6.5f,0f,1.4f), .65f);
            AddDecoration(art.transform, "Forest/Models/Rock_3_B_Color1.fbx", forest,
                new Vector3(7.8f,0f,3f), 1.0f);

            Transform navigation = feel.transform.Find("Navigation Geometry");
            if (navigation != null)
            {
                string[] rocks = { "Rock_3_B_Color1.fbx", "Rock_3_D_Color1.fbx",
                    "Rock_3_A_Color1.fbx", "Rock_3_C_Color1.fbx" };
                for (int i = 0; i < rocks.Length; i++)
                {
                    Transform obstacle = navigation.Find($"Obstacle {i + 1}");
                    if (obstacle == null) continue;
                    obstacle.GetComponent<Renderer>().enabled = false;
                    AddDecoration(art.transform, "Forest/Models/" + rocks[i], forest,
                        new Vector3(obstacle.position.x, 0f, obstacle.position.z), 1.45f);
                }
            }
            foreach (Transform child in feel.transform)
                if (child.name.StartsWith("Grid ")) child.gameObject.SetActive(false);

            Transform groundObject = feel.transform.Find("Navigation Geometry/Ground");
            if (groundObject != null)
                groundObject.GetComponent<Renderer>().sharedMaterial = ground;

            WorldSession session = player.GetComponent<WorldSession>();
            Ref(session, "pickupPrefab", pickupPrefab);
            if (new SerializedObject(session).FindProperty("pickupPrefab").objectReferenceValue != pickupPrefab)
                throw new InvalidOperationException("Pickup prefab reference did not persist.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] KayKit art study is ready.");
            Topaz.AnimationStudy.Editor.AnimationStudySetup.Configure();
            GroundArtSetup.ApplyHome();
        }

        static GameObject CreatePickupPrefab(Material material)
        {
            const string path = ArtRoot + "/Prefabs/Wood Pickup.prefab";
            var root = new GameObject("Wood Pickup");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Attach(visual.transform, "ResourceBits/Models/Wood_Log_Stack.fbx", material,
                .45f, "KayKit Wood Logs");
            WorldPickup pickup = root.AddComponent<WorldPickup>();
            Ref(pickup, "visual", visual.transform);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (saved == null) throw new InvalidOperationException("Could not save Wood pickup prefab.");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static void AddDecoration(Transform parent, string path, Material material,
            Vector3 position, float height)
        {
            var anchor = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = position;
            Attach(anchor.transform, path, material, height, "Visual");
        }

        static Renderer Attach(Transform parent, string path, Material material,
            float height, string name)
        {
            foreach (Transform duplicate in parent.Cast<Transform>()
                .Where(child => child.name == name).ToArray())
                UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ThirdParty + "/" + path);
            if (model == null) throw new InvalidOperationException("KayKit model missing: " + path);
            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate: " + path);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Model has no renderer: " + path);
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y > 0.001f)
                instance.transform.localScale = Vector3.one * (height / bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            instance.transform.position += Vector3.up * (parent.position.y - bounds.min.y);
            return renderers[0];
        }

        static Material Mat(string name, string texture)
        {
            string path = ArtRoot + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ThirdParty + "/" + texture);
            if (atlas == null) throw new InvalidOperationException("KayKit texture missing: " + texture);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { name = name, enableInstancing = true };
            material.SetTexture("_BaseMap", atlas);
            material.SetColor("_BaseColor", UnityEngine.Color.white);
            material.SetFloat("_Smoothness", .12f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material ColorMat(string name, UnityEngine.Color tint)
        {
            string path = ArtRoot + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", .08f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void Hide(Transform parent, params string[] names)
        {
            foreach (string name in names)
            {
                Transform child = parent.Find(name);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            field.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Color(UnityEngine.Object target, string name, UnityEngine.Color value)
        {
            var data = new SerializedObject(target);
            SerializedProperty field = data.FindProperty(name);
            if (field == null) throw new InvalidOperationException($"Missing {name} on {target.name}");
            field.colorValue = value;
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
