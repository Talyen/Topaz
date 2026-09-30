using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class DestinationPlayTests : TopazInputTestFixture
    {
        static object Property(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
        static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);

        [UnityTest,Timeout(180000)]
        public IEnumerator GraveAndPeakAreStreamedAndReachableFromTheirApproaches()
        {
            var unexpected=new List<string>();
            void Record(string message,string stack,LogType type)
            {
                if(type is not (LogType.Error or LogType.Exception))return;
                // An open Editor preview can release its target during Pipeline eval; it is not a player log.
                if(message.StartsWith("Releasing render texture that is set as Camera.targetTexture!"))return;
                unexpected.Add(message);
            }
            Application.logMessageReceived+=Record;
            LogAssert.ignoreFailingMessages=true;
            try
            {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            var stream=(Component)Property(Property(session,"ActiveRegion"),"Streaming");
            var plan=Property(stream,"Plan");
            var sites=((IEnumerable)Property(plan,"Destinations")).Cast<object>().ToArray();
            Assert.That(sites.Length,Is.GreaterThanOrEqualTo(2));
            string directory="TestResults/artifacts/synty-destinations";Directory.CreateDirectory(directory);
            foreach(var (kind,file) in new[]{("TitansGrave","grave.png"),("SplitPeak","peak.png")})
            {
                var site=sites.First(s=>Field(s,"Kind").ToString()==kind);
                string id=(string)Field(site,"Id");
                var route=((IEnumerable)Property(plan,"Routes")).Cast<object>().First(r=>(string)Field(r,"DestinationId")==id);
                var points=((IEnumerable)Field(route,"Points")).Cast<Vector3>().ToArray();
                Vector3 origin=points[kind=="SplitPeak"?10:0],destination=points[points.Length-1];
                yield return (IEnumerator)Call(stream,"PrepareDestination",destination);
                Assert.That(Property(stream,"Failure"),Is.Null);
                origin.y=(float)Call(plan,"Height",origin.x,origin.z);
                destination.y=(float)Call(plan,"Height",destination.x,destination.z);
                Assert.That(NavMesh.SamplePosition(origin,out var start,4,NavMesh.AllAreas),Is.True,id+" approach NavMesh");
                Assert.That(NavMesh.SamplePosition(destination,out var end,5,NavMesh.AllAreas),Is.True,id+" destination NavMesh");
                var path=new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path),Is.True,id+" path query");
                float yaw=(float)Field(site,"Yaw");
                var view=destination+Quaternion.Euler(0,yaw,0)*Vector3.forward*(kind=="SplitPeak"?250:90);
                view.y=(float)Call(plan,"Height",view.x,view.z);
                yield return (IEnumerator)Call(stream,"PrepareDestination",view);
                BuildingTestActions.Teleport(player,view);
                var camera=Camera.main;
                var cameraRig=camera.GetComponent("PlayerCamera");
                Call(cameraRig,"LookAtPoint",destination+Vector3.up*(kind=="SplitPeak"?65:11));
                var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var previous=camera.targetTexture;target.Create();camera.targetTexture=target;
                try
                {
                    for(int frame=0;frame<12;frame++){yield return null;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});}
                    var active=RenderTexture.active;RenderTexture.active=target;
                    var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    try{image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(directory,file),image.EncodeToPNG());}
                    finally{RenderTexture.active=active;UnityEngine.Object.Destroy(image);}
                }
                finally{camera.targetTexture=previous;target.Release();UnityEngine.Object.Destroy(target);}
                Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),
                    id+" accessible from approach. Start="+start.position+" end="+end.position+
                    " corners="+string.Join(";",path.corners.Select(p=>p.ToString())));
                if(kind=="TitansGrave")
                {
                    var guardians=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                        .Where(actor=>actor.GetType().Name=="EnemyCombatant" &&
                                      ((string)Property(actor,"SpawnId")).StartsWith(id+".enemy.")).ToArray();
                    Assert.That(guardians.Length,Is.InRange(1,3),"Grave skeleton group");
                    Assert.That(Property(guardians[0],"Species").ToString(),Is.EqualTo("Skeleton"));
                    int before=(int)Property(session,"PickupCount");
                    var world=Property(session,"ActiveWorld");
                    int savedBefore=((IEnumerable)Field(world,"pickups")).Cast<object>().Count();
                    string spawnId=(string)Property(guardians[0],"SpawnId");
                    Call(guardians[0],"TakeDamage",100);
                    Assert.That((int)Property(session,"PickupCount"),Is.GreaterThan(before));
                    var drops=((IEnumerable)Field(world,"pickups")).Cast<object>().Skip(savedBefore);
                    Assert.That(drops.Any(item=>(string)Field(item,"itemId")=="material.bone-fragments"),Is.True);
                    var deadline=((IEnumerable)Field(world,"enemyRespawns")).Cast<object>()
                        .FirstOrDefault(record=>(string)Field(record,"spawnId")==spawnId);
                    Assert.That(deadline,Is.Not.Null,"Grave skeleton return deadline");
                }
            }
            foreach(var (kind,species) in new[]{("GoblinGate","Goblin"),("Cabin","Raider")})
            {
                var site=sites.First(s=>Field(s,"Kind").ToString()==kind);
                string id=(string)Field(site,"Id");
                float x=(float)Field(site,"X"),z=(float)Field(site,"Z");
                var point=new Vector3(x,(float)Call(plan,"Height",x,z),z);
                yield return (IEnumerator)Call(stream,"PrepareDestination",point);
                var siteRoute=((IEnumerable)Property(plan,"Routes")).Cast<object>().First(r=>(string)Field(r,"DestinationId")==id);
                var routePoints=((IEnumerable)Field(siteRoute,"Points")).Cast<Vector3>().ToArray();
                var view=routePoints[0];view.y=(float)Call(plan,"Height",view.x,view.z);
                BuildingTestActions.Teleport(player,view);
                var camera=Camera.main;var rig=camera.GetComponent("PlayerCamera");
                Call(rig,"LookAtPoint",point+Vector3.up*2);
                for(int frame=0;frame<10;frame++)yield return null;
                Capture(camera,Path.Combine(directory,kind.ToLowerInvariant()+".png"));
                var group=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Where(actor=>actor.GetType().Name=="EnemyCombatant" &&
                                  ((string)Property(actor,"SpawnId")).StartsWith(id+".enemy.")).ToArray();
                Assert.That(group.Length,Is.EqualTo(2),kind+" group size");
                foreach(var actor in group)
                {
                    Assert.That(Property(actor,"Species").ToString(),Is.EqualTo(species),kind+" species");
                    Assert.That(actor.GetComponent<NavMeshAgent>().isOnNavMesh,Is.True,kind+" enemy navigation");
                }
                if(kind!="GoblinGate")continue;
                int before=(int)Property(session,"PickupCount");
                Call(group[0],"TakeDamage",100);
                Assert.That((int)Property(session,"PickupCount"),Is.GreaterThan(before),"Goblin material drop");
                var world=Property(session,"ActiveWorld");
                var pickup=((IEnumerable)Field(world,"pickups")).Cast<object>().Last();
                Assert.That((string)Field(pickup,"itemId"),Is.EqualTo("material.wood").Or.EqualTo("material.stone"));
            }
            var dock=sites.First(s=>Field(s,"Kind").ToString()=="Dock");
            string dockId=(string)Field(dock,"Id");
            float dockX=(float)Field(dock,"X"),dockZ=(float)Field(dock,"Z");
            var dockPoint=new Vector3(dockX,(float)Call(plan,"Height",dockX,dockZ),dockZ);
            yield return (IEnumerator)Call(stream,"PrepareDestination",dockPoint);
            var dockRoute=((IEnumerable)Property(plan,"Routes")).Cast<object>().First(r=>(string)Field(r,"DestinationId")==dockId);
            var dockView=((IEnumerable)Field(dockRoute,"Points")).Cast<Vector3>().First();
            dockView.y=(float)Call(plan,"Height",dockView.x,dockView.z);
            BuildingTestActions.Teleport(player,dockView);
            var dockCamera=Camera.main;var dockRig=dockCamera.GetComponent("PlayerCamera");
            Call(dockRig,"LookAtPoint",dockPoint+Vector3.up);
            for(int frame=0;frame<10;frame++)yield return null;
            Capture(dockCamera,Path.Combine(directory,"dock.png"));
            Assert.That(unexpected,Is.Empty,string.Join("\n",unexpected));
            }
            finally
            {
                Application.logMessageReceived-=Record;
                LogAssert.ignoreFailingMessages=false;
            }
        }

        static void Capture(Camera camera,string path)
        {
            var texture=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var prior=camera.targetTexture;texture.Create();camera.targetTexture=texture;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=texture});
                var active=RenderTexture.active;RenderTexture.active=texture;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                try{image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
                finally{RenderTexture.active=active;UnityEngine.Object.Destroy(image);}
            }
            finally{camera.targetTexture=prior;texture.Release();UnityEngine.Object.Destroy(texture);}
        }

        [UnityTest,Category("Stress"),Timeout(360000)]
        public IEnumerator EverySelectedDestinationHasLocalNavMeshAccess()
        {
            var unexpected=new List<string>();
            void Record(string message,string stack,LogType type)
            {
                if(type is not (LogType.Error or LogType.Exception) ||
                    message.StartsWith("Releasing render texture that is set as Camera.targetTexture!"))return;
                unexpected.Add(message);
            }
            Application.logMessageReceived+=Record;
            LogAssert.ignoreFailingMessages=true;
            try
            {
                yield return SceneManager.LoadSceneAsync("Bootstrap");
                yield return WaitForWilderness();
                var session=GameObject.Find("Player").GetComponent("WorldSession");
                var stream=(Component)Property(Property(session,"ActiveRegion"),"Streaming");
                var plan=Property(stream,"Plan");
                var sites=((IEnumerable)Property(plan,"Destinations")).Cast<object>().ToArray();
                foreach(var site in sites)
                {
                    string id=(string)Field(site,"Id");
                    var route=((IEnumerable)Property(plan,"Routes")).Cast<object>().First(r=>(string)Field(r,"DestinationId")==id);
                    var points=((IEnumerable)Field(route,"Points")).Cast<Vector3>().ToArray();
                    Vector3 approach=points[id.EndsWith("splitpeak")?10:0],target=points[points.Length-1];
                    if(!id.EndsWith("splitpeak") && !id.EndsWith("titansgrave"))
                    {
                        var toward=approach-target;toward.y=0;toward.Normalize();
                        target+=toward*((float)Field(site,"Radius")+2);
                    }
                    yield return (IEnumerator)Call(stream,"PrepareDestination",target);
                    Assert.That(Property(stream,"Failure"),Is.Null,id+" streaming");
                    approach.y=(float)Call(plan,"Height",approach.x,approach.z);
                    target.y=(float)Call(plan,"Height",target.x,target.z);
                    Assert.That(NavMesh.SamplePosition(approach,out var start,4,NavMesh.AllAreas),Is.True,id+" approach");
                    Assert.That(NavMesh.SamplePosition(target,out var end,8,NavMesh.AllAreas),Is.True,id+" site");
                    var path=new NavMeshPath();
                    Assert.That(NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path),Is.True,id+" query");
                    Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),id+" local access");
                }
                Assert.That(unexpected,Is.Empty,string.Join("\n",unexpected));
            }
            finally{Application.logMessageReceived-=Record;LogAssert.ignoreFailingMessages=false;}
        }
    }
}
