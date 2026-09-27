using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Topaz.Tests
{
    /// <summary>Failed coroutine fixtures must not leak their save-directory override into another test.</summary>
    public class TopazInputTestFixture : InputTestFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            TopazTestTravel.Reset();
            Type.GetType("Topaz.Gameplay.WorldSession, Assembly-CSharp",true)
                .GetProperty("EditorTestSaveDirectory").SetValue(null,null);
            Time.timeScale=1;
        }
        [TearDown]
        public override void TearDown()
        {
            Type.GetType("Topaz.Gameplay.WorldSession, Assembly-CSharp",true)
                .GetProperty("EditorTestSaveDirectory").SetValue(null,null);
            base.TearDown();
        }
        internal static IEnumerator WaitForWilderness()
        {
            float deadline=Time.realtimeSinceStartup+40;
            while(Time.realtimeSinceStartup<deadline)
            {
                var session=GameObject.Find("Player")?.GetComponent("WorldSession");
                if(session!=null && (bool)session.GetType().GetProperty("HasActivePair").GetValue(session))
                {
                    var region=session.GetType().GetProperty("ActiveRegion").GetValue(session);
                    var stream=region?.GetType().GetProperty("Streaming").GetValue(region);
                    if(stream!=null && (bool)stream.GetType().GetProperty("InitialReady").GetValue(stream)) {yield return null;yield break;}
                }
                yield return null;
            }
            Assert.Fail("The wilderness did not become ready within 40 seconds.");
        }

        protected static GameObject FindResource(string type,string property=null,object expected=null)
        {
            var player=GameObject.Find("Player");
            var found=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c=>c.GetType().Name==type && (property==null || Equals(c.GetType().GetProperty(property).GetValue(c),expected)))
                .OrderBy(c=>(c.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
            Assert.That(found,Is.Not.Null,"Expected a loaded "+type);
            return found.gameObject;
        }

        protected static void AimAt(Vector3 point)
        {
            var rig=Camera.main.GetComponent("PlayerCamera");
            rig.GetType().GetMethod("LookAtPoint").Invoke(rig,new object[]{point});
        }
    }
}
