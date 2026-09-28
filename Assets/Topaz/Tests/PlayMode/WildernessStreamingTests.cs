using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class WildernessStreamingTests : TopazInputTestFixture
    {
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static object Property(object obj,string name)=>obj.GetType().GetProperty(name,Flags).GetValue(obj);
        static object Field(object obj,string name)=>obj.GetType().GetField(name,Flags).GetValue(obj);
        [UnityTest]
        public IEnumerator LoadingCollisionFreezesGravityUntilTheWorldIsReady()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            Component stream=null;float deadline=Time.realtimeSinceStartup+5;
            while(stream==null && Time.realtimeSinceStartup<deadline)
            {
                var region=Property(session,"ActiveRegion");stream=region==null?null:(Component)Property(region,"Streaming");
                if(stream==null)yield return null;
            }
            Assert.That(stream,Is.Not.Null);
            Assert.That(Property(stream,"InitialReady"),Is.False);
            BuildingTestActions.Teleport(player,new Vector3(40,10,40));var start=player.transform.position;
            for(int i=0;i<10;i++)yield return null;
            Assert.That(Vector3.Distance(player.transform.position,start),Is.LessThan(.01f),"Loading must not accumulate gravity over absent terrain.");
            yield return WaitForWilderness();
        }
        [UnityTest]
        public IEnumerator InvalidDestinationPreservesPlayableOriginAndRepeatedRoutesBoundResidency()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            var stream=(MonoBehaviour)Property(Property(session,"ActiveRegion"),"Streaming");
            var prepare=stream.GetType().GetMethod("PrepareDestination");
            Vector3 origin=player.transform.position;
            yield return (IEnumerator)prepare.Invoke(stream,new object[]{new Vector3(5000,0,5000)});
            Assert.That(stream.GetType().GetMethod("IsReadyAt").Invoke(stream,new object[]{origin}),Is.True);
            Assert.That(stream.GetType().GetMethod("CanMoveTo").Invoke(stream,new object[]{new Vector3(5000,0,5000),.5f}),Is.False);
            int[] counts=new int[4];
            for(int lap=0;lap<4;lap++)
            {
                foreach(var point in new[]{new Vector3(780,0,650),origin})
                {
                    yield return (IEnumerator)prepare.Invoke(stream,new object[]{point});
                    var terrain=Terrain.activeTerrains.First(t=>point.x>=t.transform.position.x&&point.x<t.transform.position.x+128&&point.z>=t.transform.position.z&&point.z<t.transform.position.z+128);
                    BuildingTestActions.Teleport(player,new Vector3(point.x,terrain.SampleHeight(point)+.1f,point.z));
                    yield return new WaitForSecondsRealtime(1.5f);
                }
                counts[lap]=(int)Property(stream,"OwnedMeshCount");
                Assert.That(Property(stream,"DistantCount"),Is.LessThanOrEqualTo(49));
                Assert.That(Property(stream,"DistantTerrainCount"),Is.LessThanOrEqualTo(81));
                int canopyRoots=stream.transform.Cast<Transform>().Count(t=>t.name.EndsWith(" far canopy",System.StringComparison.Ordinal));
                Assert.That(canopyRoots,Is.EqualTo((int)Property(stream,"DistantCount")),"Unloaded canopy roots must not survive outside the residency registry.");
                Assert.That(Property(stream,"LoadedCount"),Is.LessThanOrEqualTo(25));
                Assert.That(Property(stream,"PreloadedCount"),Is.LessThanOrEqualTo(25));
                Assert.That(Property(stream,"PooledTerrainCount"),Is.LessThanOrEqualTo(8));
            }
            Assert.That(counts[3],Is.EqualTo(counts[2]));
            Assert.That(Property(stream,"Failure"),Is.Null);
        }
        [UnityTest]
        public IEnumerator OverlappingDestinationRequestsDoNotDestroyEachOthersPopulation()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var session=GameObject.Find("Player").GetComponent("WorldSession");
            var stream=(MonoBehaviour)Property(Property(session,"ActiveRegion"),"Streaming");
            var method=stream.GetType().GetMethod("PrepareDestination");
            var first=stream.StartCoroutine((IEnumerator)method.Invoke(stream,new object[]{new Vector3(270,0,130)}));
            var second=stream.StartCoroutine((IEnumerator)method.Invoke(stream,new object[]{new Vector3(-270,0,-130)}));
            yield return first;yield return second;
            Assert.That(Property(stream,"Failure"),Is.Null);
            Assert.That(Property(stream,"InitialReady"),Is.True);
            Assert.That(Property(stream,"NavigationReady"),Is.True);
        }
        [UnityTest]
        public IEnumerator OnlyCampfireRadiusProtectsAndEnteringCampDoesNotHeal()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            var fire=(Component)Field(session,"homeCampfire");var vitality=player.GetComponent("PlayerVitality");
            var damage=vitality.GetType().GetMethod("TryTakeDamage");
            BuildingTestActions.Teleport(player,fire.transform.position);yield return null;
            Assert.That((bool)damage.Invoke(vitality,new object[]{1}),Is.False);
            BuildingTestActions.Teleport(player,fire.transform.position+Vector3.right*15);yield return null;
            Assert.That((bool)damage.Invoke(vitality,new object[]{1}),Is.True,"The old entire-home-region immunity must not protect wilderness.");
            int health=(int)Property(vitality,"CurrentHealth");
            BuildingTestActions.Teleport(player,fire.transform.position);yield return null;
            Assert.That((int)Property(vitality,"CurrentHealth"),Is.EqualTo(health),"Recovery requires rest or defeat recovery, not boundary crossing.");
        }
        [UnityTest]
        public IEnumerator WorldLoadsContinuousTerrainAndCrossesChunkBoundary()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            var region=Property(session,"ActiveRegion");var stream=(Component)Property(region,"Streaming");
            Assert.That(stream,Is.Not.Null);
            for(int n=0;n<300 && !(bool)Property(stream,"NavigationReady");n++)yield return null;
            Assert.That((bool)Property(stream,"NavigationReady"),Is.True);
            Assert.That(Terrain.activeTerrains.Length,Is.InRange(4,12));
            var detailData=Terrain.activeTerrains.First().terrainData;
            int details=0;for(int z=0;z<4;z++)for(int x=0;x<4;x++)details+=detailData.ComputeDetailInstanceTransforms(x,z,0,1,out _).Length;
            Assert.That(details,Is.GreaterThan(0),"A populated density map must produce actual grass instances.");
            var destination=new Vector3(140,0,20);
            yield return (IEnumerator)stream.GetType().GetMethod("PrepareDestination").Invoke(stream,new object[]{destination});
            Assert.That((bool)stream.GetType().GetMethod("IsReadyAt").Invoke(stream,new object[]{destination}),Is.True);
            var terrain=Terrain.activeTerrains.First(t=>destination.x>=t.transform.position.x && destination.x<=t.transform.position.x+128 && destination.z>=t.transform.position.z && destination.z<=t.transform.position.z+128);
            destination.y=terrain.SampleHeight(destination)+.1f;
            BuildingTestActions.Teleport(player,destination);
            yield return null;
            Assert.That(player.transform.position.y,Is.GreaterThanOrEqualTo(terrain.SampleHeight(destination)-.15f));
            yield return null;
            Assert.That(Vector3.Distance(Camera.main.transform.position,player.transform.position),Is.LessThan(12),"Teleport must warp the follow camera instead of sweeping across unloaded terrain.");
            // The retained row already exists on this reversal; navigation still has to recenter.
            var back=new Vector3(120,0,20);back.y=Terrain.activeTerrains.First(t=>t.transform.position.x==0&&t.transform.position.z==0).SampleHeight(back)+.1f;
            BuildingTestActions.Teleport(player,back);
            var ready=stream.GetType().GetMethod("IsReadyAt");float limit=Time.realtimeSinceStartup+10;
            while(!(bool)ready.Invoke(stream,new object[]{new Vector3(-1,0,20)}) && Time.realtimeSinceStartup<limit)yield return null;
            Assert.That(ready.Invoke(stream,new object[]{new Vector3(-1,0,20)}),Is.True,"Reversing into retained terrain must update navigation coverage before the next boundary.");
            Assert.That(SceneManager.GetSceneByName("Woodland").isLoaded,Is.False);
        }
        [UnityTest]
        public IEnumerator DroppedLootUnloadsItsVisualAndReturnsWithoutDuplicatingSavedStock()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            var stream=(Component)Property(Property(session,"ActiveRegion"),"Streaming");
            object Call(object o,string method,params object[] args)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==method&&m.GetParameters().Length==args.Length).Invoke(o,args);
            Vector3 drop=player.transform.position+Vector3.right*8;
            Call(session,"DropItem",Property(session,"WoodItem"),3,drop);
            var pickup=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(c=>c.GetType().Name=="WorldPickup");
            int saved=(int)Property(session,"PickupCount");
            Vector3 far=new Vector3(350,0,350);
            yield return (IEnumerator)Call(stream,"PrepareDestination",far);
            far.y=Terrain.activeTerrains.First(t=>t.transform.position.x==256&&t.transform.position.z==256).SampleHeight(far);
            BuildingTestActions.Teleport(player,far);
            float end=Time.realtimeSinceStartup+20;
            while((bool)Call(stream,"HasTerrainAt",drop)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Call(stream,"HasTerrainAt",drop),Is.False,"The departing terrain must retire within the bounded streaming wait.");
            yield return null; // Deferred destruction completes on the terrain-retirement frame.
            Assert.That(pickup==null,Is.True,"Distant loot must release its scene object.");Assert.That(Property(session,"PickupCount"),Is.EqualTo(saved));
            yield return (IEnumerator)Call(stream,"PrepareDestination",drop);BuildingTestActions.Teleport(player,drop+Vector3.back*5);
            Call(session,"RefreshPickupVisibility");yield return null;
            Assert.That(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Count(c=>c.GetType().Name=="WorldPickup"),Is.EqualTo(1));
            Assert.That(Property(session,"PickupCount"),Is.EqualTo(saved));
        }
        [UnityTest]
        public IEnumerator SavedNearbyChestMaterialsWorkWithoutAChestGameObject()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            var world=Field(session,"_world");var records=(IList)Field(world,"structures");
            var record=System.Activator.CreateInstance(System.Type.GetType("Topaz.Gameplay.StructureStateRecord, Assembly-CSharp",true));
            void Set(object value,string field,object data)=>value.GetType().GetField(field).SetValue(value,data);
            Set(record,"instanceId","test.unloaded-chest");Set(record,"definitionId","structure.storage_chest");
            Set(record,"x",player.transform.position.x+29f);Set(record,"z",player.transform.position.z);
            var slots=(IList)Field(record,"slots");
            var item=System.Activator.CreateInstance(System.Type.GetType("Topaz.Gameplay.ItemStackRecord, Assembly-CSharp",true));
            var wood=Property(session,"WoodItem");Set(item,"itemId",Property(wood,"StableId"));Set(item,"count",8);slots.Add(item);records.Add(record);
            var spend=session.GetType().GetMethod("TrySpendHomeMaterials",new[]{typeof(int),typeof(int),typeof(int)});
            Assert.That((bool)spend.Invoke(session,new object[]{1,0,0}),Is.True);
            var pack=Field(session,"_backpack");
            pack.GetType().GetMethod("Remove").Invoke(pack,new[]{Property(wood,"StableId"),(object)10000});
            Set(record,"x",player.transform.position.x+31f);
            Assert.That((bool)spend.Invoke(session,new object[]{1,0,0}),Is.False);
            Set(record,"x",player.transform.position.x+29f);
            int before=(int)Field(item,"count");
            Assert.That((bool)spend.Invoke(session,new object[]{1,0,0}),Is.True);
            Assert.That((int)Field(item,"count"),Is.EqualTo(before-1));
        }
    }
}
