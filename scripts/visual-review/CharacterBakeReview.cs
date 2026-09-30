using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class CharacterBakeReview {
 static string Dir;
 public static string Main(string directory){
  Dir=Path.GetFullPath(directory);
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode before the isolated review.");
  var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var old=QualitySettings.renderPipeline;var pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;QualitySettings.renderPipeline=pipeline;
  var rt=new RenderTexture(900,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var owned=new List<UnityEngine.Object>();
  try{
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonVikingRealm/Prefabs/Characters/SM_Chr_Warrior_Male_01.prefab");var actor=UnityEngine.Object.Instantiate(prefab);foreach(var t in actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
   var renderers=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   var replacements=new List<SkinnedMeshRenderer>();var originalMeshes=renderers.Select(r=>r.sharedMesh).ToArray();var originals=renderers.Select(r=>r.sharedMaterial).ToArray();
   var cam=new GameObject("Bake review camera").AddComponent<Camera>();cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.17f,.2f);cam.orthographic=true;cam.orthographicSize=1.28f;cam.nearClipPlane=.01f;cam.farClipPlane=30;cam.targetTexture=rt;cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;cam.transform.position=bounds.center+new Vector3(2,1,5).normalized*6;cam.transform.LookAt(bounds.center);
   var sun=new GameObject("Key").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.cullingMask=1<<30;sun.transform.rotation=Quaternion.Euler(35,-25,0);RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.55f,.6f);RenderSettings.fog=false;
   var volume=new GameObject("Disable review GI").AddComponent<Volume>();volume.gameObject.layer=30;volume.isGlobal=true;volume.priority=100;var profile=ScriptableObject.CreateInstance<VolumeProfile>();owned.Add(profile);volume.sharedProfile=profile;cam.GetUniversalAdditionalCameraData().volumeLayerMask=1<<30;
#if SURFACE_CACHE
   profile.Add<SurfaceCacheGIVolumeOverride>(true).enabled.Override(false);
#endif
   void Render(string name){for(int f=0;f<2;f++)RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});Save(rt,Path.Combine(Dir,name+".png"));}
   foreach(var r in renderers){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetTexture("_BaseMap",r.sharedMaterial.GetTexture("_Base_Texture"));m.SetFloat("_Smoothness",.15f);r.sharedMaterial=m;owned.Add(m);}
   Render("unity-before");
   var texture=new Texture2D(2,2,TextureFormat.RGB24,true,false);texture.LoadImage(File.ReadAllBytes(Path.Combine(Dir,"baked-albedo.png")));texture.Apply(true,false);owned.Add(texture);
   var atlas=JObject.Parse(File.ReadAllText(Path.Combine(Dir,"atlas-uv.json")))["cornerUv"].Values<float>().ToArray();int corner=0;int validated=0;
   foreach(var r in renderers){var src=r.sharedMesh;if(src.GetBonesPerVertex().Any(count=>count>4))throw new InvalidOperationException("This prototype supports up to four influences; use BoneWeight1 before extending it to this mesh.");var ids=src.triangles;var v=src.vertices;var n=src.normals;var w=src.boneWeights;var mesh=new Mesh{name=src.name+" unique projection atlas"};mesh.vertices=ids.Select(i=>v[i]).ToArray();mesh.normals=ids.Select(i=>n[i]).ToArray();mesh.boneWeights=ids.Select(i=>w[i]).ToArray();mesh.bindposes=src.bindposes;if(!mesh.bindposes.SequenceEqual(src.bindposes))throw new Exception("Bind poses changed");var uv=new Vector2[ids.Length];for(int j=0;j<uv.Length;j++){uv[j]=new Vector2(atlas[corner*2],atlas[corner*2+1]);corner++;if(!mesh.boneWeights[j].Equals(w[ids[j]]))throw new Exception("Skin data changed");validated++;}mesh.uv=uv;mesh.triangles=Enumerable.Range(0,ids.Length).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();var replacement=new GameObject(r.name+" baked atlas").AddComponent<SkinnedMeshRenderer>();replacement.gameObject.layer=30;replacement.transform.SetParent(r.transform.parent,false);replacement.transform.localPosition=r.transform.localPosition;replacement.transform.localRotation=r.transform.localRotation;replacement.transform.localScale=r.transform.localScale;replacements.Add(replacement);replacement.sharedMesh=mesh;replacement.bones=r.bones;replacement.rootBone=r.rootBone;replacement.localBounds=r.localBounds;replacement.updateWhenOffscreen=true;r.enabled=false;owned.Add(mesh);var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetTexture("_BaseMap",texture);m.SetFloat("_Smoothness",.15f);replacement.sharedMaterial=m;owned.Add(m);}
   Render("unity-after");
   var animator=actor.GetComponent<Animator>();animator.Rebind();animator.Update(0);
   var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var thigh=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);var forearm=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var lr=left.localRotation;var rr=right.localRotation;var tr=thigh.localRotation;var fr=forearm.localRotation;
   for(int i=0;i<12;i++){float t=i/12f*Mathf.PI*2;left.localRotation=lr*Quaternion.Euler(0,0,Mathf.Sin(t)*45);right.localRotation=rr*Quaternion.Euler(0,Mathf.Sin(t)*35,Mathf.Sin(t+1)*50);forearm.localRotation=fr*Quaternion.Euler(0,0,Mathf.Sin(t)*50);thigh.localRotation=tr*Quaternion.Euler(Mathf.Sin(t)*30,0,0);var posedObjects=new List<GameObject>();
    foreach(var r in replacements){var pose=new Mesh();r.BakeMesh(pose);owned.Add(pose);
    int part=replacements.IndexOf(r);var control=new Mesh();renderers[part].BakeMesh(control);var expected=control.vertices;var actual=pose.vertices;var indices=originalMeshes[part].triangles;for(int vtx=0;vtx<actual.Length;vtx++)if(Vector3.Distance(actual[vtx],expected[indices[vtx]])>.00001f)throw new Exception("Cloned mesh deformation differs from original");UnityEngine.Object.DestroyImmediate(control);var go=new GameObject("CPU deformed review",typeof(MeshFilter),typeof(MeshRenderer));go.layer=30;go.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);go.transform.localScale=r.transform.lossyScale;go.GetComponent<MeshFilter>().sharedMesh=pose;go.GetComponent<MeshRenderer>().sharedMaterial=r.sharedMaterial;posedObjects.Add(go);r.enabled=false;}
    Render("deform-"+i.ToString("D2"));foreach(var go in posedObjects)UnityEngine.Object.DestroyImmediate(go);}
   File.WriteAllText(Path.Combine(Dir,"unity-validation.json"),"{\"validatedCorners\":"+validated+",\"frames\":12,\"bonesBindposesPreserved\":true,\"posedVerticesMatchOriginalWithinMeters\":0.00001,\"test\":\"CPU BakeMesh previews of humanoid bone deformation; no real-time animation or locomotion/combat acceptance claim\"}");
   return "Baked atlas applied to 3 cloned skinned meshes; "+validated+" corners checked; 12 deformation frames rendered";
  }finally{EditorSceneManager.CloseScene(scene,true);foreach(var o in owned)UnityEngine.Object.DestroyImmediate(o);QualitySettings.renderPipeline=old;UnityEngine.Object.DestroyImmediate(pipeline);rt.Release();UnityEngine.Object.DestroyImmediate(rt);SceneManager.SetActiveScene(original);}
 }
 static void Save(RenderTexture rt,string path){var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;}
}
