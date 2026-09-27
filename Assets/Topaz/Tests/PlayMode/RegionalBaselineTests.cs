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
    public sealed class RegionalBaselineTests : TopazInputTestFixture
    {
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        static object Get(object o,string n)=>o.GetType().GetProperty(n,Flags)?.GetValue(o) ?? o.GetType().GetField(n,Flags).GetValue(o);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==n && m.GetParameters().Length==a.Length).Invoke(o,a);
        static object[] Records(Component s)=>((IEnumerable)Get(Get(s,"ActiveWorld"),"structures")).Cast<object>().ToArray();
        static void Give(Component s,int wood,int stone=0)
        {var pack=Get(s,"_backpack");Call(pack,"Add",Get(s,"WoodItem"),wood);Call(pack,"Add",Get(s,"StoneItem"),stone);}
        [UnityTest]
        public IEnumerator OutdoorStructuresKeepWorldHeightAndSpatialStorageAfterTravel()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            var player=GameObject.Find("Player");var s=player.GetComponent("WorldSession");
            Give(s,20);var home=BuildingTestActions.Place(s,"structure.storage_chest");
            var chest=((IEnumerable)Get(Get(s,"homeBuilds"),"Chests")).Cast<object>().Single();
            Call(Get(chest,"Inventory"),"Add",Get(s,"WoodItem"),17);
            int carried=(int)Get(s,"WoodCount");
            yield return TopazTestTravel.EnterWoodland(player);
            Assert.That(Get(s,"HomeWoodCount"),Is.EqualTo(carried),"Distant saved storage must not fund construction.");
            var away=BuildingTestActions.Place(s,"structure.storage_chest");
            var records=Records(s).Where(r=>(string)Get(r,"definitionId")=="structure.storage_chest").ToArray();
            Assert.That(records.Length,Is.EqualTo(2));
            Assert.That(records.Select(r=>(string)Get(r,"regionId")).Distinct().Count(),Is.EqualTo(1));
            Assert.That((float)Get(records[1],"y"),Is.EqualTo(away.y).Within(.01f));
            Call(s,"FlushCurrent");
            var repo=Get(s,"_repository");var loaded=Call(repo,"Load");
            var worlds=(IEnumerable)Get(loaded,"worlds");
            Assert.That(((IEnumerable)Get(worlds.Cast<object>().Single(),"structures")).Cast<object>().Count(r=>(string)Get(r,"definitionId")=="structure.storage_chest"),Is.EqualTo(2));
            yield return (IEnumerator)Call(s,"FastTravel",((IEnumerable)Get(s,"TravelDestinations")).Cast<object>().Single(d=>(string)Get(d,"stableId")=="campfire.home"));
            Assert.That(((IEnumerable)Get(Get(s,"homeBuilds"),"Chests")).Cast<object>().Any(),Is.True);
            Assert.That(Get(s,"HomeWoodCount"),Is.EqualTo((int)Get(s,"WoodCount")+17));
        }
        [UnityTest]
        public IEnumerator BuiltCampIsDiscoveredProtectsTravelsRecoversAndRemovalRepairsVisits()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            var player=GameObject.Find("Player");var s=player.GetComponent("WorldSession");
            Give(s,20,20);yield return TopazTestTravel.EnterWoodland(player);
            Vector3 position=BuildingTestActions.Place(s,"structure.campfire");yield return null;
            yield return new WaitForSeconds(.2f);
            Assert.That(UnityEngine.AI.NavMesh.SamplePosition(position,out _,.5f,UnityEngine.AI.NavMesh.AllAreas),Is.False,"Camps carve enemy navigation while remaining walkable to the player.");
            var record=Records(s).Single(r=>(string)Get(r,"definitionId")=="structure.campfire");string id=(string)Get(record,"instanceId");
            Assert.That(((IEnumerable)Get(s,"DiscoveredCampfireIds")).Cast<string>(),Does.Contain(id));
            BuildingTestActions.Teleport(player,position+Vector3.forward);yield return null;
            Assert.That(Get(s,"ReturnCampfireId"),Is.EqualTo(id));
            var safety=Type.GetType("Topaz.Combat.CampSafety, Assembly-CSharp",true);
            Assert.That(safety.GetMethod("IsProtected").Invoke(null,new object[]{position}),Is.True);
            var vitality=player.GetComponent("PlayerVitality");
            Assert.That(Call(vitality,"TryTakeDamage",100),Is.False);
            yield return (IEnumerator)Call(s,"FastTravel",((IEnumerable)Get(s,"TravelDestinations")).Cast<object>().Single(d=>(string)Get(d,"stableId")=="campfire.home"));
            var destination=((IEnumerable)Get(s,"TravelDestinations")).Cast<object>().Single(d=>(string)Get(d,"stableId")==id);
            yield return (IEnumerator)Call(s,"FastTravel",destination);
            Assert.That(Get(s,"CurrentRegionId"),Is.EqualTo("wilderness"));
            Assert.That(Vector3.Distance(player.transform.position,position),Is.LessThan(3));
            double before=(double)Get(s,"WorldHours");
            Assert.That(Call(s,"RecoverAfterDefeat",vitality),Is.True);
            float timeout=Time.realtimeSinceStartup+15;
            while((bool)Get(s,"IsRecovering")&&Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(Get(s,"IsRecovering"),Is.False);
            Assert.That((double)Get(s,"WorldHours"),Is.GreaterThanOrEqualTo(before+8));
            var b=Get(s,"homeBuilds");Call(b,"BeginEdit");b.GetType().GetField("_selected",Flags).SetValue(b,record);Call(b,"RemoveSelected");
            Assert.That(Get(s,"ReturnCampfireId"),Is.EqualTo("campfire.home"));
            Assert.That(((IEnumerable)Get(s,"TravelDestinations")).Cast<object>().Any(d=>(string)Get(d,"stableId")==id),Is.False);
        }
        [UnityTest]
        public IEnumerator UnavailableCampDestinationRestoresCurrentRegionAndStructures()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            Give(session,6);yield return TopazTestTravel.EnterWoodland(player);
            BuildingTestActions.Place(session,"structure.storage_chest");
            var type=Type.GetType("Topaz.Gameplay.CampfireTravelCatalog+Destination, Assembly-CSharp",true);
            var target=Activator.CreateInstance(type);
            type.GetField("stableId").SetValue(target,"missing-camp");
            type.GetField("regionId").SetValue(target,"wilderness");
            type.GetField("sceneName").SetValue(target,"Bootstrap");
            var position=player.transform.position;
            yield return (IEnumerator)Call(session,"FastTravel",target);
            Assert.That(Get(session,"CurrentRegionId"),Is.EqualTo("wilderness"));
            Assert.That(Get(session,"BlockMovement"),Is.False);
            Assert.That(Vector3.Distance(player.transform.position,position),Is.LessThan(.3f));
            Assert.That(((IEnumerable)Get(Get(session,"homeBuilds"),"Chests")).Cast<object>().Count(),Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator OverlappingProtectionBlocksMeleeSpellAndBoltWithoutAwardingDamage()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");
            yield return TopazTestTravel.EnterWoodland(player);
            var enemy=TopazTestTravel.Scout().GetComponent("EnemyCombatant");
            ((Behaviour)enemy).enabled=false;
            enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
            Vector3 center=enemy.transform.position;
            var safety=Type.GetType("Topaz.Combat.CampSafety, Assembly-CSharp",true);
            var first=new GameObject("First Test Camp");first.transform.position=center;first.AddComponent(safety);
            var second=new GameObject("Second Test Camp");second.transform.position=center+Vector3.forward*5;second.AddComponent(safety);
            int health=(int)Get(enemy,"CurrentHealth");
            Call(enemy,"TakeDirectedDamage",3,center+Vector3.right*20);
            Assert.That(Get(enemy,"CurrentHealth"),Is.EqualTo(health));
            BuildingTestActions.Teleport(player,center+Vector3.right*20);
            var spell=player.GetComponent("GroundSpellAbility");
            Assert.That(Call(spell,"Begin",center,false,3,0f),Is.True);
            yield return new WaitForSeconds(1.1f);
            Assert.That(Get(enemy,"CurrentHealth"),Is.EqualTo(health));
            var item=Call(session,"Item","gear.crypt.crossbow");var attack=Get(Get(item,"Weapon"),"CrossbowAttack");
            var bolt=(Component)UnityEngine.Object.Instantiate((UnityEngine.Object)Get(attack,"BoltPrefab"));
            Call(bolt,"Launch",attack,center+Vector3.right*14+Vector3.up,Vector3.left,3,null,session,false,0f);
            yield return new WaitForSeconds(.5f);
            Assert.That(bolt==null,Is.True,"Bolt must stop at the protected boundary.");
            Assert.That(Get(enemy,"CurrentHealth"),Is.EqualTo(health));
            UnityEngine.Object.Destroy(first);yield return null;
            Assert.That(safety.GetMethod("IsProtected").Invoke(null,new object[]{center}),Is.True);
            UnityEngine.Object.Destroy(second);yield return null;
            Assert.That(safety.GetMethod("IsProtected").Invoke(null,new object[]{center}),Is.False);
            Call(enemy,"TakeDirectedDamage",1,center+Vector3.right*20);
            Assert.That((int)Get(enemy,"CurrentHealth"),Is.LessThan(health));
        }
    }
}
