using System;
using Topaz.Expedition;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Authors stable prompt anchors on the existing interaction owners.</summary>
    public static class InteractionCueSetup
    {
        [MenuItem("Topaz/Apply Interaction Cue Anchors")]
        public static void Apply()
        {
            ApplyHome();
            ApplyExpedition();
        }

        public static void ApplyHome()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Topaz/World/Scenes/Bootstrap.unity", OpenSceneMode.Single);
            WorldSession session = UnityEngine.Object.FindFirstObjectByType<WorldSession>();
            if (session == null) throw new InvalidOperationException("Home WorldSession is missing.");
            var data = new SerializedObject(session);
            EnsureAnchor(Reference(data, "workbench"), 1.1f);
            EnsureAnchor(Reference(data, "restPoint"), .75f);
            EnsureAnchor(Reference(data, "tree"), 1.8f);
            EnsureAnchor(Reference(data, "chest"), .95f);
            EnsureAnchor(Reference(data, "homeGate"), 2.2f);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ApplyExpedition()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Topaz/World/Scenes/Expedition.unity", OpenSceneMode.Single);
            ExpeditionSceneBootstrap context =
                UnityEngine.Object.FindFirstObjectByType<ExpeditionSceneBootstrap>();
            if (context == null) throw new InvalidOperationException("Expedition context is missing.");
            var data = new SerializedObject(context);
            EnsureAnchor(Reference(data, "departure"), 2.2f);
            EnsureAnchor(Reference(data, "supplyCache"), 1.0f);
            EditorSceneManager.SaveScene(scene);
        }

        static Transform Reference(SerializedObject data, string name)
        {
            UnityEngine.Object value = data.FindProperty(name)?.objectReferenceValue;
            return value is Transform transform ? transform :
                value is Component component ? component.transform : null;
        }

        static void EnsureAnchor(Transform owner, float height)
        {
            if (owner == null) return;
            Transform anchor = owner.Find("Interaction Anchor");
            if (anchor == null)
            {
                anchor = new GameObject("Interaction Anchor").transform;
                anchor.SetParent(owner, false);
            }
            anchor.localPosition = new Vector3(0f, height, 0f);
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
        }
    }
}
