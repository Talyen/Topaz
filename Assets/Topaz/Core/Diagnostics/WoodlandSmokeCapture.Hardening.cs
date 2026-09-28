using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Topaz.Gameplay;
using Topaz.Rendering;
using UnityEngine;
namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class ResidencyReport
        {
            public string status,profile,graphics,api,worldId,structures;
            public SurfaceCacheLighting.Status globalIllumination;
            public int seed,errors;
            public bool restarted;
            public bool focused=true;
            public List<ResidencyLap> laps=new List<ResidencyLap>();
        }
        [Serializable] sealed class ResidencyLap
        {
            public int lap,terrain,far,meshes,preload,objects;
            public long memory;
            public float p50,p95,p99,max,peakTerrainCommitMs,peakPreparationSliceMs,peakNavigationSubmitMs;
        }
        IEnumerator HardeningReview(WorldSession session,string directory,Report baseline)
        {
            var stream=session.ActiveRegion.Streaming;
            var report=new ResidencyReport{profile=stream.Plan.Settings.profileId,seed=stream.Plan.Seed,graphics=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),worldId=session.ActiveWorldId};
            report.globalIllumination=FindAnyObjectByType<VisualLookController>().GlobalIllumination;
            string checkpoint=Path.Combine(directory,"restart-checkpoint.json");
            bool resume=Environment.GetCommandLineArgs().Contains("--topaz-hardening-resume");
            if(resume)
            {
                var previous=JsonUtility.FromJson<ResidencyReport>(File.ReadAllText(checkpoint));
                report.restarted=previous.worldId==session.ActiveWorldId && previous.structures==StructureIdentity(session);
                report.status=report.restarted?"restart passed":"restart failed";
            }
            else
            {
                yield return BaselineReview(session,directory,baseline);
                report.status=baseline.gathered&&baseline.cooked&&baseline.homeBuilt&&baseline.woodlandBuilt&&baseline.campTravel&&baseline.reloadPreserved?"loop passed":"loop failed";
                var pack=(InventorySlots)typeof(WorldSession).GetField("_backpack",M0Fields).GetValue(session);
                pack.Add(session.WoodItem,99);pack.Add(session.StoneItem,99);
                var floor=Build(session,BuildCatalog.Floor);var builder=FindAnyObjectByType<RegionBuildings>();
                if(!builder.TryPlaceAt(BuildCatalog.Wall,floor+Vector3.forward*.75f) || !builder.TryPlaceAt(BuildCatalog.Roof,floor))
                    throw new InvalidOperationException("Hardening shelter construction failed.");
                for(int lap=0;lap<4;lap++)
                {
                    var frames=new List<float>();
                    foreach(var p in new[]{new Vector3(780,0,650),new Vector3(-700,0,640),new Vector3(270,0,-270),Vector3.zero})
                    {
                        bool ready=false;
                        IEnumerator Prepare(){yield return stream.PrepareDestination(p);ready=true;}
                        StartCoroutine(Prepare());
                        while(!ready){report.focused&=Application.isFocused;frames.Add(Time.unscaledDeltaTime*1000);yield return null;}
                        if(!stream.IsReadyAt(p))throw new InvalidOperationException("Hardening arrival failed: "+p+" "+stream.Failure);
                        MovePlayer(session,new Vector3(p.x,stream.Plan.Height(p.x,p.z),p.z));
                        for(int n=0;n<120;n++){report.focused&=Application.isFocused;frames.Add(Time.unscaledDeltaTime*1000);yield return null;}
                    }
                    frames.Sort();
                    report.laps.Add(new ResidencyLap{lap=lap,peakTerrainCommitMs=stream.PeakTerrainCommitMs,peakPreparationSliceMs=stream.PeakPreparationSliceMs,peakNavigationSubmitMs=stream.PeakNavigationSubmitMs,terrain=stream.LoadedCount,far=stream.DistantCount,meshes=stream.OwnedMeshCount,preload=stream.PreloadedCount,objects=FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,memory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),p50=frames[frames.Count/2],p95=frames[(int)(frames.Count*.95)],p99=frames[(int)(frames.Count*.99)],max=frames[frames.Count-1]});
                }
                report.structures=StructureIdentity(session);session.FlushCurrent();
                File.WriteAllText(checkpoint,JsonUtility.ToJson(report,true));
            }
            var menu=FindAnyObjectByType<VisualOptionsMenu>();menu.Toggle();yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,resume?"restart-options.png":"graphics-options.png"));yield return new WaitForEndOfFrame();menu.Close();
            report.errors=runtimeErrors;if(runtimeErrors>0)report.status="runtime errors";
            File.WriteAllText(Path.Combine(directory,resume?"restart.json":"residency.json"),JsonUtility.ToJson(report,true));
            if(Environment.GetCommandLineArgs().Contains("--topaz-smoke-quit"))Application.Quit();
        }
        static string StructureIdentity(WorldSession session)=>string.Join("|",session.ActiveWorld.structures.Select(r=>r.instanceId+":"+r.definitionId).OrderBy(x=>x));
    }
}
