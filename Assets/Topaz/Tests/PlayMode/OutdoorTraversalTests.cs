using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Topaz.Tests
{
    public sealed class OutdoorTraversalTests : TopazInputTestFixture
    {
        static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
        static object F(object o,string n)=>o.GetType().GetField(n).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
        [UnityTest, Category("Stress"), Timeout(1200000)]
        public IEnumerator RequiredApproachesFitTheActualControllerAcrossFixedSeeds()
        {
            yield return ValidateApproaches(new[]{0,1,7,42,63,99});
        }
        [UnityTest]
        public IEnumerator RequiredApproachesFitTheActualControllerAtRepresentativeSeed()
        {
            yield return ValidateApproaches(new[]{42});
        }
        static IEnumerator ValidateApproaches(int[] seeds)
        {
            foreach(int seed in seeds)
            {
                float seedStarted=Time.realtimeSinceStartup;
                Debug.Log($"[Topaz/Traversal] Begin seed {seed}");
                Type.GetType("Topaz.Gameplay.WorldSession, Assembly-CSharp",true).GetProperty("EditorTestSeed").SetValue(null,seed);
                yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
                var player=GameObject.Find("Player");var controller=player.GetComponent<CharacterController>();
                Assert.That(controller.height,Is.EqualTo(1.8f).Within(.01));Assert.That(controller.radius,Is.EqualTo(.36f).Within(.01));
                Assert.That(controller.slopeLimit,Is.EqualTo(45));Assert.That(controller.stepOffset,Is.EqualTo(.25f));
                var movement=(Behaviour)player.GetComponent("PlayerController");movement.enabled=false;
                var session=player.GetComponent("WorldSession");var stream=P(P(session,"ActiveRegion"),"Streaming");var plan=P(stream,"Plan");
                foreach(var route in ((IEnumerable)P(plan,"Routes")).Cast<object>().Take(3))
                {
                    var points=((IEnumerable)F(route,"Points")).Cast<Vector3>().ToArray();
                    yield return (IEnumerator)Call(stream,"PrepareDestination",points[0]);
                    BuildingTestActions.Teleport(player,points[0]);
                    for(int segment=1;segment<points.Length;segment++)
                    {
                        Vector3 a=points[segment-1],b=points[segment];
                        // Populate collision and navigation for each bounded section, then sweep rather than teleport it.
                        yield return (IEnumerator)Call(stream,"PrepareDestination",a);
                        int steps=Mathf.CeilToInt(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z))/.35f);
                        for(int n=1;n<=steps;n++)
                        {
                            Vector3 target=Vector3.Lerp(a,b,n/(float)steps);target.y=Ground(target)+.015f;
                            Vector3 delta=target-player.transform.position;
                            controller.Move(new Vector3(delta.x,Mathf.Max(0,delta.y),delta.z));controller.Move(Vector3.down*.5f);
                            Assert.That(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(target.x,target.z)),Is.LessThan(.5f),$"Seed {seed}, {F(route,"Id")}, section {segment}, target {target}; nearby: {string.Join(",",Physics.OverlapSphere(target+Vector3.up,.9f).Select(c=>c.name))}");
                            Assert.That(player.transform.position.y,Is.GreaterThan(Ground(player.transform.position)-.15f));
                            if(n%20==0)yield return null;
                        }
                    }
                    Assert.That(NavMesh.SamplePosition(player.transform.position,out var hit,2,NavMesh.AllAreas),Is.True,$"Seed {seed} destination navigation.");
                }
                movement.enabled=true;
                Debug.Log($"[Topaz/Traversal] Seed {seed} passed in {Time.realtimeSinceStartup-seedStarted:F1}s");
            }
        }
        static float Ground(Vector3 point)
        {
            var terrain=Terrain.activeTerrains.First(t=>point.x>=t.transform.position.x&&point.x<t.transform.position.x+128&&point.z>=t.transform.position.z&&point.z<t.transform.position.z+128);
            return terrain.SampleHeight(point)+terrain.transform.position.y;
        }
    }
}
