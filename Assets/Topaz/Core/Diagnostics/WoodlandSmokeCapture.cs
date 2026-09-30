using System;
using System.Collections;
using System.IO;
using System.Linq;
using Topaz.Generation;
using Topaz.Gameplay;
using UnityEngine;
namespace Topaz
{
    /// <summary>Opt-in standalone review capture. Uses a fresh temporary profile and never runs in normal play.</summary>
    public sealed partial class WoodlandSmokeCapture : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string unity, platform, graphics, pipeline, status, lightState, mainLight, keywords, graphicsSettingsPath, preparationStage, failure;
            public Topaz.Rendering.SurfaceCacheLighting.Status globalIllumination;
            public double captureWorldHours;
            public int seed, frames, enemies, width, height, errors;
            public double refreshHz;
            public bool focused, gpuOcclusion;
            public double averageGpuMs;
            public int gpuSamples;
            public double homeGenerationMs, expeditionGenerationMs, averageFrameMs, longestFrameMs;
            public long allocatedBytes;
            public bool gathered, cooked, homeBuilt, woodlandBuilt, campProtected, campTravel, reloadPreserved;
        }
        int runtimeErrors;
        void Awake()=>Application.logMessageReceived+=ObserveLog;
        void OnDestroy(){StopAlpineRenderLoop();Application.logMessageReceived-=ObserveLog;}
        void ObserveLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)runtimeErrors++;}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(!System.Environment.GetCommandLineArgs().Contains("--topaz-smoke"))return;
            if(FindAnyObjectByType<WoodlandSmokeCapture>()!=null)return;
            var go=new GameObject("Standalone Smoke Capture");DontDestroyOnLoad(go);go.AddComponent<WoodlandSmokeCapture>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true; // Automated temporary-profile captures must continue while the owner uses other apps.
            string directory=Path.Combine(Application.persistentDataPath,"Diagnostics");
            foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.StartsWith("--topaz-capture-dir="))directory=arg.Substring(20);
            Directory.CreateDirectory(directory);
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-alpine-review") || System.Environment.GetCommandLineArgs().Contains("--topaz-isometric-review") || System.Environment.GetCommandLineArgs().Contains("--topaz-area-review"))StartAlpineRenderLoop();
            var report=new Report{unity=Application.unityVersion,platform=Application.platform.ToString(),graphics=SystemInfo.graphicsDeviceName,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name,status="starting"};
            WorldSession session=null;float deadline=Time.realtimeSinceStartup+180;
            while(Time.realtimeSinceStartup<deadline)
            {
                session=FindAnyObjectByType<WorldSession>();if(session!=null&&session.HasActivePair&&session.ActiveRegion.Streaming.InitialReady)break;yield return null;
            }
            if(session==null||!session.HasActivePair||!session.ActiveRegion.Streaming.InitialReady){
                report.status="world creation failed";report.errors=runtimeErrors;
                report.preparationStage=session?.ActiveRegion?.Streaming?.PreparationStage;report.failure=session?.ActiveRegion?.Streaming?.Failure;
                File.WriteAllText(Path.Combine(directory,"smoke.json"),JsonUtility.ToJson(report,true));
                if(System.Environment.GetCommandLineArgs().Contains("--topaz-smoke-quit"))Application.Quit(1);
                yield break;
            }
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-area-review"))
            {yield return CaptureAreaJourney(session,directory);yield break;}
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-isometric-review"))
            {yield return IsometricReview(session,directory);yield break;}
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-stability"))
            {yield return TerrainStability(session,directory);if(System.Environment.GetCommandLineArgs().Contains("--topaz-smoke-quit"))Application.Quit();yield break;}
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-alpine-review"))
            {yield return CaptureAlpineViews(session,directory);if(System.Environment.GetCommandLineArgs().Contains("--topaz-smoke-quit"))Application.Quit();yield break;}
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-traversal"))
            {yield return TraversalReview(session,directory);yield break;}
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-hardening"))
            {yield return HardeningReview(session,directory,report);yield break;}
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-m0"))
            {
                yield return ReviewM0(session,directory);
                yield break;
            }
            report.width=Screen.width;report.height=Screen.height;report.refreshHz=Screen.currentResolution.refreshRateRatio.value;
            report.seed=session.ActiveWorld.seed;report.homeGenerationMs=FindObjectsByType<WoodlandRegion>().First(r=>r.regionId==TopazSaveData.WildernessRegion).GenerationMilliseconds;
            yield return new WaitForSecondsRealtime(8);
            report.globalIllumination=FindAnyObjectByType<Topaz.Rendering.VisualLookController>().GlobalIllumination;
            report.captureWorldHours=session.WorldHours;
            report.graphicsSettingsPath=FindAnyObjectByType<Topaz.Rendering.VisualLookController>().SettingsPath;
            report.lightState=string.Join(";",FindObjectsByType<Light>().Where(l=>l.type==LightType.Directional).Select(l=>l.name+":"+l.enabled+"/"+l.intensity+"/"+l.transform.eulerAngles+"/"+l.bakingOutput.isBaked));
            report.mainLight=Shader.GetGlobalColor("_MainLightColor").ToString()+" position "+Shader.GetGlobalVector("_MainLightPosition");
            report.keywords=string.Join(",",Shader.enabledGlobalKeywords.Select(k=>k.name));
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"home.png"));yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(1);
            report.gpuOcclusion=((UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline).gpuResidentDrawerEnableOcclusionCullingInCameras;
            var timing=new FrameTiming[1];
            report.focused=true;
            for(int i=0;i<180;i++)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){report.averageGpuMs+=timing[0].gpuFrameTime;report.gpuSamples++;}
                double ms=Time.unscaledDeltaTime*1000;report.averageFrameMs+=ms;report.longestFrameMs=Math.Max(report.longestFrameMs,ms);report.frames++;report.focused&=Application.isFocused;
            }
            report.averageFrameMs/=report.frames;
            if(report.gpuSamples>0)report.averageGpuMs/=report.gpuSamples;
            if (System.Environment.GetCommandLineArgs().Contains("--topaz-baseline-smoke"))
                yield return BaselineReview(session, directory, report);
            session.SetEventWeatherOverride("rain");
            yield return new WaitForSecondsRealtime(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"rain.png"));yield return new WaitForEndOfFrame();
            session.SetEventWeatherOverride(null);
            var site=session.ActiveRegion.Wilderness.Discoveries.OrderBy(s=>s.X*s.X+s.Z*s.Z).First();
            var destination=new Vector3(site.X,session.ActiveRegion.Wilderness.Height(site.X,site.Z),site.Z);
            yield return session.ActiveRegion.Streaming.PrepareDestination(destination);
            MovePlayer(session,destination);
            Camera.main.GetComponent<Topaz.Player.PlayerCamera>().LookAtPoint(destination+Vector3.forward*10);
            yield return new WaitForSecondsRealtime(12);
            report.enemies=FindObjectsByType<Topaz.Combat.EnemyCombatant>().Count(e=>e.enabled&&e.IsAlive&&e.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"expedition.png"));yield return new WaitForEndOfFrame();
            report.status=report.enemies>0?"passed":"enemies not activated";
            session.FlushCurrent();
            if (System.Environment.GetCommandLineArgs().Contains("--topaz-baseline-smoke") &&
                !(report.gathered && report.cooked && report.homeBuilt && report.woodlandBuilt && report.campProtected && report.campTravel && report.reloadPreserved))
                report.status="baseline scenario failed";
            report.errors=runtimeErrors;
            if(runtimeErrors>0)report.status="runtime errors";
            report.allocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            File.WriteAllText(Path.Combine(directory,"smoke.json"),JsonUtility.ToJson(report,true));
            Debug.Log("[Topaz] Standalone smoke: "+report.status+"; "+directory);
            if(System.Environment.GetCommandLineArgs().Contains("--topaz-smoke-quit")){yield return new WaitForSecondsRealtime(1);Application.Quit();}
        }

        static void MovePlayer(WorldSession session, Vector3 position)
        {
            var controller=session.GetComponent<CharacterController>();controller.enabled=false;
            session.transform.position=position+Vector3.up*.1f;controller.enabled=true;
            Physics.SyncTransforms();
        }
        static Vector3 Build(WorldSession session, string id)
        {
            var builder=FindAnyObjectByType<RegionBuildings>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var valid=typeof(RegionBuildings).GetMethod("CanPlace",flags);
            var height=typeof(RegionBuildings).GetMethod("PlacementHeight",flags);
            var origin=session.transform.position;
            if(!builder.BeginPlacement(id))throw new InvalidOperationException("Could not begin "+id);
            for(float z=-18;z<=42;z+=1.5f)for(float x=15;x<=42;x+=1.5f)
            {
                var p=origin+new Vector3(x,0,z);p.y=(float)height.Invoke(builder,new object[]{p,id});
                if(!(bool)valid.Invoke(builder,new object[]{id,p,0,null}))continue;
                builder.Cancel();
                if(!builder.TryPlaceAt(id,p))throw new InvalidOperationException("Could not commit "+id);
                return p;
            }
            builder.Cancel();throw new InvalidOperationException("No valid site for "+id);
        }
        IEnumerator BaselineReview(WorldSession session,string directory,Report report)
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            // Diagnostic supplies exist only in the temporary smoke profile.
            var pack=(InventorySlots)typeof(WorldSession).GetField("_backpack",flags).GetValue(session);
            var tree=FindObjectsByType<HarvestTree>(FindObjectsSortMode.None).First(t=>t.IsAvailable);
            MovePlayer(session,tree.transform.position+Vector3.back*1.3f);
            for(int i=0;i<8&&tree.IsAvailable;i++)tree.TryChop(session.transform.position,Vector3.forward,2.1f,90f);
            report.gathered=!tree.IsAvailable;
            foreach(var pickup in FindObjectsByType<WorldPickup>(FindObjectsSortMode.None))session.TryCollect(pickup);
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"gathering.png"));yield return new WaitForEndOfFrame();
            pack.Add(session.WoodItem,30);pack.Add(session.StoneItem,30);
            pack.Add(Resources.Load<ItemDefinition>("Mushrooms"),1);
            var homeFire=FindObjectsByType<Campfire>(FindObjectsSortMode.None).First(f=>f.StableId==Campfire.HomeId);
            MovePlayer(session,homeFire.transform.position+Vector3.forward);
            report.cooked=session.TryCookStew();
            Vector3 home=Build(session,BuildCatalog.Chest);report.homeBuilt=true;
            MovePlayer(session,home+new Vector3(0,0,-3));
            Camera.main.GetComponent<Topaz.Player.PlayerCamera>().LookAtPoint(home+Vector3.up*.5f);
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"home-building.png"));yield return new WaitForEndOfFrame();
            var journal=FindAnyObjectByType<BuildingJournalView>();journal.Show();yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"build-journal.png"));yield return new WaitForEndOfFrame();journal.Hide();
            var away=new Vector3(160,session.ActiveRegion.Wilderness.Height(160,20),20);
            yield return session.ActiveRegion.Streaming.PrepareDestination(away);
            MovePlayer(session,away);
            yield return new WaitForSecondsRealtime(8);
            Vector3 camp=Build(session,BuildCatalog.Camp);report.woodlandBuilt=true;
            MovePlayer(session,camp+Vector3.forward);yield return null;
            string campId=session.ReturnCampfireId;
            report.campProtected=Topaz.Combat.CampSafety.IsProtected(session.transform.position)&&!session.GetComponent<Topaz.Combat.PlayerVitality>().TryTakeDamage(99);
            MovePlayer(session,camp+new Vector3(0,0,-3));
            Camera.main.GetComponent<Topaz.Player.PlayerCamera>().LookAtPoint(camp+Vector3.up*.5f);
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"woodland-camp.png"));yield return new WaitForEndOfFrame();
            MovePlayer(session,camp+Vector3.forward);yield return null;
            var hud=FindAnyObjectByType<LoopHud>();hud.ShowTravelPanel();yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"camp-travel.png"));yield return new WaitForEndOfFrame();
            if(!session.TryFastTravel(Campfire.HomeId))throw new InvalidOperationException("Home travel unavailable");
            while(session.BlockMovement)yield return null;
            hud.ShowTravelPanel();yield return null;
            if(!session.TryFastTravel(campId))throw new InvalidOperationException("Camp travel unavailable");
            while(session.BlockMovement)yield return null;
            report.campTravel=session.ReturnCampfireId==campId;
            session.FlushCurrent();
            var repository=(ProfileRepository)typeof(WorldSession).GetField("_repository",flags).GetValue(session);
            var saved=repository.Load().World(session.ActiveWorldId);
            report.reloadPreserved=saved.structures.Any(r=>r.regionId==TopazSaveData.HomeRegion&&r.definitionId==BuildCatalog.Chest)&&saved.structures.Any(r=>r.instanceId==campId);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"camp-return.png"));yield return new WaitForEndOfFrame();
        }
    }
}
