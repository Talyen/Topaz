using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class CharacterPoseCapture {
 static string Dir;
 [Serializable] public class Part{public string name;public float[] vertices,normals,uv;public int[] triangles;public string texture;}
 [Serializable] public class View{public string name;public Vector3 position,right,up,forward,center;public float scale;}
 [Serializable] public class Data{public List<Part> parts=new List<Part>();public List<Part> occluders=new List<Part>();public List<View> views=new List<View>();}
 public static string Main(string directory){
  Dir=Path.GetFullPath(directory);
  if(EditorApplication.isPlaying)throw new Exception("Stop play first");Directory.CreateDirectory(Dir);
  var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var old=QualitySettings.renderPipeline;var pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;QualitySettings.renderPipeline=pipeline;
  var rt=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var owned=new List<Material>();
  try{
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonVikingRealm/Prefabs/Characters/SM_Chr_Warrior_Male_01.prefab");var actor=UnityEngine.Object.Instantiate(prefab);var data=new Data();
   var animator=actor.GetComponent<Animator>();animator.Rebind();animator.Update(0);animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).localRotation*=Quaternion.Euler(0,0,-65);animator.GetBoneTransform(HumanBodyBones.RightUpperArm).localRotation*=Quaternion.Euler(0,0,65);animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation*=Quaternion.Euler(-40,0,25);animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).localRotation*=Quaternion.Euler(20,0,-25);
   var renderers=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   foreach(var t in actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
   foreach(var r in renderers){
    var mesh=new Mesh();r.BakeMesh(mesh);var vs=mesh.vertices.Select(r.transform.TransformPoint).ToArray();var ns=mesh.normals.Select(r.transform.TransformDirection).ToArray();var src=r.sharedMaterial;var tex=src.GetTexture("_Base_Texture");
    data.parts.Add(new Part{name=r.name,vertices=vs.SelectMany(v=>new[]{v.x,v.y,v.z}).ToArray(),normals=ns.SelectMany(v=>new[]{v.x,v.y,v.z}).ToArray(),uv=mesh.uv.SelectMany(v=>new[]{v.x,v.y}).ToArray(),triangles=mesh.triangles,texture=AssetDatabase.GetAssetPath(tex)});UnityEngine.Object.DestroyImmediate(mesh);
    var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetTexture("_BaseMap",tex);m.SetFloat("_Smoothness",.15f);r.sharedMaterial=m;owned.Add(m);
   }
   foreach(var r in actor.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy)){
    var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
    data.occluders.Add(new Part{name=r.name,vertices=mesh.vertices.Select(r.transform.TransformPoint).SelectMany(v=>new[]{v.x,v.y,v.z}).ToArray(),triangles=mesh.triangles});
   }
   var cam=new GameObject("Projection camera").AddComponent<Camera>();cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.5f,.5f,.5f);cam.orthographic=true;cam.orthographicSize=Mathf.Max(bounds.size.y,bounds.size.x)*.62f;cam.nearClipPlane=.01f;cam.farClipPlane=30;cam.targetTexture=rt;cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
   var sun=new GameObject("Key").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.8f;sun.cullingMask=1<<30;sun.transform.rotation=Quaternion.Euler(30,-25,0);RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.65f,.65f);RenderSettings.fog=false;
   string[] names={"pose-front","pose-back","pose-left","pose-right"};Vector3[] dirs={new Vector3(.2f,-.3f,1),new Vector3(-.2f,-.3f,-1),new Vector3(-1,.4f,.2f),new Vector3(1,-.5f,-.2f)};
   for(int i=0;i<dirs.Length;i++){cam.transform.position=bounds.center+dirs[i].normalized*6;cam.transform.LookAt(bounds.center);data.views.Add(new View{name=names[i],position=cam.transform.position,right=cam.transform.right,up=cam.transform.up,forward=cam.transform.forward,center=bounds.center,scale=cam.orthographicSize});for(int f=0;f<2;f++)RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});Save(rt,Path.Combine(Dir,names[i]+".png"));}
   File.WriteAllText(Path.Combine(Dir,"posed-source.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {parts=data.parts,occluders=data.occluders,views=data.views.Select(v=>new {name=v.name,position=new[]{v.position.x,v.position.y,v.position.z},right=new[]{v.right.x,v.right.y,v.right.z},up=new[]{v.up.x,v.up.y,v.up.z},forward=new[]{v.forward.x,v.forward.y,v.forward.z},center=new[]{v.center.x,v.center.y,v.center.z},scale=v.scale})}));
   return renderers.Length+" visible skinned parts; "+data.parts.Sum(p=>p.vertices.Length/3)+" vertices exported; four posed calibrated views captured";
  }finally{foreach(var m in owned)UnityEngine.Object.DestroyImmediate(m);QualitySettings.renderPipeline=old;UnityEngine.Object.DestroyImmediate(pipeline);EditorSceneManager.CloseScene(scene,true);rt.Release();UnityEngine.Object.DestroyImmediate(rt);SceneManager.SetActiveScene(original);}
 }
 static void Save(RenderTexture rt,string path){var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;}
}
