using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.AssetReview.Editor
{
    /// <summary>Replaces reviewed source models in authored scenes without rebuilding those scenes.</summary>
    public static class AssetReviewSceneMigration
    {
        const string PlanPath = "AssetReview/replacements.json";
        static readonly string[] Scenes = {
            "Assets/Topaz/World/Scenes/Bootstrap.unity",
            "Assets/Topaz/World/Scenes/Expedition.unity"
        };

        [Serializable]
        sealed class Plan
        {
            public int schemaVersion;
            public Entry[] replacements;
        }

        [Serializable]
        sealed class Entry
        {
            public string oldGuid;
            public string newPath;
        }

        [MenuItem("Topaz/Asset Review/Replace Archived Scene Models")]
        public static void Apply()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string file = Path.Combine(projectRoot, PlanPath);
            Plan plan = JsonUtility.FromJson<Plan>(File.ReadAllText(file));
            if (plan == null || plan.schemaVersion != 1 || plan.replacements == null)
                throw new InvalidOperationException("Invalid asset review replacement plan.");
            var replacements = plan.replacements.ToDictionary(entry => entry.oldGuid, entry => entry.newPath);
            foreach (Entry entry in plan.replacements)
            {
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(entry.oldGuid)) ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(entry.newPath) == null)
                    throw new InvalidOperationException("Missing model in replacement plan: " + entry.oldGuid);
            }

            int total = 0;
            foreach (string scenePath in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var instances = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(transform => transform.gameObject)
                    .Where(gameObject => PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) == gameObject)
                    .Select(gameObject => (gameObject,
                        guid: AssetDatabase.AssetPathToGUID(
                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject))))
                    .Where(item => replacements.ContainsKey(item.guid)).ToArray();
                foreach (var item in instances)
                {
                    Replace(scene, item.gameObject, replacements[item.guid]);
                    total++;
                }
                if (instances.Length > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Could not save scene: " + scenePath);
                }
                Debug.Log($"[Topaz] Replaced {instances.Length} archived model instances in {scenePath}.");
            }
            Debug.Log($"[Topaz] Asset review migration replaced {total} scene instances.");
        }

        static void Replace(Scene scene, GameObject instance, string newPath)
        {
            GameObject target = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
            Renderer[] oldRenderers = instance.GetComponentsInChildren<Renderer>(true);
            if (oldRenderers.Length == 0)
                throw new InvalidOperationException("Source instance has no renderer: " + instance.name);
            float height = BoundsOf(oldRenderers).size.y;
            float bottom = BoundsOf(oldRenderers).min.y;
            Material material = oldRenderers[0].sharedMaterial;
            Renderer oldRenderer = oldRenderers[0];
            var linkedComponents = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(component => component != null &&
                    new SerializedObject(component).FindProperty("trunkRenderer")?.objectReferenceValue == oldRenderer)
                .ToArray();

            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(instance, target, InteractionMode.AutomatedAction);
            Renderer[] newRenderers = instance.GetComponentsInChildren<Renderer>(true);
            if (newRenderers.Length == 0)
                throw new InvalidOperationException("Replacement has no renderer: " + newPath);
            foreach (Renderer renderer in newRenderers)
            {
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            float newHeight = BoundsOf(newRenderers).size.y;
            if (height > 0.001f && newHeight > 0.001f)
                instance.transform.localScale *= height / newHeight;
            instance.transform.position += Vector3.up * (bottom - BoundsOf(newRenderers).min.y);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            foreach (MonoBehaviour component in linkedComponents)
            {
                var serialized = new SerializedObject(component);
                serialized.FindProperty("trunkRenderer").objectReferenceValue = newRenderers[0];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
            }
        }

        static Bounds BoundsOf(Renderer[] renderers)
        {
            Bounds result = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) result.Encapsulate(renderer.bounds);
            return result;
        }
    }
}
