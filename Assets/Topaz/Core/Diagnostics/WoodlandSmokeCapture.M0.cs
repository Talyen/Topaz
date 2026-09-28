using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Topaz.Combat;
using Topaz.Gameplay;
using Topaz.Player;
using Topaz.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;

namespace Topaz
{
    // Opt-in diagnostic only. All world mutations use the smoke mode's temporary collection.
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class M0Report
        {
            public string status="starting", failure, capturedUtc, unity, platform, gpu, api, os, cpu;
            public string quality="High", aa="Native TAA", scene, settingsPolicy;
            public string timingNote="Capture IO and synthetic input are instrumented diagnostics, not a performance acceptance run.";
            public int seed, width, height, errors, ramMb, vSync;
            public SurfaceCacheLighting.Status globalIllumination;
            public float renderScale;
            public double refreshHz;
            public List<M0View> views=new List<M0View>();
            public List<M0Route> routes=new List<M0Route>();
        }
        [Serializable] sealed class M0View
        {
            public string file, weather;
            public double hour, captureRealtime;
            public Vector3 player, camera, cameraEuler;
            public float fieldOfView, zoom, rain, cloudiness, wetness, renderScale;
            public int aa;
            public Vector3 biome;
            public bool lantern, focused;
            public int loadedChunks, livingEnemies, attackingEnemies, playerHealth;
        }
        [Serializable] sealed class M0Route
        {
            public string name, outcome;
            public Vector3 start, target, end;
            public bool reached;
            public int frames, blockedFrames, startHealth, endHealth, maxAttackingEnemies;
            public float seconds, p50Ms, p95Ms, p99Ms, longestMs, minGroundClearance;
            public List<Vector3> sampledPositions=new List<Vector3>();
        }
        static readonly BindingFlags M0Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        WorldSession m0Session;
        VisualLookController m0Look;
        TopazSaveData m0Data;
        Gamepad m0Pad;
        PlayerCamera m0Camera;
        Vector3 m0Facing=Vector3.forward;
        InputSettings.BackgroundBehavior m0PreviousBackground;
        bool m0InputConfigured;
        double m0Hour=12;
        string m0Weather=WeatherSchedule.Clear;

        void LateUpdate()
        {
            // Automatic traversal has a repeatable heading; physical mouse/focus changes must not steer its camera.
            if(traversalSession!=null && traversalCamera!=null)
                traversalCamera.LookAtPoint(traversalSession.transform.position+Vector3.forward*10);
            if(traversalClock!=null && traversalSession!=null)
            {
                // Repeated performance circuits compare residency/pacing under the same light and weather.
                traversalClock.worldHours=traversalHour;
            }
            if(m0Session==null || m0Data==null || m0Look==null)return;
            // Fixed visual comparison time; does not run in normal play or ordinary smoke mode.
            // Midnight is hour 24, not elapsed hour zero (before the world's 08:00 start).
            m0Data.worldHours=WorldClock.HoursPerDay+m0Hour;
            m0Look.SetWorldHours(m0Data.worldHours);
            m0Session.GetComponent<PlayerLantern>()?.SetWorldHours(m0Data.worldHours);
            // Keep physical mouse movement from changing fixed review poses; Cinemachine stays active.
            m0Camera?.LookAtPoint(m0Session.transform.position+m0Facing*10);
        }

        IEnumerator ReviewM0(WorldSession session,string directory)
        {
            var pipeline=(UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var report=new M0Report {capturedUtc=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,
                platform=Application.platform.ToString(),gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),
                os=SystemInfo.operatingSystem,cpu=SystemInfo.processorType,ramMb=SystemInfo.systemMemorySize,
                seed=session.ActiveWorld.seed,width=Screen.width,height=Screen.height,scene=session.gameObject.scene.name,
                refreshHz=Screen.currentResolution.refreshRateRatio.value,vSync=QualitySettings.vSyncCount,renderScale=pipeline.renderScale,
                globalIllumination=FindAnyObjectByType<VisualLookController>().GlobalIllumination,
                settingsPolicy="Fresh in-memory GraphicsPreferences, High/native TAA; no visual preference file writes."};
            var stack=new Stack<IEnumerator>();stack.Push(M0Steps(session,directory,report));
            try
            {
                while(stack.Count>0)
                {
                    object current=null;bool more=false;Exception failure=null;
                    try {more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
                    catch(Exception error){failure=error;}
                    if(failure!=null){report.failure=failure.ToString();Debug.LogException(failure);break;}
                    if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                    if(current is IEnumerator nested)stack.Push(nested);else yield return current;
                }
            }
            finally
            {
                while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
                if(m0Pad!=null){InputSystem.RemoveDevice(m0Pad);m0Pad=null;}
                if(m0InputConfigured){InputSystem.settings.backgroundBehavior=m0PreviousBackground;m0InputConfigured=false;}
                if(m0Camera!=null){m0Camera.enabled=true;m0Camera=null;}
                m0Session=null;m0Data=null;m0Look=null;
                report.errors=runtimeErrors;
                report.status=report.failure==null&&runtimeErrors==0?"capture completed":"capture failed";
                File.WriteAllText(Path.Combine(directory,"m0.json"),JsonUtility.ToJson(report,true));
            }
            Debug.Log("[Topaz] M0 "+report.status+"; "+directory);
            if(Environment.GetCommandLineArgs().Contains("--topaz-smoke-quit"))Application.Quit();
        }

        IEnumerator M0Steps(WorldSession session,string directory,M0Report report)
        {
            m0Session=session;m0Data=(TopazSaveData)typeof(WorldSession).GetField("_data",M0Fields).GetValue(session);
            m0Look=FindAnyObjectByType<VisualLookController>();
            // Avoid setters that persist preferences into the owner's normal visual settings.
            typeof(VisualLookController).GetField("settings",M0Fields).SetValue(m0Look,new GraphicsPreferences {look=1,antiAliasing=3});
            typeof(VisualLookController).GetMethod("Apply",M0Fields).Invoke(m0Look,null);
            m0PreviousBackground=InputSystem.settings.backgroundBehavior;
            m0InputConfigured=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            m0Pad=InputSystem.AddDevice<Gamepad>();
            InputSystem.EnableDevice(m0Pad);
            m0Camera=Camera.main.GetComponent<PlayerCamera>();m0Camera.SetZoom(6.5f);
            typeof(PlayerCamera).GetField("pitch",M0Fields).SetValue(m0Camera,14f);
            m0Camera.enabled=false;
            if (Environment.GetCommandLineArgs().Contains("--topaz-look-check"))
            { yield return FantasyChecks(directory,report); yield break; }
            if (Environment.GetCommandLineArgs().Contains("--topaz-shelter"))
            { yield return ShelterSteps(session,directory,report); yield break; }
            foreach(var phase in new[]{("dawn",7d,WeatherSchedule.Clear),("day",12d,WeatherSchedule.Clear),("sunset",17d,WeatherSchedule.Clear),("dusk",19.5d,WeatherSchedule.Clear),
                ("night",0d,WeatherSchedule.Clear),("rain",12d,WeatherSchedule.Rain)})
            {
                m0Hour=phase.Item2;m0Weather=phase.Item3;
                session.SetEventWeatherOverride(m0Weather);
                FindAnyObjectByType<WeatherPresentation>().SetCondition(m0Weather,true);
                yield return M0Position(new Vector3(0,0,-3),Vector3.forward);
                session.GetComponent<PlayerLantern>().SetLit(false);
                yield return new WaitForSecondsRealtime(3);
                yield return M0Shot(directory,phase.Item1+"-home",report);
                if(phase.Item1=="night")
                {
                    session.GetComponent<PlayerLantern>().SetLit(true);
                    yield return new WaitForSecondsRealtime(1);
                    yield return M0Shot(directory,"night-home-lantern",report);
                    session.GetComponent<PlayerLantern>().SetLit(false);
                }
                yield return M0Position(new Vector3(118,0,20),Vector3.right);
                yield return new WaitForSecondsRealtime(2);
                yield return M0Shot(directory,phase.Item1+"-boundary",report);
            }
            if(Environment.GetCommandLineArgs().Contains("--topaz-lighting-review"))
            {
                var menu=FindAnyObjectByType<VisualOptionsMenu>();menu.Toggle();yield return null;
                yield return M0Shot(directory,"graphics-options",report);menu.Close();yield break;
            }
            m0Hour=12;m0Weather=WeatherSchedule.Clear;
            session.SetEventWeatherOverride(m0Weather);FindAnyObjectByType<WeatherPresentation>().SetCondition(m0Weather,true);
            yield return M0RouteCapture("boundary",new Vector3(118,0,20),new Vector3(142,0,20),directory,report);
            yield return M0RouteCapture("woodland",new Vector3(72,0,72),new Vector3(96,0,72),directory,report);

            yield return M0Position(new Vector3(0,0,-3),Vector3.forward);
            var pack=(InventorySlots)typeof(WorldSession).GetField("_backpack",M0Fields).GetValue(session);
            pack.Add(session.WoodItem,30);pack.Add(session.StoneItem,30);
            Vector3 chest=Build(session,BuildCatalog.Chest);
            yield return M0Position(chest+Vector3.back*5,Vector3.forward);
            yield return M0Shot(directory,"homestead-built-chest",report);
            yield return M0RouteCapture("homestead",new Vector3(8,0,-8),new Vector3(8,0,12),directory,report);

            if(Environment.GetCommandLineArgs().Contains("--topaz-outdoor"))yield return OutdoorViews(directory,report);
            var site=session.ActiveRegion.Wilderness.Discoveries.OrderBy(s=>s.X*s.X+s.Z*s.Z).First();
            var approach=new Vector3(site.X+12,0,site.Z-12);
            yield return M0Position(approach,Vector3.forward);
            yield return M0Shot(directory,"encounter-approach",report);
            yield return M0RouteCapture("encounter",approach,approach+Vector3.forward*9,directory,report,true);
            session.FlushCurrent();
        }

        IEnumerator M0Position(Vector3 point,Vector3 facing)
        {
            var stream=m0Session.ActiveRegion.Streaming;
            yield return stream.PrepareDestination(point);
            if(!stream.IsReadyAt(point))throw new InvalidOperationException("M0 destination not ready: "+point+" "+stream.Failure);
            point.y=Topaz.Generation.WoodlandRegion.GroundHeight(point);
            MovePlayer(m0Session,point);
            m0Facing=facing;
            m0Camera.LookAtPoint(point+facing*10);
            yield return new WaitForSecondsRealtime(1);
        }

        IEnumerator M0Shot(string directory,string name,M0Report report)
        {
            yield return new WaitForEndOfFrame();
            string file=name+".png";
            // Synchronous save ties the manifest pose to the actual captured frame.
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try {File.WriteAllBytes(Path.Combine(directory,file),texture.EncodeToPNG());}
            finally {Destroy(texture);}
            var weather=FindAnyObjectByType<WeatherPresentation>();var camera=Camera.main;
            var enemies=FindObjectsByType<EnemyCombatant>(FindObjectsSortMode.None);
            report.views.Add(new M0View {file=file,weather=m0Weather,hour=m0Hour,captureRealtime=Time.realtimeSinceStartupAsDouble,player=m0Session.transform.position,
                camera=camera.transform.position,cameraEuler=camera.transform.eulerAngles,fieldOfView=camera.fieldOfView,
                zoom=camera.GetComponent<PlayerCamera>().CurrentZoom,rain=weather.Rain,cloudiness=weather.Cloudiness,
                wetness=m0Look.State.Wetness,biome=m0Look.State.Biomes,aa=m0Look.CurrentAa,renderScale=((UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline).renderScale,
                lantern=m0Session.GetComponent<PlayerLantern>().IsOn,focused=Application.isFocused,
                loadedChunks=m0Session.ActiveRegion.Streaming.LoadedCount,livingEnemies=enemies.Count(e=>e.IsAlive),
                attackingEnemies=enemies.Count(e=>e.IsAlive&&e.IsAttacking),playerHealth=m0Session.GetComponent<PlayerVitality>().CurrentHealth});
        }

        IEnumerator M0RouteCapture(string name,Vector3 start,Vector3 target,string directory,M0Report report,bool combat=false)
        {
            yield return M0Position(start,(target-start).normalized);
            var health=m0Session.GetComponent<PlayerVitality>();
            var route=new M0Route {name=name,start=m0Session.transform.position,target=target,startHealth=health.CurrentHealth,minGroundClearance=float.MaxValue};
            report.routes.Add(route);
            var frames=new List<float>();float began=Time.realtimeSinceStartup,nextShot=began,timeout=combat?8:12;int shot=0;
            while(Time.realtimeSinceStartup-began<timeout)
            {
                Vector3 direction=target-m0Session.transform.position;direction.y=0;
                bool arrived=direction.magnitude<.6f;
                if(arrived&&!combat)break;
                var camera=Camera.main.transform;
                var forward=Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;
                var right=Vector3.ProjectOnPlane(camera.right,Vector3.up).normalized;
                Vector2 stick=arrived?Vector2.zero:new Vector2(Vector3.Dot(direction.normalized,right),Vector3.Dot(direction.normalized,forward));
                float elapsed=Time.realtimeSinceStartup-began;
                InputSystem.QueueStateEvent(m0Pad,new GamepadState {leftStick=stick,rightTrigger=combat&&elapsed%1f<.2f?1:0});
                yield return null;
                frames.Add(Time.unscaledDeltaTime*1000);route.frames++;
                if(m0Session.BlockMovement)route.blockedFrames++;
                route.minGroundClearance=Mathf.Min(route.minGroundClearance,m0Session.transform.position.y-Topaz.Generation.WoodlandRegion.GroundHeight(m0Session.transform.position));
                if(Time.realtimeSinceStartup>=nextShot)
                {
                    route.sampledPositions.Add(m0Session.transform.position);
                    route.maxAttackingEnemies=Math.Max(route.maxAttackingEnemies,FindObjectsByType<EnemyCombatant>(FindObjectsSortMode.None).Count(e=>e.IsAlive&&e.IsAttacking));
                    yield return M0Shot(directory,name+"-motion-"+(shot++).ToString("D3"),report);
                    nextShot=Time.realtimeSinceStartup+.25f;
                }
            }
            InputSystem.QueueStateEvent(m0Pad,new GamepadState());yield return null;
            route.end=m0Session.transform.position;route.seconds=Time.realtimeSinceStartup-began;route.endHealth=health.CurrentHealth;
            Vector3 remaining=target-route.end;remaining.y=0;route.reached=remaining.magnitude<.6f;
            route.outcome=route.reached?"destination reached":"timed observation ended before destination";
            frames.Sort();
            if(frames.Count>0)
            {
                float P(float q)=>frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count*q)-1,0,frames.Count-1)];
                route.p50Ms=P(.5f);route.p95Ms=P(.95f);route.p99Ms=P(.99f);route.longestMs=frames[frames.Count-1];
            }
            yield return M0Shot(directory,name+"-end",report);
        }
    }
}
