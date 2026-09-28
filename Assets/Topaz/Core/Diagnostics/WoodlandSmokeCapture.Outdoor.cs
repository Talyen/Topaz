using System;
using System.Collections;
using System.IO;
using System.Linq;
using Topaz.Gameplay;
using Topaz.Generation;
using Topaz.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        IEnumerator FantasyChecks(string directory,M0Report report)
        {
            M0SetConditions(12,WeatherSchedule.Clear);
            yield return M0Position(new Vector3(0,0,-3),Vector3.forward);
            var terrainMaterial=Terrain.activeTerrains[0].materialTemplate;
            var originalShader=terrainMaterial.shader;
            var sun=RenderSettings.sun;
            foreach(var mode in new[]{"default","natural-light","no-ao","no-post","instancing","no-shadows","stock-terrain","no-lens","balanced"})
            {
                var settings=new GraphicsPreferences();
                if(mode=="natural-light")settings.lightingStyle=0;
                if(mode=="no-ao")settings.ambientOcclusion=false;
                if(mode=="no-lens"){settings.focusMode=0;settings.motionBlur=0;settings.chromaticAberration=0;}
                if(mode=="balanced")settings.look=0;
                typeof(VisualLookController).GetField("settings",M0Fields).SetValue(m0Look,settings);
                typeof(VisualLookController).GetMethod("Apply",M0Fields).Invoke(m0Look,null);
                Camera.main.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=mode!="no-post";
                foreach(var terrain in Terrain.activeTerrains){terrain.drawInstanced=mode=="instancing";terrain.shadowCastingMode=mode=="no-shadows"?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;}
                terrainMaterial.shader=mode=="stock-terrain"?Shader.Find("Universal Render Pipeline/Terrain/Lit"):originalShader;
                yield return new WaitForSecondsRealtime(2);
                if(mode=="default"||mode=="balanced")
                {
                    var frames=new System.Collections.Generic.List<float>();
                    for(int i=0;i<120;i++){yield return null;frames.Add(Time.unscaledDeltaTime*1000);}
                    frames.Sort();report.routes.Add(new M0Route{name="stationary-quality-"+mode,frames=120,outcome="stationary native-resolution sample; no capture IO during sampling",p50Ms=frames[60],p95Ms=frames[114],p99Ms=frames[118],longestMs=frames[119]});
                }
                yield return M0Shot(directory,"check-"+mode,report);
            }
            terrainMaterial.shader=originalShader;
        }
        IEnumerator OutdoorViews(string directory,M0Report report)
        {
            var plan=m0Session.ActiveRegion.Wilderness;
            foreach(var site in plan.Discoveries.Take(3))
            {
                var route=plan.Routes.First(r=>r.DestinationId==site.Id);
                var end=route.Points[route.Points.Count-1];var from=route.Points[route.Points.Count-2];
                m0Hour=12;yield return M0Position(Vector3.Lerp(from,end,.45f),(end-from).normalized);
                yield return M0Shot(directory,"outdoor-"+site.Id,report);
                yield return M0RouteCapture("outdoor-approach-"+site.Id,Vector3.Lerp(from,end,.35f),Vector3.Lerp(from,end,.8f),directory,report);
            }
            var crossing=plan.Routes[1];int crossingIndex=crossing.Points.Count-4;
            yield return M0Position(crossing.Points[crossingIndex-1],(crossing.Points[crossingIndex+1]-crossing.Points[crossingIndex-1]).normalized);
            yield return M0Shot(directory,"water-day",report);
            m0Hour=24;yield return new WaitForSecondsRealtime(1);yield return M0Shot(directory,"water-night",report);
            m0Hour=12;
            foreach(var mode in new[]{"taa","smaa","stp-native","stp-85"})
            {
                m0Look.BeginWorldPresentation(m0Data.worldHours);
                var settings=new GraphicsPreferences{look=1,antiAliasing=mode=="smaa"?2:mode.StartsWith("stp")?4:3};
                typeof(VisualLookController).GetField("settings",M0Fields).SetValue(m0Look,settings);
                typeof(VisualLookController).GetMethod("Apply",M0Fields).Invoke(m0Look,null);
                var scale=Camera.main.GetComponent<UrpRenderScaleController>();scale.enabled=false;
                var pipeline=(UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                pipeline.renderScale=mode=="stp-85"?.85f:1;
                yield return M0RouteCapture("water-"+mode,crossing.Points[crossingIndex-1],crossing.Points[crossingIndex+1],directory,report);
                if(Environment.GetCommandLineArgs().Contains("--topaz-motion")){m0Look.BeginWorldPresentation(m0Data.worldHours);yield return OutdoorMotion(mode,crossing.Points[crossingIndex-1],crossing.Points[crossingIndex+1],directory);}
            }
            typeof(VisualLookController).GetField("settings",M0Fields).SetValue(m0Look,new GraphicsPreferences{look=1,antiAliasing=3});
            typeof(VisualLookController).GetMethod("Apply",M0Fields).Invoke(m0Look,null);
            var weather=FindAnyObjectByType<WeatherPresentation>();
            yield return M0Position(crossing.Points[crossingIndex-1],(crossing.Points[crossingIndex+1]-crossing.Points[crossingIndex-1]).normalized);
            m0Weather=WeatherSchedule.Rain;m0Session.SetEventWeatherOverride(m0Weather);weather.SetCondition(m0Weather,false);
            for(int i=0;i<4;i++){weather.Tick(10);yield return new WaitForSecondsRealtime(.5f);yield return M0Shot(directory,"rain-transition-"+i,report);}
            m0Weather=WeatherSchedule.Clear;m0Session.SetEventWeatherOverride(m0Weather);weather.SetCondition(m0Weather,false);
            for(int i=0;i<4;i++){weather.Tick(30);yield return new WaitForSecondsRealtime(.5f);yield return M0Shot(directory,"drying-"+i,report);}
            var profile=(UnityEngine.Rendering.VolumeProfile)typeof(VisualLookController).GetField("profile",M0Fields).GetValue(m0Look);
            if(profile.TryGet<Tonemapping>(out var tonemapping))
            {
                var original=tonemapping.mode.value;
                foreach(var tone in new[]{TonemappingMode.Neutral,TonemappingMode.ACES})
                {tonemapping.mode.Override(tone);yield return new WaitForSecondsRealtime(.5f);yield return M0Shot(directory,"tone-"+tone,report);}
                tonemapping.mode.Override(original);
            }
            File.WriteAllText(Path.Combine(directory,"outdoor-contracts.json"),JsonUtility.ToJson(plan.Settings,true));
        }
    }
}
