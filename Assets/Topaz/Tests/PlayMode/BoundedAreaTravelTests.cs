using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class BoundedAreaTravelTests : TopazInputTestFixture
    {
        static object Get(object target,string name)
        {var property=target.GetType().GetProperty(name);return property!=null?property.GetValue(target):target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(target);}
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
        static object[] Values(object value)=>((IEnumerable)value).Cast<object>().ToArray();
        static void MoveTo(GameObject player,object plan,object exit)
        {
            var p=(Vector3)Call(plan,"ExitPosition",exit);p.y=(float)Call(plan,"Height",p.x,p.z)+.08f;
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=p;controller.enabled=true;
            Call(player.GetComponent("PlayerController"),"ResetMotion");
        }
        static IEnumerator WaitForArea(object session,string id)
        {
            float deadline=Time.realtimeSinceStartup+50;
            while(Time.realtimeSinceStartup<deadline)
            {
                if((string)Get(session,"CurrentRegionId")==id && !(bool)Get(session,"_traveling"))
                {
                    var stream=Get(Get(session,"ActiveRegion"),"Streaming");
                    if(stream!=null && (bool)Get(stream,"InitialReady")){yield return null;yield break;}
                }
                yield return null;
            }
            Assert.Fail("Area travel did not finish: "+id+"; "+Get(session,"WorldProblem"));
        }
        [UnityTest]
        public IEnumerator AreaDepartureReturnPreservesEncounterStateClockAndOneActiveLandscape()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            var stream=Get(Get(session,"ActiveRegion"),"Streaming");var plan=Get(stream,"Plan");
            Assert.That((int)Get(stream,"LoadedCount"),Is.EqualTo(4));
            var enemy=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(e=>e.GetType().Name=="EnemyCombatant" && (int)Get(e,"CurrentHealth")>1);
            Call(enemy,"TakeDamage",1);int health=(int)Get(enemy,"CurrentHealth");string enemyId=(string)Get(enemy,"SpawnId");
            var world=Get(session,"ActiveWorld");var graph=Get(world,"graph");var sourceArea=Call(graph,"Find","wilderness");
            var exit=Values(Get(sourceArea,"exits"))[0];string destination=(string)Get(exit,"destinationAreaId");
            MoveTo(player,plan,exit);Call(session,"Commit");double hours=(double)Get(session,"WorldHours");
            Assert.That(Call(session,"RequestAreaTravel",Get(exit,"id")),Is.True);
            Assert.That(Call(player.GetComponent("PlayerVitality"),"TryTakeDamage",1),Is.False,"Loading cannot admit damage or start defeat recovery.");
            Assert.That(Time.timeScale,Is.Zero,"Actors, effects and physics pause during loading.");
            Assert.That(Call(session,"RequestAreaTravel",Get(exit,"id")),Is.False,"Overlapping travel must not publish another scene.");
            yield return WaitForArea(session,destination);
            Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(GameObject.Find("Home Campfire"),Is.Null,"An unloaded home's fire cannot protect another area's origin.");
            var destinationArea=Call(graph,"Find",destination);var back=Values(Get(destinationArea,"exits")).Single(e=>Equals(Get(e,"id"),Get(exit,"destinationExitId")));
            var newPlan=Get(Get(Get(session,"ActiveRegion"),"Streaming"),"Plan");MoveTo(player,newPlan,back);
            Assert.That(Call(session,"RequestAreaTravel",Get(back,"id")),Is.True);
            yield return WaitForArea(session,"wilderness");
            var returned=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(e=>e.GetType().Name=="EnemyCombatant" && Equals(Get(e,"SpawnId"),enemyId));
            Assert.That((int)Get(returned,"CurrentHealth"),Is.EqualTo(health));
            Assert.That((double)Get(session,"WorldHours")-hours,Is.LessThan(.1));
            Assert.That(SceneManager.sceneCount,Is.EqualTo(2),"Bootstrap and one area scene remain after repeated travel.");
            Assert.That((int)Get(Get(Get(session,"ActiveRegion"),"Streaming"),"LoadedCount"),Is.EqualTo(4));
            Assert.That((string)Call(session,"DiscoveredAreaConnections"),Does.Contain("River Terraces"));
        }
        [UnityTest]
        public IEnumerator FailedDestinationReconstructsTheRecordedOriginAndKeepsSavingEnabled()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");var world=Get(session,"ActiveWorld");var graph=Get(world,"graph");
            var exit=Values(Get(Call(graph,"Find","wilderness"),"exits"))[0];var destination=Call(graph,"Find",Get(exit,"destinationAreaId"));
            var settings=Get(destination,"generationSettings");var size=settings.GetType().GetField("worldSize");int correctSize=(int)size.GetValue(settings);
            var plan=Get(Get(Get(session,"ActiveRegion"),"Streaming"),"Plan");MoveTo(player,plan,exit);Vector3 departure=player.transform.position;
            Application.LogCallback restore=(message,stack,type)=>{if(type==LogType.Exception)size.SetValue(settings,correctSize);};
            Application.logMessageReceived+=restore;
            try
            {
                LogAssert.Expect(LogType.Exception,new System.Text.RegularExpressions.Regex("ArgumentException: Unsupported wilderness profile or content mapping"));
                Assert.That(Call(session,"RequestAreaTravel",Get(exit,"id")),Is.True);
                // The durable departure snapshot is valid. Fail destination realization after its retirement begins.
                size.SetValue(settings,-1);
                yield return WaitForArea(session,"wilderness");
                Assert.That((bool)Get(session,"_savingEnabled"),Is.True);
                Assert.That(Vector3.Distance(player.transform.position,departure),Is.LessThan(3));
                Assert.That(SceneManager.sceneCount,Is.EqualTo(2));
                var profile=Call(Get(session,"_repository"),"Load");Call(profile,"Validate");
            }
            finally{size.SetValue(settings,correctSize);Application.logMessageReceived-=restore;}
        }
        [UnityTest]
        public IEnumerator AllEightAreasRealizeSavedRecipesAndRetireTheirScenes()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var session=GameObject.Find("Player").GetComponent("WorldSession");var world=Get(session,"ActiveWorld");
            var travel=session.GetType().GetMethod("TravelToArea",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(var area in Values(Get(Get(world,"graph"),"areas")))
            {
                Call(session,"Commit");
                yield return (IEnumerator)travel.Invoke(session,new object[]{Get(area,"id"),null,(Vector3?)Vector3.zero});
                Assert.That(Get(session,"CurrentRegionId"),Is.EqualTo(Get(area,"id")));
                var stream=Get(Get(session,"ActiveRegion"),"Streaming");
                Assert.That(Get(stream,"Failure"),Is.Null);
                Assert.That(Get(stream,"NavigationReady"),Is.True);
                Assert.That(SceneManager.sceneCount,Is.EqualTo(2));
                Assert.That(Time.timeScale,Is.EqualTo(1));
            }
        }
    }
}
