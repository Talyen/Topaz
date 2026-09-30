using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Topaz.Gameplay;
using Topaz.Rendering;
using Topaz.Player;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        WorldSession traversalSession;
        PlayerCamera traversalCamera;
        TopazSaveData traversalClock;
        double traversalHour;
        [Serializable] sealed class TraversalReport
        {
            public string graphics, api, status, route,quality,depthPriming,processor,operatingSystem,unity;
            public int systemMemoryMB,processorCount,vSyncCount,targetFrameRate;
            public double refreshHz;
            public bool developmentBuild;
            public SurfaceCacheLighting.Status gi;
            public int aa,depthOfField,shadowResolution,shadowCascades;
            public float shadowDistance,initialRenderScale,minimumRenderScale,maximumRenderScale,finalRenderScale,cloudiness,rain;
            public double worldHours,worldHoursEnd;
            public bool timeAndWeatherHeld;
            public List<TraversalCircuit> circuits=new List<TraversalCircuit>();
            public List<GpuPassTiming> gpuPasses=new List<GpuPassTiming>();
            public int seed, frames, blockedFrames, over16Ms, over20Ms, over33Ms, errors, loaded, directionChanges, width, height, gpuSamples, cpuSamples;
            public double meanGpuMs,meanCpuMs,meanMainThreadMs,meanRenderThreadMs;
            public bool focused = true, instrumentedMotionCapture, aoDownsample, gpuOcclusion;
            public float seconds, distance, p50, p95, p99, maximum, terrainCommitMs, preparationMs, navigationSubmitMs;
            public long allocatedMemory;
            public Vector3 start, end, cameraStart, cameraEnd, cameraEulerStart, cameraEulerEnd;
        }
        [Serializable] sealed class TraversalCircuit
        {
            public string phase;
            public int frames,over16Ms,over33Ms,loaded,peakLoaded,farCanopy,farTerrain,terrainPool,preloaded,ownedMeshes;
            public float seconds,p50,p95,p99,maximum;
            public long memory,peakMemory;
        }
        // Continuous real input, no screenshots, teleports or file IO inside the sample window.
        IEnumerator TraversalReview(WorldSession session, string directory)
        {
            var stream = session.ActiveRegion.Streaming;
            var route = stream.Plan.Routes[0].Points;
            var pad = InputSystem.AddDevice<Gamepad>();
            var frames = new List<float>(24000);
            var report = new TraversalReport { graphics = SystemInfo.graphicsDeviceName,
                api = SystemInfo.graphicsDeviceType.ToString(), seed = stream.Plan.Seed, start = session.transform.position,width=Screen.width,height=Screen.height };
            report.processor=SystemInfo.processorType;report.processorCount=SystemInfo.processorCount;
            report.systemMemoryMB=SystemInfo.systemMemorySize;report.operatingSystem=SystemInfo.operatingSystem;
            report.unity=Application.unityVersion;report.developmentBuild=Debug.isDebugBuild;
            report.vSyncCount=QualitySettings.vSyncCount;report.targetFrameRate=Application.targetFrameRate;
            report.refreshHz=Screen.currentResolution.refreshRateRatio.value;
            var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            if(Camera.main.GetUniversalAdditionalCameraData().scriptableRenderer is UniversalRenderer renderer)
            {
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-depth-priming")>=0)renderer.depthPrimingMode=DepthPrimingMode.Auto;
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-no-depth-priming")>=0)renderer.depthPrimingMode=DepthPrimingMode.Disabled;
                report.depthPriming=renderer.depthPrimingMode.ToString();
            }
            var look=FindAnyObjectByType<VisualLookController>();var weather=FindAnyObjectByType<WeatherPresentation>();
            var ao=typeof(VisualLookController).GetField("ambientOcclusionFeature",M0Fields).GetValue(look);
            var aoSettings=ao.GetType().GetField("m_Settings",M0Fields).GetValue(ao);
            var downsample=aoSettings.GetType().GetField("Downsample",M0Fields);bool priorDownsample=(bool)downsample.GetValue(aoSettings);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-half-ao")>=0)downsample.SetValue(aoSettings,true);
            report.aoDownsample=(bool)downsample.GetValue(aoSettings);
            report.gpuOcclusion=pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras;
            report.quality=look.HighQuality?"High":"Balanced";report.gi=look.GlobalIllumination;report.aa=look.CurrentAa;report.depthOfField=look.CurrentDepthMode;
            report.shadowResolution=pipeline.mainLightShadowmapResolution;report.shadowCascades=pipeline.shadowCascadeCount;report.shadowDistance=pipeline.shadowDistance;
            report.worldHours=session.WorldHours;report.cloudiness=weather.Cloudiness;report.rain=weather.Rain;
            report.initialRenderScale=report.minimumRenderScale=report.maximumRenderScale=pipeline.renderScale;
            var adapter=Camera.main.GetComponent<PlayerCamera>();bool adapterEnabled=adapter.enabled;
            float oldZoom=adapter.CurrentZoom;
            traversalSession=session;traversalCamera=adapter;adapter.SetZoom(30);
            adapter.LookAtPoint(session.transform.position+Vector3.forward*10);
            yield return null;yield return null;
            report.cameraStart=Camera.main.transform.position;report.cameraEulerStart=Camera.main.transform.eulerAngles;
            var timings=new FrameTiming[1];
            using var gpuProbe=new GpuPassProbe(Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-gpu-passes")>=0);
            float duration = 180;
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith("--topaz-traversal-seconds=") && float.TryParse(arg.Substring(26), out float value)) duration = Mathf.Clamp(value, 10, 1800);
            int requestedCircuits=0;
            foreach(string arg in Environment.GetCommandLineArgs())
                if(arg.StartsWith("--topaz-traversal-circuits=") && int.TryParse(arg.Substring(27),out int count))requestedCircuits=Mathf.Clamp(count,1,4);
            if(requestedCircuits>0)
            {
                traversalClock=(TopazSaveData)typeof(WorldSession).GetField("_data",M0Fields).GetValue(session);
                traversalHour=session.WorldHours;report.worldHours=traversalHour;report.timeAndWeatherHeld=true;
                session.SetEventWeatherOverride("clear");weather.SetCondition("clear",true);
                report.cloudiness=weather.Cloudiness;report.rain=weather.Rain;
                bool explicitDuration=false;foreach(string arg in Environment.GetCommandLineArgs())explicitDuration|=arg.StartsWith("--topaz-traversal-seconds=");
                if(!explicitDuration)duration=1800;
                // Use a bounded route approach for repeated streaming circuits, before its encounter center.
                var circuit=new List<Vector3>{route[0]};float length=0;
                for(int i=1;i<route.Count;i++)
                {
                    float segment=Vector3.Distance(route[i-1],route[i]);
                    if(length+segment>240){circuit.Add(Vector3.Lerp(route[i-1],route[i],(240-length)/segment));break;}
                    circuit.Add(route[i]);length+=segment;
                }
                route=circuit;
            }
            report.route=stream.Plan.Routes[0].Id+(requestedCircuits>0?" (first 240 m, out and back)":" (out and back)");
            int target = 1, direction = 1;
            var circuitFrames=new List<float>(16000);
            float circuitStart=Time.realtimeSinceStartup;
            long circuitPeakMemory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            int circuitPeakLoaded=stream.LoadedCount;
            float Quantile(List<float> values,float q)=>values[Mathf.Clamp(Mathf.CeilToInt(values.Count*q)-1,0,values.Count-1)];
            void FinishCircuit()
            {
                if(circuitFrames.Count==0)return;
                int over16=0,over33=0;foreach(float ms in circuitFrames){if(ms>16.667f)over16++;if(ms>33.333f)over33++;}
                circuitFrames.Sort();report.circuits.Add(new TraversalCircuit{phase=report.circuits.Count==0?"cold":"warm",frames=circuitFrames.Count,
                    seconds=Time.realtimeSinceStartup-circuitStart,p50=Quantile(circuitFrames,.5f),p95=Quantile(circuitFrames,.95f),p99=Quantile(circuitFrames,.99f),maximum=circuitFrames[circuitFrames.Count-1],memory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),peakMemory=circuitPeakMemory,
                    over16Ms=over16,over33Ms=over33,loaded=stream.LoadedCount,peakLoaded=circuitPeakLoaded,farCanopy=stream.DistantCount,
                    farTerrain=stream.DistantTerrainCount,terrainPool=stream.PooledTerrainCount,preloaded=stream.PreloadedCount,ownedMeshes=stream.OwnedMeshCount});
                circuitFrames.Clear();circuitStart=Time.realtimeSinceStartup;
                circuitPeakMemory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();circuitPeakLoaded=stream.LoadedCount;
            }
            var previous = session.transform.position;
            float began = Time.realtimeSinceStartup;
            bool capture=Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-traversal-motion")>=0;
            Coroutine motion=null;
            var previousBackground=InputSystem.settings.backgroundBehavior;
            try
            {
                // Synthetic review input must keep running when the owner uses another app,
                // as in M0. The focused flag still disqualifies background pacing evidence.
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.EnableDevice(pad);
                while (Time.realtimeSinceStartup - began < duration)
                {
                    if(capture && motion==null && Time.realtimeSinceStartup-began>3)
                    {report.instrumentedMotionCapture=true;motion=StartCoroutine(CaptureMotionFrames("traversal",directory,session,6));}
                    var delta = route[target] - session.transform.position; delta.y = 0;
                    if (delta.magnitude < 1.2f)
                    {
                        target += direction;
                        if (target >= route.Count || target < 1)
                        {
                            if(target<1){FinishCircuit();if(requestedCircuits>0 && report.circuits.Count>=requestedCircuits)break;}
                            direction = -direction; target = Mathf.Clamp(target, 1, route.Count - 1); report.directionChanges++;
                        }
                        delta = route[target] - session.transform.position; delta.y = 0;
                    }
                    var camera = Camera.main.transform;
                    var forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized;
                    var right = Vector3.ProjectOnPlane(camera.right, Vector3.up).normalized;
                    InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(Vector3.Dot(delta.normalized, right), Vector3.Dot(delta.normalized, forward)) });
                    FrameTimingManager.CaptureFrameTimings();
                    yield return null;
                    gpuProbe.Sample();
                    report.minimumRenderScale=Mathf.Min(report.minimumRenderScale,pipeline.renderScale);
                    report.maximumRenderScale=Mathf.Max(report.maximumRenderScale,pipeline.renderScale);
                    if(FrameTimingManager.GetLatestTimings(1,timings)>0)
                    {
                        var t=timings[0];
                        if(t.gpuFrameTime>0){report.gpuSamples++;report.meanGpuMs+=t.gpuFrameTime;}
                        if(t.cpuFrameTime>0){report.cpuSamples++;report.meanCpuMs+=t.cpuFrameTime;report.meanMainThreadMs+=t.cpuMainThreadFrameTime;report.meanRenderThreadMs+=t.cpuRenderThreadFrameTime;}
                    }
                    float ms = Time.unscaledDeltaTime * 1000;
                    frames.Add(ms);circuitFrames.Add(ms); report.frames++;
                    if(ms>16.667f)report.over16Ms++;
                    circuitPeakLoaded=Math.Max(circuitPeakLoaded,stream.LoadedCount);
                    // Sparse allocation sampling avoids turning the pacing run into a profiler capture.
                    if(report.frames%60==0)circuitPeakMemory=Math.Max(circuitPeakMemory,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
                    if (ms > 20) report.over20Ms++;
                    if (ms > 33.333f) report.over33Ms++;
                    if (!stream.CanMoveTo(session.transform.position + delta.normalized * .6f, .5f)) report.blockedFrames++;
                    report.focused &= Application.isFocused;
                    if(requestedCircuits>0 && !report.focused)break;
                    report.distance += Vector3.Distance(previous, session.transform.position); previous = session.transform.position;
                }
            if(motion!=null)yield return motion;
            frames.Sort();
            float Percentile(float q) => frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count * q) - 1, 0, frames.Count - 1)];
            if(report.gpuSamples>0)report.meanGpuMs/=report.gpuSamples;
            if(report.cpuSamples>0){report.meanCpuMs/=report.cpuSamples;report.meanMainThreadMs/=report.cpuSamples;report.meanRenderThreadMs/=report.cpuSamples;}
            report.seconds = Time.realtimeSinceStartup - began;
            report.worldHoursEnd=session.WorldHours;
            report.gpuPasses=gpuProbe.Results();report.finalRenderScale=pipeline.renderScale;
            report.p50 = Percentile(.5f); report.p95 = Percentile(.95f); report.p99 = Percentile(.99f); report.maximum = frames[frames.Count - 1];
            report.terrainCommitMs = stream.PeakTerrainCommitMs; report.preparationMs = stream.PeakPreparationSliceMs; report.navigationSubmitMs = stream.PeakNavigationSubmitMs;
            report.loaded = stream.LoadedCount; report.end = session.transform.position; report.errors = runtimeErrors;
            report.allocatedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            report.status = !report.focused ? "focus lost" : requestedCircuits>0 && report.circuits.Count<requestedCircuits ? "incomplete circuits" : runtimeErrors > 0 ? "runtime errors" : report.blockedFrames > 0 ? "readiness stops" : report.distance < report.seconds * 2 ? "insufficient traversal" : "completed";
            InputSystem.QueueStateEvent(pad,new GamepadState());
            yield return new WaitForEndOfFrame();
            report.cameraEnd=Camera.main.transform.position;report.cameraEulerEnd=Camera.main.transform.eulerAngles;
            var shot=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(Path.Combine(directory,"traversal-end.png"),shot.EncodeToPNG());}
            finally{Destroy(shot);}
            File.WriteAllText(Path.Combine(directory, "traversal.json"), JsonUtility.ToJson(report, true));
            }
            finally
            {
                downsample.SetValue(aoSettings,priorDownsample);InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior=previousBackground;
                traversalCamera=null;traversalSession=null;traversalClock=null;
                if(adapter!=null){adapter.SetZoom(oldZoom);adapter.enabled=adapterEnabled;}
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--topaz-smoke-quit") >= 0) Application.Quit();
        }
    }
}
