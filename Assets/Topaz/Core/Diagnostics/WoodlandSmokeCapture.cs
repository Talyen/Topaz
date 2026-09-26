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
    public sealed class WoodlandSmokeCapture : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string unity, platform, graphics, pipeline, status;
            public int seed, frames, enemies, width, height;
            public double refreshHz;
            public bool focused, gpuOcclusion;
            public double averageGpuMs;
            public int gpuSamples;
            public double homeGenerationMs, expeditionGenerationMs, averageFrameMs, longestFrameMs;
            public long allocatedBytes;
            public bool gathered, cooked, homeBuilt, woodlandBuilt, campProtected, campTravel, reloadPreserved;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(!System.Environment.GetCommandLineArgs().Contains("--topaz-smoke"))return;
            if(FindAnyObjectByType<WoodlandSmokeCapture>()!=null)return;
            var go=new GameObject("Standalone Smoke Capture");DontDestroyOnLoad(go);go.AddComponent<WoodlandSmokeCapture>();
        }
        IEnumerator Start()
        {
            string directory=Path.Combine(Application.persistentDataPath,"Diagnostics");
            foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.StartsWith("--topaz-capture-dir="))directory=arg.Substring(20);
            Directory.CreateDirectory(directory);
            var report=new Report{unity=Application.unityVersion,platform=Application.platform.ToString(),graphics=SystemInfo.graphicsDeviceName,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name,status="starting"};
            WorldSession session=null;float deadline=Time.realtimeSinceStartup+30;
            while(Time.realtimeSinceStartup<deadline)
            {
                session=FindAnyObjectByType<WorldSession>();if(session!=null&&session.HasActivePair)break;yield return null;
            }
            if(session==null||!session.HasActivePair){report.status="world creation failed";File.WriteAllText(Path.Combine(directory,"smoke.json"),JsonUtility.ToJson(report,true));yield break;}
            report.width=Screen.width;report.height=Screen.height;report.refreshHz=Screen.currentResolution.refreshRateRatio.value;
            report.seed=session.ActiveWorld.seed;report.homeGenerationMs=FindObjectsByType<WoodlandRegion>().First(r=>r.regionId=="home").GenerationMilliseconds;
            yield return new WaitForSecondsRealtime(8);
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
            session.RequestTrailCrossing("expedition.clearing");deadline=Time.realtimeSinceStartup+30;
            while(Time.realtimeSinceStartup<deadline&&(session.CurrentRegionId!="expedition.clearing"||session.BlockMovement))yield return null;
            var expedition=FindObjectsByType<WoodlandRegion>().FirstOrDefault(r=>r.regionId=="expedition.clearing");
            if(expedition==null||!expedition.Ready){report.status="expedition failed";}else
            {
                report.expeditionGenerationMs=expedition.GenerationMilliseconds;yield return new WaitForSecondsRealtime(3);
                report.enemies=FindObjectsByType<Topaz.Combat.EnemyCombatant>().Length;
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"expedition.png"));yield return new WaitForEndOfFrame();
                report.status=report.enemies>0?"passed":"enemies not activated";
                session.FlushCurrent();
            }
            if (System.Environment.GetCommandLineArgs().Contains("--topaz-baseline-smoke") &&
                !(report.gathered && report.cooked && report.homeBuilt && report.woodlandBuilt && report.campProtected && report.campTravel && report.reloadPreserved))
                report.status="baseline scenario failed";
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
            var origin=session.ActiveRegion.regionOffset;
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
            var journal=FindAnyObjectByType<BuildingJournalView>();journal.Show();yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"build-journal.png"));yield return new WaitForEndOfFrame();journal.Hide();
            session.RequestTrailCrossing(TopazSaveData.ExpeditionRegion);
            float deadline=Time.realtimeSinceStartup+30;
            while((session.IsAtHome||session.BlockMovement)&&Time.realtimeSinceStartup<deadline)yield return null;
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
