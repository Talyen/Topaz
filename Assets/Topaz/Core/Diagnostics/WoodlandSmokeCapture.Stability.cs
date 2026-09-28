using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Topaz.Gameplay;
using Topaz.Player;
using Topaz.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class StabilityResult { public string mode; public float pathMeanDifference,pathMaximumDifference; public int pairs; }
        [Serializable] sealed class StabilityReport { public int seed,errors; public bool moving; public string note="Frozen scene, normal player renders; capture IO means this is not a performance benchmark."; public List<StabilityResult> results=new List<StabilityResult>(); }
        IEnumerator TerrainStability(WorldSession session,string directory)
        {
            var stream=session.ActiveRegion.Streaming;var route=stream.Plan.Routes[1].Points;
            var point=route[route.Count-3];var facing=(route[route.Count-1]-point).normalized;
            yield return stream.PrepareDestination(point);MovePlayer(session,new Vector3(point.x,stream.Plan.Height(point.x,point.z),point.z));
            m0Session=session;m0Data=(TopazSaveData)typeof(WorldSession).GetField("_data",M0Fields).GetValue(session);
            m0Look=FindAnyObjectByType<VisualLookController>();m0Camera=Camera.main.GetComponent<PlayerCamera>();m0Facing=facing;m0Hour=12;
            m0Camera.enabled=false;m0Camera.LookAtPoint(session.transform.position+facing*20);
            session.SetEventWeatherOverride("clear");FindAnyObjectByType<WeatherPresentation>().SetCondition("clear",true);
            for(int n=0;n<32;n++)yield return null;
            float previousTime=Time.timeScale;Time.timeScale=0;
            var full=new RenderTexture(Screen.width,Screen.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var small=new RenderTexture(640,360,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            full.Create();small.Create();var texture=new Texture2D(640,360,TextureFormat.RGB24,false);
            var profile=(VolumeProfile)typeof(VisualLookController).GetField("profile",M0Fields).GetValue(m0Look);
            var normals=new Dictionary<TerrainLayer,float>();foreach(var terrain in Terrain.activeTerrains)foreach(var layer in terrain.terrainData.terrainLayers)normals[layer]=layer.normalScale;
            bool moving=Environment.GetCommandLineArgs().Contains("--topaz-stability-moving");
            var camera=Camera.main;var adapter=m0Camera;
            var brain=camera.GetComponent<Unity.Cinemachine.CinemachineBrain>();bool brainEnabled=brain!=null&&brain.enabled;
            Vector3 cameraStart=camera.transform.position;Quaternion rotationStart=camera.transform.rotation;
            if(moving){if(brain!=null)brain.enabled=false;m0Camera=null;}
            var report=new StabilityReport{seed=stream.Plan.Seed,moving=moving};
            if(moving)report.note="Identical camera translation and yaw per frame, frozen world, normal player renders. Adjacent-frame pixel differences include camera motion; inspect matched sequences, not this metric, for flicker. Capture IO excludes performance conclusions.";
            try
            {
                foreach(string mode in new[]{"baseline","no-gi","no-shadows","smaa","flat-normals"})
                {
                    m0Look.ResetSelection();typeof(VisualLookController).GetField("noSunShadows",M0Fields).SetValue(m0Look,mode=="no-shadows");
                    if(mode=="smaa")m0Look.SetAa(2);
                    foreach(var entry in normals)entry.Key.normalScale=mode=="flat-normals"?0:entry.Value;
#if SURFACE_CACHE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
                    if(profile.TryGet<SurfaceCacheGIVolumeOverride>(out var gi))gi.enabled.Override(mode!="no-gi");
#endif
                    camera.transform.SetPositionAndRotation(cameraStart,rotationStart);
                    camera.GetUniversalAdditionalCameraData().resetHistory=true;
                    for(int n=0;n<64;n++)yield return null;
                    Color32[] prior=null;var result=new StabilityResult{mode=mode};
                    for(int frame=0;frame<(moving?48:8);frame++)
                    {
                        if(moving)
                        {
                            camera.transform.SetPositionAndRotation(cameraStart+rotationStart*Vector3.right*(frame*.18f),
                                Quaternion.AngleAxis(frame*.12f,Vector3.up)*rotationStart);
                        }
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshotIntoRenderTexture(full);Graphics.Blit(full,small);
                        var previous=RenderTexture.active;RenderTexture.active=small;
                        texture.ReadPixels(new Rect(0,0,640,360),0,0);texture.Apply();RenderTexture.active=previous;
                        var colors=texture.GetPixels32();
                        if(moving)
                        {
                            var original=RenderTexture.active;RenderTexture.active=full;
                            var native=new Texture2D(full.width,full.height,TextureFormat.RGB24,false);
                            native.ReadPixels(new Rect(0,0,full.width,full.height),0,0);native.Apply();RenderTexture.active=original;
                            File.WriteAllBytes(Path.Combine(directory,mode+"-native-"+frame.ToString("D2")+".png"),native.EncodeToPNG());Destroy(native);
                        }
                        File.WriteAllBytes(Path.Combine(directory,mode+"-"+frame.ToString("D2")+".png"),texture.EncodeToPNG());
                        if(prior!=null)
                        {
                            float sum=0,max=0;int pixels=0;
                            // Dirt below the player's feet; frozen sun/wind/animation excludes intentional moving shadows.
                            for(int y=28;y<68;y++)for(int x=265;x<330;x++)
                            {
                                int i=y*640+x;float change=(Mathf.Abs(colors[i].r-prior[i].r)+Mathf.Abs(colors[i].g-prior[i].g)+Mathf.Abs(colors[i].b-prior[i].b))/3f;
                                sum+=change;max=Mathf.Max(max,change);pixels++;
                            }
                            result.pathMeanDifference+=sum/pixels;result.pathMaximumDifference=Mathf.Max(result.pathMaximumDifference,max);result.pairs++;
                        }
                        prior=colors;
                    }
                    result.pathMeanDifference/=Mathf.Max(1,result.pairs);report.results.Add(result);
                }
            }
            finally
            {
                report.errors=runtimeErrors;File.WriteAllText(Path.Combine(directory,"stability.json"),JsonUtility.ToJson(report,true));
                foreach(var entry in normals)if(entry.Key!=null)entry.Key.normalScale=entry.Value;
                typeof(VisualLookController).GetField("noSunShadows",M0Fields).SetValue(m0Look,false);m0Look.ResetSelection();
                if(brain!=null)brain.enabled=brainEnabled;m0Camera=adapter;Time.timeScale=previousTime;m0Camera.enabled=true;m0Camera=null;m0Session=null;m0Data=null;m0Look=null;
                full.Release();small.Release();Destroy(full);Destroy(small);Destroy(texture);
            }
        }
    }
}
