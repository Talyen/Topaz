using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using Topaz;
using Topaz.Player;
using Topaz.Characters;
using Topaz.Generation;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class PreparePlayableReview {
 const string Content="Assets/Topaz/ReviewContent";
 public static string Main(string directory){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode before preparing review");
  if(!AssetDatabase.IsValidFolder(Content))AssetDatabase.CreateFolder("Assets/Topaz","ReviewContent");
  var path=Content+"/TopazVisualLab.shader";
  File.WriteAllText(path,File.ReadAllText("Assets/Topaz/Core/Editor/VisualLab/TopazVisualLab.shader").Replace("Topaz/Review/Visual Lab","Topaz/Review/Playable Visual Lab"));AssetDatabase.ImportAsset(path);
  var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
  var original=SceneManager.GetActiveScene();var templateCamera=UnityEngine.Object.FindAnyObjectByType<PlayerCamera>();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var fixture=new VisualLabFixture();var projected=new EnvironmentTextureSet(directory);
  string stage="fixture";
  try{
   fixture.BuildLabFixture(AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset"),shader);projected.Apply(fixture);
   var root=fixture.Root;root.name="Projected environment";
   var character=new GameObject("Review player");character.transform.position=new Vector3(1,.1f,-6);
   var capsule=character.AddComponent<CharacterController>();capsule.height=1.8f;capsule.center=new Vector3(0,.9f,0);capsule.radius=.33f;capsule.stepOffset=.35f;
   var player=character.AddComponent<PlayerController>();
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonVikingRealm/Prefabs/Characters/SM_Chr_Warrior_Male_01.prefab");var facing=new GameObject("Review facing").transform;facing.SetParent(character.transform,false);var actor=UnityEngine.Object.Instantiate(source,facing);actor.name="Review warrior";
   foreach(var r in actor.GetComponentsInChildren<Renderer>())fixture.RegisterLabRenderer(r,"actor");
   projected.ApplyCharacter(fixture,actor,Path.Combine(Application.dataPath,"../TestResults/artifacts/character-projection"));
   var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.enabled&&r.name=="SM_Chr_Warrior_Male_01 projected");
   var visual=actor.AddComponent<CharacterVisual>();var visualSettings=new SerializedObject(visual);visualSettings.FindProperty("animator").objectReferenceValue=actor.GetComponent<Animator>();visualSettings.FindProperty("bodyRenderer").objectReferenceValue=body;visualSettings.ApplyModifiedPropertiesWithoutUndo();actor.AddComponent<PrototypeHumanoidMotion>();
   var camGo=new GameObject("Review camera");camGo.SetActive(false);camGo.tag="MainCamera";var camera=camGo.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.3f,.35f);camera.nearClipPlane=.1f;camera.farClipPlane=200;camGo.AddComponent<AudioListener>();
   var additional=camera.GetUniversalAdditionalCameraData();additional.renderPostProcessing=true;additional.volumeLayerMask=1<<30;additional.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
   var view=camGo.AddComponent<PlayerCamera>();if(templateCamera!=null)EditorUtility.CopySerialized(templateCamera,view);
   var controls=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Topaz/Core/Input/TopazControls.inputactions");
   var movement=Save(UnityEngine.Object.Instantiate(controls),Content+"/ReviewControls.asset");
   var settings=new SerializedObject(player);settings.FindProperty("controls").objectReferenceValue=movement;settings.FindProperty("viewCamera").objectReferenceValue=camera;settings.FindProperty("visualRoot").objectReferenceValue=facing;
   settings.FindProperty("normalBodyTint").colorValue=Color.white;settings.FindProperty("dodgeBodyTint").colorValue=Color.white;settings.FindProperty("hitBodyTint").colorValue=Color.white;settings.ApplyModifiedPropertiesWithoutUndo();
   settings=new SerializedObject(view);settings.FindProperty("target").objectReferenceValue=player;settings.FindProperty("controls").objectReferenceValue=movement;settings.ApplyModifiedPropertiesWithoutUndo();
   var light=new GameObject("Review sun").AddComponent<Light>();light.type=LightType.Directional;light.shadows=LightShadows.Soft;light.shadowBias=.03f;light.shadowNormalBias=.3f;RenderSettings.sun=light;
   var volume=new GameObject("Review post-processing").AddComponent<Volume>();volume.gameObject.layer=30;volume.isGlobal=true;volume.priority=200;var profile=ScriptableObject.CreateInstance<VolumeProfile>();
#if SURFACE_CACHE
   profile.Add<SurfaceCacheGIVolumeOverride>(true).enabled.Override(false);
#endif
   profile.Add<ColorAdjustments>(true);profile.Add<Tonemapping>(true);profile.Add<Bloom>(true);profile.Add<DepthOfField>(true);volume.sharedProfile=Save(profile,Content+"/ReviewVolume.asset");foreach(var component in volume.sharedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
   var pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;pipeline.renderScale=1;
   var manager=new GameObject("Visual review").AddComponent<VisualLabReviewPlayer>();manager.player=player;manager.view=view;manager.sun=light;manager.volume=volume;manager.controls=movement;manager.pipeline=Save(pipeline,Content+"/ReviewPipeline.asset");manager.theme=AssetDatabase.LoadAssetAtPath<TopazUiTheme>("Assets/Topaz/UI/Themes/TopazUiTheme.asset");manager.font=TMP_Settings.defaultFontAsset;
   var eventObject=new GameObject("Review EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
   var renderers=root.GetComponentsInChildren<Renderer>().Concat(actor.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled&&r.gameObject.activeInHierarchy || r.enabled&&r.transform.IsChildOf(actor.transform)).Distinct().ToArray();
   manager.bindings=renderers.Select(r=>new VisualLabReviewPlayer.Binding{renderer=r,looks=new VisualLabReviewPlayer.MaterialSet[12]}).ToArray();
   stage="materials";var textures=new Dictionary<Texture,Texture>();var materials=new Dictionary<string,Material>();int textureCount=0,materialCount=0;
   for(int look=0;look<12;look++){
    fixture.ApplyLabMaterials(VisualLabLook.All[look],shader);
    for(int i=0;i<renderers.Length;i++){
     var saved=renderers[i].sharedMaterials.Select(m=>{
      var texture=m.GetTexture("_BaseMap");if(texture is Texture2D image&&AssetDatabase.GetAssetPath(texture)==""){
       if(!textures.TryGetValue(texture,out var asset)){
        string name=Content+"/Paint-"+(textureCount++)+".png";var pixels=image.EncodeToPNG();if(!File.Exists(name)||!File.ReadAllBytes(name).SequenceEqual(pixels)){File.WriteAllBytes(name,pixels);AssetDatabase.ImportAsset(name);var importer=(TextureImporter)AssetImporter.GetAtPath(name);importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.sRGBTexture=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}asset=AssetDatabase.LoadAssetAtPath<Texture2D>(name);textures.Add(texture,asset);
       }texture=asset;
      }
      string key=look+"|"+m.shader.name+"|"+AssetDatabase.GetAssetPath(texture)+"|"+m.GetColor("_BaseColor");
      if(!materials.TryGetValue(key,out var material)){material=new Material(m);material.SetTexture("_BaseMap",texture);material=Save(material,Content+"/Look-"+(materialCount++)+".mat");materials.Add(key,material);}return material;
     }).ToArray();manager.bindings[i].looks[look]=new VisualLabReviewPlayer.MaterialSet{materials=saved};
    }
   }
   foreach(var b in manager.bindings)b.renderer.sharedMaterials=b.looks[0].materials;
   stage="mesh assets";
   // Preserve local geometry in separate private mesh assets, including the projected skin.
   int meshCount=0;foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){if(AssetDatabase.GetAssetPath(filter.sharedMesh)=="")filter.sharedMesh=Save(UnityEngine.Object.Instantiate(filter.sharedMesh),Content+"/Mesh-"+(meshCount++)+".asset");}
   foreach(var r in actor.GetComponentsInChildren<SkinnedMeshRenderer>())if(r.enabled)r.sharedMesh=Save(UnityEngine.Object.Instantiate(r.sharedMesh),Content+"/Mesh-"+(meshCount++)+".asset");
   stage="colliders";var water=root.transform.Find("River");foreach(var collider in water.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(collider);
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.transform!=water)){var meshCollider=filter.GetComponent<MeshCollider>();if(meshCollider==null)meshCollider=filter.gameObject.AddComponent<MeshCollider>();meshCollider.sharedMesh=filter.sharedMesh;}
   var bridge=root.transform.Cast<Transform>().First(t=>t.name.Contains("Bridge"));var bridgeRenderers=bridge.GetComponentsInChildren<Renderer>();float deck=bridgeRenderers.Max(r=>r.bounds.max.y);bridge.position+=Vector3.up*(.35f-deck);
   Barrier(root.transform,"River north",new Vector3(-9,1,13),new Vector3(5,4,30));Barrier(root.transform,"River south",new Vector3(-9,1,-17),new Vector3(5,4,24));
   Barrier(root.transform,"West bound",new Vector3(-27,2,1),new Vector3(1,12,54));Barrier(root.transform,"East bound",new Vector3(27,2,1),new Vector3(1,12,54));Barrier(root.transform,"South bound",new Vector3(0,2,-24),new Vector3(54,12,1));Barrier(root.transform,"North bound",new Vector3(0,2,28),new Vector3(54,12,1));
   character.SetActive(true);camGo.SetActive(true);RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=VisualLabLook.All[0].Ambient*.7f;
   stage="save scene";if(!EditorSceneManager.SaveScene(scene,Content+"/VisualReview.unity"))throw new InvalidOperationException("Review scene save returned false");AssetDatabase.SaveAssets();
   return "Prepared playable review: "+manager.bindings.Length+" renderer bindings, "+materialCount+" materials, "+textureCount+" textures, "+meshCount+" meshes; "+Content+"/VisualReview.unity";
  }catch(Exception error){File.WriteAllText(Path.Combine(directory,"prepare-failure.txt"),stage+"\n"+error);throw;}finally{
   // The saved scene now owns persistent assets; discard only temporary authoring data.
   EditorSceneManager.CloseScene(scene,true);fixture.Dispose();projected.Dispose();SceneManager.SetActiveScene(original);
  }
 }
 static void Barrier(Transform parent,string name,Vector3 position,Vector3 size){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;var collider=go.AddComponent<BoxCollider>();collider.size=size;}
 static void EnableCollision(UnityEngine.Object asset){if(asset is Mesh){var settings=new SerializedObject(asset);settings.FindProperty("m_PreBakeTriangleCollisionMesh").boolValue=true;settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);}}
 static T Save<T>(T source,string path)where T:UnityEngine.Object{
  var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing!=null){EditorUtility.CopySerialized(source,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(source);EnableCollision(existing);return existing;}
  AssetDatabase.CreateAsset(source,path);EnableCollision(source);return source;
 }
}
