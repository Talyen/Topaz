using System;
using System.Collections;
using System.IO;
using System.Linq;
using Topaz.Gameplay;
using Topaz.Generation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class AreaJourneyReport
        {
            public int seed,errors,terrainCells;
            public string status,area;
            public bool returned;
            public double outboundSeconds,returnSeconds;
            public long memoryBefore,memoryAfter;
            public float forestRouteMeters,riverRouteMeters,walkDistance;
            public int forestTrees,riverTrees;
        }
        IEnumerator CaptureAreaJourney(WorldSession session,string directory)
        {
            var report=new AreaJourneyReport {seed=session.ActiveWorld.seed,status="starting",memoryBefore=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()};
            IEnumerator Picture(string name)
            {
                yield return new WaitForSecondsRealtime(.7f); // Initial entry's reveal fade uses real time, not rendered frame count.
                for(int i=0;i<30;i++)yield return null;
                var previous=RenderTexture.active;RenderTexture.active=alpineTarget;
                var texture=new Texture2D(alpineTarget.width,alpineTarget.height,TextureFormat.RGB24,false);
                try{texture.ReadPixels(new Rect(0,0,alpineTarget.width,alpineTarget.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());}
                finally{RenderTexture.active=previous;Destroy(texture);}
            }
            void Move(Vector3 point)
            {
                point.y=session.ActiveRegion.Wilderness.Height(point.x,point.z)+.08f;
                var controller=session.GetComponent<CharacterController>();controller.enabled=false;session.transform.position=point;controller.enabled=true;
                session.GetComponent<Topaz.Player.PlayerController>().ResetMotion();
                Camera.main?.GetComponent<Topaz.Player.PlayerCamera>()?.SnapAfterAreaTravel();
            }
            IEnumerator Cross(AreaConnection exit,Action<double> timing)
            {
                Move(session.ActiveRegion.Wilderness.ExitPosition(exit));yield return null;
                double start=Time.realtimeSinceStartupAsDouble;
                if(!session.RequestAreaTravel(exit.id)){report.status="departure rejected";yield break;}
                float deadline=Time.realtimeSinceStartup+60;
                while(Time.realtimeSinceStartup<deadline)
                {
                    if(session.CurrentRegionId==exit.destinationAreaId && !session.BlockMovement && session.ActiveRegion.Streaming.InitialReady)
                    {timing(Time.realtimeSinceStartupAsDouble-start);yield break;}
                    yield return null;
                }
                report.status="travel failed";
            }
            var original=session.CurrentRegionId;
            report.forestRouteMeters=session.ActiveRegion.Wilderness.AreaRouteLength;
            report.forestTrees=CountAreaTrees(session.ActiveRegion.Wilderness);
            var route=session.ActiveRegion.Wilderness.Routes[0].Points;
            Move(route[Mathf.Max(1,route.Count-4)]);yield return Picture("forest");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-area-walk")>=0)
            {
                var pad=InputSystem.AddDevice<Gamepad>();var previous=InputSystem.settings.backgroundBehavior;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                try
                {
                    var start=session.transform.position;float began=Time.realtimeSinceStartup,nextFrame=began;int frame=0;
                    string motion=Path.Combine(directory,"walk");Directory.CreateDirectory(motion);
                    while(Time.realtimeSinceStartup-began<6)
                    {
                        var delta=route[route.Count-1]-session.transform.position;delta.y=0;
                        var camera=Camera.main.transform;var forward=Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;var right=Vector3.ProjectOnPlane(camera.right,Vector3.up).normalized;
                        var stick=delta.magnitude<2?Vector2.zero:new Vector2(Vector3.Dot(delta.normalized,right),Vector3.Dot(delta.normalized,forward));
                        InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=stick});yield return null;
                        if(Time.realtimeSinceStartup<nextFrame)continue;nextFrame=Time.realtimeSinceStartup+.2f;
                        var old=RenderTexture.active;RenderTexture.active=alpineTarget;var image=new Texture2D(alpineTarget.width,alpineTarget.height,TextureFormat.RGB24,false);
                        try{image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(motion,"frame-"+(frame++).ToString("D3")+".png"),image.EncodeToPNG());}
                        finally{RenderTexture.active=old;Destroy(image);}
                    }
                    report.walkDistance=Vector3.Distance(start,session.transform.position);
                }
                finally{InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=previous;}
            }
            var exit=session.ActiveWorld.graph.Find(original).exits[0];Move(session.ActiveRegion.Wilderness.ExitPosition(exit));yield return Picture("passage");
            yield return Cross(exit,value=>report.outboundSeconds=value);
            if(session.CurrentRegionId==exit.destinationAreaId)
            {
                report.riverRouteMeters=session.ActiveRegion.Wilderness.AreaRouteLength;report.riverTrees=CountAreaTrees(session.ActiveRegion.Wilderness);
                var riverRoute=session.ActiveRegion.Wilderness.Routes[1].Points;
                var crossing=session.ActiveRegion.Wilderness.Crossings.FirstOrDefault();
                Move(crossing.Id!=null?crossing.Position-crossing.Forward*8:riverRoute[riverRoute.Count-4]);yield return Picture("river");
                var back=session.ActiveWorld.graph.Find(session.CurrentRegionId).exits.Single(e=>e.id==exit.destinationExitId);
                yield return Cross(back,value=>report.returnSeconds=value);
                report.returned=session.CurrentRegionId==original;
            }
            report.errors=runtimeErrors;report.area=session.CurrentRegionId;
            report.terrainCells=session.ActiveRegion.Streaming.LoadedCount;report.memoryAfter=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            report.status=report.returned && report.errors==0?"passed":report.status;
            File.WriteAllText(Path.Combine(directory,"areas.json"),JsonUtility.ToJson(report,true));
            StopAlpineRenderLoop();
            if(Environment.GetCommandLineArgs().Contains("--topaz-smoke-quit"))Application.Quit(report.status=="passed"?0:1);
        }
        static int CountAreaTrees(WildernessPlan plan)
        {int count=0;for(int z=plan.MinChunk;z<plan.MaxChunk;z++)for(int x=plan.MinChunk;x<plan.MaxChunk;x++)count+=plan.Decorations(new WildernessPlan.Chunk(x,z),plan.Settings.decorationCount).Count(s=>s.Kind==0);return count;}
    }
}
