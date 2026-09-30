using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    /// <summary>Shared owned-prefab helpers for current character and Alpine authoring.</summary>
    public static class SyntySampleSetup
    {
        internal const string WorldRoot = "Assets/Topaz/Presentation/Art/World/";
        internal const string CharacterRoot = "Assets/Topaz/Presentation/Art/Characters/";
        internal static void Set(Object o,string field,Object value)
        { var so=new SerializedObject(o);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(o.name+" missing "+field);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
        internal static GameObject Instance(GameObject prefab,Transform parent)
        { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;return go; }
        internal static void Fit(GameObject model,Vector3 size,Vector3 bottom)
        {
            // Measure in the model parent's coordinates, independent of scene placement and scale.
            var parent=model.transform.parent;var p=parent.position;var q=parent.rotation;var scale=parent.localScale;
            parent.position=Vector3.zero;parent.rotation=Quaternion.identity;parent.localScale=Vector3.one;
            var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            var factor=new Vector3(size.x/Mathf.Max(.01f,b.size.x),size.y/Mathf.Max(.01f,b.size.y),size.z/Mathf.Max(.01f,b.size.z));
            model.transform.localScale=Vector3.Scale(model.transform.localScale,factor);
            model.transform.localPosition=bottom-Vector3.Scale(new Vector3(b.center.x,b.min.y,b.center.z),factor);
            parent.position=p;parent.rotation=q;parent.localScale=scale;
        }
        internal static GameObject Save(GameObject go,string name)
        {var prefab=PrefabUtility.SaveAsPrefabAsset(go,ArtPath(name,".prefab"));Object.DestroyImmediate(go);return prefab;}
        internal static string ArtPath(string name,string extension)
        {
            bool character=name=="Wanderer"||name=="Wanderer Female"||name=="Skeleton"||
                name=="Storybook Knight"||name.StartsWith("Wilderness Skeleton ",StringComparison.Ordinal);
            return (character?CharacterRoot:WorldRoot)+name+extension;
        }
    }
}
