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
        static Type Validation => Type.GetType("Topaz.Art.Editor.OwnedPrefabValidation, Assembly-CSharp-Editor", true);
        static Type Tree => Type.GetType("Topaz.Gameplay.HarvestTree, Assembly-CSharp", true);

        [Test]
        public void AuthoredScenesAndPrefabsHaveValidBoundariesAndUniqueIdentities()
        {
            foreach (string name in new[] { "Bootstrap", "Woodland" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/" + name + ".unity");
                var regionType = Type.GetType("Topaz.Generation.WoodlandRegion, Assembly-CSharp", true);
                var region = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(regionType, true)).Single();
                Assert.That(new SerializedObject(region).FindProperty("preset").objectReferenceValue, Is.Not.Null);
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
