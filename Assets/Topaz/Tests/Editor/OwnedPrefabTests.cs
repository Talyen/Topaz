using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Topaz.Tests.Editor
{
    public sealed class OwnedPrefabTests
    {
        static Type Migration => Type.GetType("Topaz.Art.Editor.OwnedPrefabMigration, Assembly-CSharp-Editor", true);
        static Type Validation => Type.GetType("Topaz.Art.Editor.OwnedPrefabValidation, Assembly-CSharp-Editor", true);
        static Type Tree => Type.GetType("Topaz.LoopStudy.HarvestTree, Assembly-CSharp", true);

        [Test]
        public void AuthoredScenesAndPrefabsHaveValidBoundariesAndUniqueIdentities()
        {
            Validation.GetMethod("Validate").Invoke(null, null);
        }

        [Test]
        public void SharedVisualReplacementReachesTwoScenesAndKeepsGameplayAndIdentity()
        {
            string folder = "Assets/TopazPrefabTest_" + Guid.NewGuid().ToString("N");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            string prefabPath = folder + "/Tree.prefab";
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var root = new GameObject("Authored Tree");
                    root.transform.position = new Vector3(i * 8f, 0f, 3f);
                    root.AddComponent<CapsuleCollider>().radius = .4f + i * .2f;
                    if (i == 1) root.AddComponent(Type.GetType("Topaz.CombatStudy.SafeZone, Assembly-CSharp", true));
                    Component tree = root.AddComponent(Tree);
                    var data = new SerializedObject(tree);
                    data.FindProperty("stableObjectId").stringValue = "test.tree." + i;
                    GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = "Visual";
                    visual.transform.SetParent(root.transform, false);
                    Object.DestroyImmediate(visual.GetComponent<Collider>());
                    data.FindProperty("visualRoot").objectReferenceValue = visual;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    Migration.GetMethod("Connect", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { root, prefabPath });
                    EditorSceneManager.SaveScene(scene, folder + "/Scene" + i + ".unity");
                }
                string guidBefore = AssetDatabase.AssetPathToGUID(prefabPath);
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                Assert.That(new SerializedObject(contents.GetComponent(Tree)).FindProperty("stableObjectId").stringValue, Is.Empty);
                Object.DestroyImmediate(contents.transform.Find("Visual").gameObject);
                GameObject replacement = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                replacement.name = "Visual";
                replacement.transform.SetParent(contents.transform, false);
                replacement.transform.localScale = new Vector3(2f, 4f, 2f);
                Object.DestroyImmediate(replacement.GetComponent<Collider>());
                var treeData = new SerializedObject(contents.GetComponent(Tree));
                treeData.FindProperty("visualRoot").objectReferenceValue = replacement;
                treeData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                PrefabUtility.UnloadPrefabContents(contents);
                for (int i = 0; i < 2; i++)
                {
                    Scene scene = EditorSceneManager.OpenScene(folder + "/Scene" + i + ".unity", OpenSceneMode.Single);
                    GameObject root = scene.GetRootGameObjects().Single();
                    Assert.That(root.transform.position, Is.EqualTo(new Vector3(i * 8f, 0f, 3f)));
                    Assert.That(root.GetComponent<CapsuleCollider>().radius, Is.EqualTo(.4f + i * .2f).Within(.001f));
                    if (i == 1) Assert.That(root.GetComponent("SafeZone"), Is.Not.Null,
                        "A scene-specific safe zone must survive conversion to the shared gameplay prefab.");
                    Assert.That(root.transform.Find("Visual").localScale, Is.EqualTo(new Vector3(2f, 4f, 2f)));
                    Assert.That(new SerializedObject(root.GetComponent(Tree)).FindProperty("stableObjectId").stringValue,
                        Is.EqualTo("test.tree." + i));
                }
                // A later migration consumes the artist-edited asset instead of regenerating it.
                Scene third = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var fresh = new GameObject("Another Tree");
                fresh.AddComponent(Tree);
                Migration.GetMethod("Connect", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { fresh, prefabPath });
                Assert.That(fresh.transform.Find("Visual").localScale, Is.EqualTo(new Vector3(2f, 4f, 2f)));
                Assert.That(AssetDatabase.AssetPathToGUID(prefabPath), Is.EqualTo(guidBefore));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        [Test]
        public void IdentityValidationRejectsMissingAndDuplicateIds()
        {
            var root = new GameObject("Identity Test");
            try
            {
                var ids = new HashSet<string>();
                var errors = new List<string>();
                Component tree = root.AddComponent(Tree);
                var data = new SerializedObject(tree);
                data.FindProperty("stableObjectId").stringValue = "";
                data.ApplyModifiedPropertiesWithoutUndo();
                Validation.GetMethod("Check").Invoke(null, new object[] { root, false, ids, errors });
                Assert.That(errors.Any(e => e.Contains("Missing or duplicate")), Is.True);
                errors.Clear();
                data.FindProperty("stableObjectId").stringValue = "duplicate";
                data.ApplyModifiedPropertiesWithoutUndo();
                ids.Add("duplicate");
                Validation.GetMethod("Check").Invoke(null, new object[] { root, false, ids, errors });
                Assert.That(errors.Any(e => e.Contains("Missing or duplicate")), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
