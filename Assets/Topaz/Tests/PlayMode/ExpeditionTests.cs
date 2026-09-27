using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    /// <summary>Continuous-world replacements for the former scene-transition scenarios.</summary>
    public sealed class ExpeditionTests : TopazInputTestFixture
    {
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        static object Get(object o,string n)=>o.GetType().GetProperty(n,Flags)?.GetValue(o)??o.GetType().GetField(n,Flags).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);
        static void SetField(object o,string n,object value)=>o.GetType().GetField(n,Flags).SetValue(o,value);
        static Component Session=>GameObject.Find("Player").GetComponent("WorldSession");
        static IEnumerator Boot(){yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();}
        static IEnumerator Move(Component session,Vector3 point)
        {
            var stream=Get(Get(session,"ActiveRegion"),"Streaming");
            yield return (IEnumerator)Call(stream,"PrepareDestination",point);
            var terrain=Terrain.activeTerrains.First(t=>point.x>=t.transform.position.x&&point.x<=t.transform.position.x+128&&point.z>=t.transform.position.z&&point.z<=t.transform.position.z+128);
            point.y=terrain.SampleHeight(point);BuildingTestActions.Teleport(session.gameObject,point);yield return null;
        }
        static object[] Actors()=>UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(c=>c.GetType().Name=="EnemyCombatant").Cast<object>().ToArray();
        static IEnumerator WaitRecovery(Component session)
        {
            float end=Time.realtimeSinceStartup+20;while((bool)Get(session,"IsRecovering")&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Get(session,"IsRecovering"),Is.False);
        }
        [UnityTest]
        public IEnumerator WalkingAcrossAChunkEdgeKeepsOneContinuousScene()
        {
            var pad=InputSystem.AddDevice<Gamepad>();yield return Boot();var s=Session;
            yield return Move(s,new Vector3(126,0,16));
            Vector3 point=s.transform.position;bool found=false;
            for(float z=16;z<112;z+=8)
            {
                var candidate=new Vector3(126,0,z);var region=Get(s,"ActiveRegion");var plan=Get(region,"Wilderness");candidate.y=(float)Call(plan,"Height",candidate.x,candidate.z);
                if(Physics.OverlapBox(candidate+new Vector3(3,.9f,0),new Vector3(4,.7f,.45f)).Any(c=>!(c is TerrainCollider)&&!c.transform.IsChildOf(s.transform)))continue;
                point=candidate;found=true;break;
            }
            Assert.That(found,Is.True);BuildingTestActions.Teleport(s.gameObject,point);AimAt(point+Vector3.forward*10);yield return null;
            Set(pad.leftStick,Vector2.right);yield return new WaitForSeconds(1);Set(pad.leftStick,Vector2.zero);
            Assert.That(s.transform.position.x,Is.GreaterThan(128));
            Assert.That(Get(s,"CurrentRegionId"),Is.EqualTo("wilderness"));Assert.That(SceneManager.GetSceneByName("Woodland").isLoaded,Is.False);
        }
        [UnityTest]
        public IEnumerator HarvestedNodesKeepTheirStableStateAfterUnloadingAndReturning()
        {
            yield return Boot();var s=Session;var node=FindResource("HarvestTree").GetComponent("HarvestTree");
            string id=(string)Get(node,"StableObjectId");Vector3 position=node.transform.position;
            for(int i=0;i<4&&(bool)Get(node,"IsAvailable");i++)Assert.That(Call(node,"TryChop",position+Vector3.back*1.3f,Vector3.forward,2.1f,90f),Is.True);
            Assert.That(Get(node,"IsAvailable"),Is.False);double deadline=(double)Get(Call(s,"GetOrCreateNodeState",id),"readyAtWorldHours");
            yield return Move(s,new Vector3(350,0,350));yield return new WaitForSeconds(.8f);
            Assert.That(node==null,Is.True,"The original chunk instance must actually unload.");
            yield return Move(s,position);
            var restored=FindResource("HarvestTree","StableObjectId",id).GetComponent("HarvestTree");
            Assert.That(Get(restored,"IsAvailable"),Is.False);Assert.That(Get(Call(s,"GetOrCreateNodeState",id),"readyAtWorldHours"),Is.EqualTo(deadline));
        }
        [UnityTest]
        public IEnumerator StreamedEnemyDefeatStoresAWorldDeadlineAndDropsRewardsOnce()
        {
            yield return Boot();var s=Session;yield return TopazTestTravel.EnterWoodland(s.gameObject);
            var enemy=TopazTestTravel.Scout().GetComponent("EnemyCombatant");string id=(string)Get(enemy,"SpawnId");
            int before=(int)Get(s,"PickupCount");Call(enemy,"TakeDamage",100);yield return null;
            var records=((IEnumerable)Get(Get(s,"ActiveWorld"),"enemyRespawns")).Cast<object>().Where(r=>(string)Get(r,"spawnId")==id).ToArray();
            Assert.That(records.Length,Is.EqualTo(1));Assert.That((double)Get(records[0],"readyAtWorldHours"),Is.GreaterThan((double)Get(s,"WorldHours")));
            int after=(int)Get(s,"PickupCount");Assert.That(after,Is.GreaterThan(before));Call(enemy,"TakeDamage",100);Assert.That(Get(s,"PickupCount"),Is.EqualTo(after));
        }
        [UnityTest]
        public IEnumerator StreamedScoutsStaggerWhileGuardianDefencesRemainDistinct()
        {
            yield return Boot();var s=Session;yield return TopazTestTravel.EnterWoodland(s.gameObject);
            var scout=TopazTestTravel.Scout().GetComponent("EnemyCombatant");
            var guardian=Actors().First(e=>Get(e,"LootRole").ToString()=="Warrior");
            Assert.That(scout.GetComponent<NavMeshAgent>().isOnNavMesh,Is.True);
            Call(scout,"StaggerFromWeapon",.4f);Call(guardian,"StaggerFromWeapon",.4f);
            Assert.That(Get(scout,"HasHitReaction"),Is.True);Assert.That(Get(guardian,"HasHitReaction"),Is.False);
        }
        [UnityTest]
        public IEnumerator DefeatReturnsToTheDiscoveredBuiltCampAndAdvancesEightHours()
        {
            yield return Boot();var s=Session;yield return Move(s,new Vector3(160,0,20));
            var pack=Get(s,"_backpack");Call(pack,"Add",Get(s,"WoodItem"),20);Call(pack,"Add",Get(s,"StoneItem"),20);
            Vector3 point=BuildingTestActions.Place(s,"structure.campfire");BuildingTestActions.Teleport(s.gameObject,point+Vector3.forward);yield return null;
            string camp=(string)Get(s,"ReturnCampfireId");Assert.That(camp,Is.Not.EqualTo("campfire.home"));
            double hours=(double)Get(s,"WorldHours");BuildingTestActions.Teleport(s.gameObject,point+Vector3.right*16);yield return null;
            var health=s.GetComponent("PlayerVitality");Assert.That(Call(health,"TryTakeDamage",100),Is.True);yield return WaitRecovery(s);
            Assert.That(Get(s,"ReturnCampfireId"),Is.EqualTo(camp));Assert.That(Vector3.Distance(s.transform.position,point),Is.LessThan(4));
            Assert.That(Get(health,"CurrentHealth"),Is.EqualTo(Get(health,"MaximumHealth")));Assert.That((double)Get(s,"WorldHours")-hours,Is.InRange(8,8.15));
        }
        [UnityTest]
        public IEnumerator DefeatReloadsAnUnloadedBuiltCampBeforeRevealingThePlayer()
        {
            yield return Boot();var s=Session;
            var pack=Get(s,"_backpack");Call(pack,"Add",Get(s,"WoodItem"),20);Call(pack,"Add",Get(s,"StoneItem"),20);
            var point=BuildingTestActions.Place(s,"structure.campfire");BuildingTestActions.Teleport(s.gameObject,point+Vector3.forward);yield return null;
            string id=(string)Get(s,"ReturnCampfireId");Assert.That(id,Is.Not.EqualTo("campfire.home"));
            yield return Move(s,new Vector3(350,0,350));yield return new WaitForSeconds(1);
            Assert.That(((IEnumerable)Get(Get(s,"homeBuilds"),"Campfires")).Cast<object>().Any(c=>(string)Get(c,"StableId")==id),Is.False);
            var health=s.GetComponent("PlayerVitality");Assert.That(Call(health,"TryTakeDamage",100),Is.True);yield return WaitRecovery(s);
            Assert.That(Get(s,"ReturnCampfireId"),Is.EqualTo(id));Assert.That(Vector3.Distance(s.transform.position,point),Is.LessThan(4));
            Assert.That(Call(Get(Get(s,"ActiveRegion"),"Streaming"),"IsReadyAt",s.transform.position),Is.True);
        }
        [UnityTest]
        public IEnumerator MissingRecoveryCampFallsBackToTheStartingFireWithoutLosingExperience()
        {
            yield return Boot();var s=Session;Call(s,"RecordSkillCompletion","logging",50,1);int experience=(int)Get(s,"LoggingExperience");
            var visit=Get(s,"_visit");((IList)Get(visit,"discoveredCampfireIds")).Add("missing-camp");SetField(visit,"lastCampfireId","missing-camp");
            yield return Move(s,new Vector3(150,0,20));var health=s.GetComponent("PlayerVitality");Assert.That(Call(health,"TryTakeDamage",100),Is.True);yield return WaitRecovery(s);
            Assert.That(Get(s,"ReturnCampfireId"),Is.EqualTo("campfire.home"));Assert.That(Get(s,"LoggingExperience"),Is.EqualTo(experience));
        }
        [UnityTest]
        public IEnumerator DiscoveryCachesRefillOnWorldTimeAndRemainIndependent()
        {
            yield return Boot();var s=Session;
            var sites=((IEnumerable)Get(Get(Get(s,"ActiveRegion"),"Wilderness"),"Discoveries")).Cast<object>().OrderBy(site=>(float)Get(site,"X")*(float)Get(site,"X")+(float)Get(site,"Z")*(float)Get(site,"Z")).Take(2).ToArray();
            yield return Move(s,new Vector3(((float)Get(sites[0],"X")+(float)Get(sites[1],"X"))*.5f,0,((float)Get(sites[0],"Z")+(float)Get(sites[1],"Z"))*.5f));
            var caches=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(c=>c.GetType().Name=="DiscoveryCache").Take(2).ToArray();
            Assert.That(caches.Length,Is.EqualTo(2));int wood=(int)Get(s,"WoodCount");
            Assert.That(Call(caches[0],"Open"),Is.True);Assert.That(Get(s,"WoodCount"),Is.EqualTo(wood+3));Assert.That(Call(caches[0],"Open"),Is.False);
            Assert.That(Call(caches[1],"Open"),Is.True);
            var data=Get(s,"_data");SetField(data,"worldHours",(double)Get(s,"WorldHours")+72.1);
            Assert.That(Call(caches[0],"Open"),Is.True);Call(s,"FlushCurrent");
            var saved=Call(Call(Get(s,"_repository"),"Load"),"World",Get(s,"ActiveWorldId"));
            Assert.That(((IEnumerable)Get(saved,"nodes")).Cast<object>().Any(n=>(string)Get(n,"objectId")== (string)Get(caches[0],"StableId") && (double)Get(n,"readyAtWorldHours")>(double)Get(s,"WorldHours")),Is.True);
        }
    }
}
