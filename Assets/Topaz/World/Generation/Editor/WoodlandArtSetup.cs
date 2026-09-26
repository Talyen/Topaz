using System.Linq;
using Topaz.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Topaz.Generation.Editor
{
    public static class WoodlandArtSetup
    {
        const string Root="Assets/Topaz/Presentation/Rendering/Environment/";
        const string Sample="Assets/ThirdParty/UnityTerrainSample/";
        [MenuItem("Topaz/Generation/Bind Unity Terrain Sample")]
        public static void Apply()
        {
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>(Root+"Woodland.asset");
            preset.treeVisual=Wrap("Pine A","Prefabs/Trees/Pines/Pine_A/Pine_A.prefab",.4f,true);
            preset.treeVariant=Wrap("Pine C","Prefabs/Trees/Pines/Pine_C/Pine_C.prefab",.5f,true);
            preset.rockVisual=Wrap("Rock","Prefabs/Rocks/Rock_A_02.prefab",.6f,false);
            preset.grass=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Sample+"Terrain/Layers/Grass_A.terrainlayer");
            preset.path=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Sample+"Terrain/Layers/Grass_Soil_A.terrainlayer");
            preset.detail=AssetDatabase.LoadAssetAtPath<GameObject>(Sample+"Prefabs/Details/Grass_A.prefab");
            EditorUtility.SetDirty(preset);
            foreach(string name in new[]{"Bootstrap","Woodland"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/"+name+".unity");
                foreach(var tree in All<HarvestTree>(scene))
                {
                    Replace(tree,preset.treeVisual,"trunkCollider");
                    var ts=new SerializedObject(tree);ts.FindProperty("idleTint").colorValue=Color.white;ts.ApplyModifiedPropertiesWithoutUndo();
                    if(name=="Woodland")tree.transform.SetParent(scene.GetRootGameObjects().First(g=>g.name=="Woodland").transform,true);
                }
                foreach(var rock in All<MiningRock>(scene))
                {
                    Replace(rock,preset.rockVisual,"rockCollider");
                    if(name=="Woodland")rock.transform.SetParent(scene.GetRootGameObjects().First(g=>g.name=="Woodland").transform,true);
                }
                if(name=="Bootstrap")
                {
                    if(All<WindZone>(scene).Length==0){var wind=new GameObject("Woodland Wind").AddComponent<WindZone>();wind.mode=WindZoneMode.Directional;wind.windMain=.65f;wind.windTurbulence=.25f;wind.transform.rotation=Quaternion.Euler(0,35,0);}
                    var session=All<WorldSession>(scene).Single();
                    var pickup=new GameObject("World Pickup");var visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);visual.transform.SetParent(pickup.transform,false);visual.transform.localPosition=Vector3.up*.3f;visual.transform.localScale=Vector3.one*.25f;Object.DestroyImmediate(visual.GetComponent<Collider>());
                    Set(pickup.AddComponent<WorldPickup>(),"visual",visual.transform);var prefab=PrefabUtility.SaveAsPrefabAsset(pickup,Root+"World Pickup.prefab");Object.DestroyImmediate(pickup);Set(session,"pickupPrefab",prefab);
                }
                foreach(var r in All<Renderer>(scene))if(!r.enabled&&r.GetComponentInParent<Canvas>()==null&&r.GetComponentInParent<Topaz.Characters.CharacterVisual>()==null&&!(r is LineRenderer))
                {var f=r.GetComponent<MeshFilter>();if(f!=null)f.sharedMesh=null;r.sharedMaterials=new Material[0];}
                foreach(var c in All<MeshCollider>(scene))if(!c.enabled)c.sharedMesh=null;
                foreach(var appearance in All<Topaz.Characters.PlayerAppearance>(scene))
                {
                    var so=new SerializedObject(appearance);so.FindProperty("playerController").objectReferenceValue=null;
                    var looks=so.FindProperty("looks");for(int i=0;i<looks.arraySize;i++){looks.GetArrayElementAtIndex(i).FindPropertyRelative("avatar").objectReferenceValue=null;looks.GetArrayElementAtIndex(i).FindPropertyRelative("material").objectReferenceValue=null;}so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
        }
        static GameObject Wrap(string label,string source,float scale,bool tree)
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Sample+source);if(original==null)throw new System.IO.FileNotFoundException("Restore the Unity URP terrain sample first: "+source);
            var root=new GameObject(label);var visual=(GameObject)PrefabUtility.InstantiatePrefab(original,root.transform);visual.transform.localScale=Vector3.one*scale;visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
            foreach(var c in root.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            if(tree){var c=root.AddComponent<CapsuleCollider>();c.center=Vector3.up*4;c.height=8;c.radius=.35f;}
            else {var c=root.AddComponent<SphereCollider>();c.center=Vector3.up*.4f;c.radius=.7f;}
            foreach(var lod in root.GetComponentsInChildren<LODGroup>())lod.animateCrossFading=false;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+label+".prefab");Object.DestroyImmediate(root);return prefab;
        }
        static void Replace(Component node,GameObject prefab,string collider)
        {
            var so=new SerializedObject(node);var old=so.FindProperty("visualRoot").objectReferenceValue as GameObject;if(old!=null && old!=node.gameObject)Object.DestroyImmediate(old);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,node.transform);visual.transform.localPosition=Vector3.zero;
            so.FindProperty("visualRoot").objectReferenceValue=visual;so.FindProperty(collider).objectReferenceValue=visual.GetComponent<Collider>();so.ApplyModifiedPropertiesWithoutUndo();
        }
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Set(Object o,string f,Object v){var s=new SerializedObject(o);s.FindProperty(f).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
    }
}
