using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Topaz.Tests
{
    /// <summary>Scenario travel uses the production streamer; tests no longer load a separate region.</summary>
    internal static class TopazTestTravel
    {
        static GameObject scout;
        internal static void Reset()=>scout=null;
        internal static GameObject Scout()
        {Assert.That(scout,Is.Not.Null,"Enter a generated encounter before querying its actor.");return scout;}
        public static IEnumerator EnterWoodland(GameObject player)
        {
            yield return TopazInputTestFixture.WaitForWilderness();
            var session=player.GetComponent("WorldSession");
            var region=Property(session,"ActiveRegion");var plan=Property(region,"Wilderness");
            var sites=((IEnumerable)plan.GetType().GetField("Discoveries").GetValue(plan)).Cast<object>();
            var site=sites.OrderBy(s=>(float)Field(s,"X")*(float)Field(s,"X")+(float)Field(s,"Z")*(float)Field(s,"Z")).First();
            float x=(float)Field(site,"X"),z=(float)Field(site,"Z");
            float y=(float)plan.GetType().GetMethod("Height").Invoke(plan,new object[]{x,z});
            var stream=(Component)Property(region,"Streaming");
            yield return (IEnumerator)stream.GetType().GetMethod("PrepareDestination").Invoke(stream,new object[]{new Vector3(x,y,z)});
            BuildingTestActions.Teleport(player,new Vector3(x,y,z));
            yield return null;
            var actors=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c=>c.GetType().Name=="EnemyCombatant" && (bool)Property(c,"IsAlive") && Property(c,"LootRole").ToString()=="Minion" && c.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh)
                .OrderBy(c=>(c.transform.position-player.transform.position).sqrMagnitude).ToArray();
            Assert.That(actors.Length,Is.GreaterThan(0));scout=actors[0].gameObject;
            Assert.That(Property(session,"CurrentRegionId"),Is.EqualTo("wilderness"));
            Assert.That(Property(session,"BlockMovement"),Is.False);
        }
        static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
        static object Property(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
    }
}
