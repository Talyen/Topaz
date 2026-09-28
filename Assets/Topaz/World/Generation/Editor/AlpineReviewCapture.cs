using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Generation.Editor
{
    public static class AlpineReviewCapture
    {
        public static void PublishWarmup() => PublishWarmup("TestResults/artifacts/viking-alpine-player-dev/Alpine-Metal.graphicsstate");
        public static void PublishWarmup(string source)
        {
            const string path="Assets/Topaz/Presentation/Art/World/Alpine Metal Warmup.asset";
            var collection=new GraphicsStateCollection(Path.GetFullPath(source));
            if(collection.runtimePlatform!=RuntimePlatform.OSXPlayer || collection.graphicsDeviceType!=GraphicsDeviceType.Metal)throw new InvalidOperationException("Expected a Metal standalone-player trace.");
            if(collection.totalGraphicsStateCount==0)throw new InvalidOperationException("The trace contains no graphics states.");
            collection.name="Alpine Metal Warmup";
            var existing=AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(path);
            if(existing==null)AssetDatabase.CreateAsset(collection,path);
            else {EditorUtility.CopySerialized(collection,existing);UnityEngine.Object.DestroyImmediate(collection);collection=existing;}
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            preset.warmupCollections=new[]{collection};EditorUtility.SetDirty(collection);EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Published Metal warmup states: "+collection.totalGraphicsStateCount);
        }
        public static void PublishPortraits()
        {
            Characters();
            const string root="Assets/Topaz/UI/Art/VikingPortraits";
            if(!AssetDatabase.IsValidFolder(root))AssetDatabase.CreateFolder("Assets/Topaz/UI/Art","VikingPortraits");
            var sprites=new System.Collections.Generic.List<Sprite>();
            foreach(string id in Topaz.Gameplay.CharacterLooks.All)
            {
                string target=root+"/"+id.Replace('.','-')+".png";
                File.Copy("TestResults/artifacts/viking-alpine-characters/"+id+".png",target,true);AssetDatabase.ImportAsset(target);
                var importer=(TextureImporter)AssetImporter.GetAtPath(target);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.SaveAndReimport();
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(target));
            }
            File.WriteAllText(root+"/SOURCE.md","# Viking journal portraits\n\nGenerated locally from the private Viking Realm character wrappers by `Topaz.Generation.Editor.AlpineReviewCapture.PublishPortraits`. PNG pixels remain ignored; metadata is retained to preserve GUIDs. Restore the owned packs and run the authoring method with graphics enabled to regenerate and rebind all ten portraits. No source models or textures are redistributed here.\n");
            AssetDatabase.ImportAsset(root+"/SOURCE.md");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            foreach(var view in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Topaz.Gameplay.EquipmentJournalView>(true)))
            {
                var data=new SerializedObject(view);var field=data.FindProperty("portraits");field.arraySize=sprites.Count;
                for(int n=0;n<sprites.Count;n++)field.GetArrayElementAtIndex(n).objectReferenceValue=sprites[n];data.ApplyModifiedPropertiesWithoutUndo();
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        // Explicit offline renders, not a performance benchmark or proof of standalone behavior.
        public static void Characters()
        {
            string directory="TestResults/artifacts/viking-alpine-characters";Directory.CreateDirectory(directory);
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var cameraRoot=new GameObject("Review camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraRoot,scene);
            var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.cameraType=CameraType.Game;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);camera.fieldOfView=32;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=true;
            var lightRoot=new GameObject("Review light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightRoot,scene);
            var light=lightRoot.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,145,0);
            var target=new RenderTexture(512,640,24);target.Create();
            try
            {
                foreach(string id in Topaz.Gameplay.CharacterLooks.All)
                {
                    string label=Topaz.Gameplay.CharacterLooks.Label(id);
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Topaz/Presentation/Art/Characters/"+label+".prefab");
                    var model=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,scene);
                    var animator=model.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Play("Locomotion",0,0);animator.Update(.5f);
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    camera.transform.position=bounds.center+new Vector3(0,.1f,3.8f);camera.transform.LookAt(bounds.center);
                    camera.targetTexture=target;
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(512,640,TextureFormat.RGB24,false);
                    texture.ReadPixels(new Rect(0,0,512,640),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,id+".png"),texture.EncodeToPNG());
                    RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(model);
                }
            }
            finally {target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
