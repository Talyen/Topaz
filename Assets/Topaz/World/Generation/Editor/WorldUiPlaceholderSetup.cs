using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Topaz.Generation.Editor
{
    public static class WorldUiPlaceholderSetup
    {
        [MenuItem("Topaz/Generation/Configure Placeholder UI Art")]
        public static void Apply()
        {
            string root="Assets/Topaz/Presentation/Rendering/Environment/";
            Sprite Create(string name,bool portrait)
            {
                string path=root+name+".asset";var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(existing!=null)return existing;
                var tex=new Texture2D(64,64){name=name};var pixels=new Color[4096];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    bool figure=portrait&&((x-32)*(x-32)+(y-43)*(y-43)<100 || y>8&&y<33&&Mathf.Abs(x-32)<14);
                    pixels[y*64+x]=figure?new Color(.19f,.34f,.4f):new Color(.94f,.87f,.74f);
                }
                tex.SetPixels(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,path);var sprite=Sprite.Create(tex,new Rect(0,0,64,64),Vector2.one*.5f,64);sprite.name=name;AssetDatabase.AddObjectToAsset(sprite,tex);return sprite;
            }
            var portrait=Create("Prototype Portrait",true);var background=Create("Prototype Journal",false);
            var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if(component==null||!(component.GetType().Namespace??"").StartsWith("Topaz"))continue;
                var so=new SerializedObject(component);var p=so.GetIterator();
                while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.type.Contains("Sprite")&&p.objectReferenceValue==null)
                    p.objectReferenceValue=p.propertyPath.ToLowerInvariant().Contains("background")?background:portrait;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var go in scene.GetRootGameObjects())if(go.name=="HDRP Effects")go.name="World Effects";
            var stage=scene.GetRootGameObjects().First(g=>g.name=="Main Menu Stage");
            var stageData=new SerializedObject(stage.GetComponent("MainMenuStage"));
            var anchor=(Transform)stageData.FindProperty("actorAnchor").objectReferenceValue;
            var old=stage.transform.Find("Woodland Menu Backdrop");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var backdrop=new GameObject("Woodland Menu Backdrop");backdrop.transform.SetParent(stage.transform,false);backdrop.transform.position=anchor.position;
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>(root+"Woodland.asset");
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(ground.GetComponent<Collider>());ground.transform.SetParent(backdrop.transform,false);ground.transform.localPosition=Vector3.down*.15f;ground.transform.localScale=new Vector3(18,.2f,18);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"Menu Ground.mat");if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,root+"Menu Ground.mat");}mat.SetTexture("_BaseMap",preset.grass.diffuseTexture);mat.SetTextureScale("_BaseMap",Vector2.one*4);ground.GetComponent<Renderer>().sharedMaterial=mat;
            for(int i=0;i<3;i++){var tree=(GameObject)PrefabUtility.InstantiatePrefab(preset.treeVisual,backdrop.transform);tree.transform.localPosition=new Vector3((i-1)*4,0,4);tree.transform.localScale=Vector3.one*.7f;}
            foreach(var t in backdrop.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;

            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
