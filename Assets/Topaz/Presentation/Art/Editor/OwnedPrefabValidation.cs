using System;
using System.Collections.Generic;
using System.Linq;
using Topaz.AnimationStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Art.Editor
{
    public static class OwnedPrefabValidation
    {
        [MenuItem("Topaz/Prefabs/Validate Authored Content")]
        public static void Validate()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var errors = new List<string>();
            var ids = new HashSet<string>();
            try
            {
                foreach (string path in OwnedPrefabMigration.ScenePaths)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects()) Check(root, false, ids, errors);
                }
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Topaz" }))
                {
                    GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    Check(root, true, new HashSet<string>(), errors);
                }
                if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
                Debug.Log("[Topaz] Owned prefab references, visual bindings and authored identities are valid.");
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        public static void Check(GameObject root, bool asset, HashSet<string> ids, List<string> errors)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = transform.gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                    errors.Add("Missing script: " + go.name);
                string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (source.StartsWith("Assets/ThirdParty/") && source.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    bool insideVisual = false;
                    for (Transform parent = transform.parent; parent != null; parent = parent.parent)
                        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(parent.gameObject).Contains("/Visuals/") ||
                            AssetDatabase.GetAssetPath(parent.gameObject).Contains("/Visuals/"))
                        { insideVisual = true; break; }
                    if (!insideVisual) errors.Add("Imported model outside a Topaz visual prefab: " + go.name);
                }
                foreach (MonoBehaviour component in go.GetComponents<MonoBehaviour>().Where(c => c != null))
                {
                    var data = new SerializedObject(component);
                    var property = data.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None)
                            errors.Add("Missing reference: " + go.name + "." + property.propertyPath);
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue is GameObject model &&
                            AssetDatabase.GetAssetPath(model).StartsWith("Assets/ThirdParty/"))
                            errors.Add("Direct model spawn reference: " + go.name + "." + property.propertyPath);
                        if (property.propertyType != SerializedPropertyType.String ||
                            !(property.name == "stableObjectId" || (component.GetType().Name == "Campfire" && property.name == "stableId"))) continue;
                        string id = property.stringValue;
                        if (asset && !string.IsNullOrEmpty(id)) errors.Add("Prefab owns an instance ID: " + go.name + " / " + id);
                        if (!asset && (string.IsNullOrWhiteSpace(id) || !ids.Add(id)))
                            errors.Add("Missing or duplicate authored ID: " + go.name + " / " + id);
                    }
                }
                CharacterVisual visual = go.GetComponent<CharacterVisual>();
                if (visual != null && (visual.Animator == null || visual.BodyRenderer == null ||
                    !visual.Animator.transform.IsChildOf(visual.transform) || !visual.BodyRenderer.transform.IsChildOf(visual.transform)))
                    errors.Add("Incomplete character visual: " + go.name);
                if (visual != null && source.Contains("Player") && !visual.HasPlayerBindings)
                    errors.Add("Incomplete player sockets: " + go.name);
            }
        }

        [MenuItem("Topaz/Prefabs/Assign New IDs To Selected Objects")]
        public static void AssignSelectedIds()
        {
            foreach (GameObject root in Selection.gameObjects.Where(g => !EditorUtility.IsPersistent(g) &&
                !EditorSceneManager.IsPreviewScene(g.scene)))
            foreach (MonoBehaviour c in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null))
            {
                var data = new SerializedObject(c);
                var p = data.FindProperty(c.GetType().Name == "Campfire" ? "stableId" : "stableObjectId");
                if (p == null) continue;
                Undo.RecordObject(c, "Assign new authored identity");
                p.stringValue = "authored." + Guid.NewGuid().ToString("N");
                data.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
            }
        }

        [MenuItem("Topaz/Prefabs/Assign Missing Instance IDs In Open Scenes")]
        public static void AssignMissingIds()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
                foreach (MonoBehaviour component in scene.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c => c != null))
                {
                    var data = new SerializedObject(component);
                    var property = data.FindProperty(component.GetType().Name == "Campfire" ? "stableId" : "stableObjectId");
                    if (property == null || !string.IsNullOrWhiteSpace(property.stringValue)) continue;
                    Undo.RecordObject(component, "Assign authored identity");
                    property.stringValue = "authored." + Guid.NewGuid().ToString("N");
                    data.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }
    }
}
