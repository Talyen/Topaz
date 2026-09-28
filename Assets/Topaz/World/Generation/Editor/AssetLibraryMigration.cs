using System;
using System.Linq;
using Topaz.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    /// <summary>One-time, repeatable migration from the early sample asset layout.</summary>
    public static class AssetLibraryMigration
    {
        const string OldArt = "Assets/Topaz/Presentation/Rendering/SyntySample";
        const string Art = "Assets/Topaz/Presentation/Art";
        const string World = Art + "/World";
        const string Characters = Art + "/Characters";
        const string Generic = "Assets/Synty/PolygonGeneric/Prefabs/Environment/";

        [MenuItem("Topaz/Assets/Organize Owned Art And Retire Legacy Vendors")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Topaz/Presentation", "Art");
            if (AssetDatabase.IsValidFolder(OldArt)) Move(OldArt, World);
            if (!AssetDatabase.IsValidFolder(Characters)) AssetDatabase.CreateFolder(Art, "Characters");
            foreach (string name in new[] { "Wanderer", "Wanderer Female", "Skeleton", "Storybook Knight",
                         "Wilderness Skeleton 0", "Wilderness Skeleton 1", "Wilderness Skeleton 2" })
                MoveIfPresent(World + "/" + name + ".prefab", Characters + "/" + name + ".prefab");
            foreach (string name in new[] { "Wanderer Motion", "Skeleton Motion" })
                MoveIfPresent(World + "/" + name + ".controller", Characters + "/" + name + ".controller");
            MoveIfPresent(World + "/Sidekick Knight.mat", Characters + "/Sidekick Knight.mat");

            ReplaceSampleVisual("Pine A", "SM_Gen_Env_Tree_Pine_01.prefab", 8f);
            ReplaceSampleVisual("Pine C", "SM_Gen_Env_Tree_Pine_02.prefab", 8f);
            ReplaceSampleVisual("Rock", "SM_Gen_Env_Rock_01.prefab", 1.2f);
            var ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Topaz/Presentation/Rendering/Environment/Menu Ground.mat");
            var grass = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Synty/PolygonGeneric/Textures/Generic_Grass.png");
            if (ground == null || grass == null) throw new InvalidOperationException("Missing menu ground or Synty grass texture");
            if (ground.HasProperty("_BaseMap")) ground.SetTexture("_BaseMap", grass);
            if (ground.HasProperty("_MainTex")) ground.SetTexture("_MainTex", grass);
            EditorUtility.SetDirty(ground);

            foreach (string path in new[] { "Assets/Topaz/Gameplay/Combat/Definitions/SwordWeapon.asset",
                         "Assets/Topaz/Gameplay/Combat/Definitions/TwoHandedAxeWeapon.asset",
                         "Assets/Topaz/Gameplay/Combat/Definitions/Weapons/PlayerCrossbowShot.asset",
                         "Assets/Topaz/Gameplay/Combat/Definitions/Weapons/RogueShot.asset" })
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null) throw new InvalidOperationException("Missing weapon: " + path);
                var serialized = new SerializedObject(asset);
                foreach (var key in new[] { "swingClip", "impactClip", "fireClip", "readyClip" })
                {
                    var property = serialized.FindProperty(key);
                    if (property != null && property.objectReferenceValue is AudioClip clip &&
                        AssetDatabase.GetAssetPath(clip).StartsWith("Assets/ThirdParty/Kenney/", StringComparison.Ordinal))
                        property.objectReferenceValue = null;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (string name in new[] { "Bootstrap", "Woodland" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/" + name + ".unity");
                foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.name.StartsWith("KayKit ", StringComparison.Ordinal))
                        transform.name = "Storybook " + transform.name.Substring(7);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var spell in root.GetComponentsInChildren<GroundSpellAbility>(true))
                {
                    var serialized = new SerializedObject(spell);
                    foreach (var key in new[] { "castClip", "impactClip" })
                    {
                        var property = serialized.FindProperty(key);
                        if (property != null && property.objectReferenceValue is AudioClip clip &&
                            AssetDatabase.GetAssetPath(clip).StartsWith("Assets/ThirdParty/JaggedStone/", StringComparison.Ordinal))
                            property.objectReferenceValue = null;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            foreach (string path in new[] { "Assets/ThirdParty/UnityTerrainSample", "Assets/ThirdParty/Kenney", "Assets/ThirdParty/JaggedStone" })
                if (AssetDatabase.IsValidFolder(path) && !AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException("Could not remove legacy vendor folder: " + path);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            Debug.Log("Topaz asset library migration complete: art organized; legacy art/audio retired.");
        }

        static void MoveIfPresent(string source, string target)
        {
            if (AssetDatabase.LoadMainAssetAtPath(source) != null) Move(source, target);
        }

        static void Move(string source, string target)
        {
            string error = AssetDatabase.MoveAsset(source, target);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(source + " -> " + target + ": " + error);
        }

        static void ReplaceSampleVisual(string wrapper, string source, float height)
        {
            string path = "Assets/Topaz/Presentation/Rendering/Environment/" + wrapper + ".prefab";
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(Generic + source);
            if (original == null) throw new InvalidOperationException("Missing Synty replacement: " + source);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) throw new InvalidOperationException("Missing wrapper: " + path);
            try
            {
                foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(original, root.transform);
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("Replacement has no renderer: " + source);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = height / Mathf.Max(0.01f, bounds.size.y);
                model.transform.localScale *= scale;
                model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
