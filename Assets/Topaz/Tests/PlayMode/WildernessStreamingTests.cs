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
            float end=Time.realtimeSinceStartup+10;while(pickup!=null&&Time.realtimeSinceStartup<end)yield return null;
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
