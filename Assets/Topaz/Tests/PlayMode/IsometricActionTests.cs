using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class IsometricActionTests : TopazInputTestFixture
    {
        [UnityTest]
        public IEnumerator BoltsRetainElevationAndStillHitAnObstructingWall()
        {
            yield return LoadTitleWithoutWorld();
            var owner=GameObject.Find("Player").GetComponent("WorldSession");
            var attack=ScriptableObject.CreateInstance(Type.GetType("Topaz.Combat.CrossbowAttackDefinition, Assembly-CSharp",true));
            var root=new GameObject("Elevated Bolt Regression");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var bolt=root.AddComponent(Type.GetType("Topaz.Combat.CrossbowBolt, Assembly-CSharp",true));
                Vector3 origin=new Vector3(50,40,50),direction=new Vector3(0,.5f,1).normalized;
                var launch=bolt.GetType().GetMethod("Launch");
                launch.Invoke(bolt,new object[]{attack,origin,direction,1,null,owner,false,0f});
                Assert.That(Vector3.Dot(root.transform.forward,direction),Is.GreaterThan(.999f),"Uphill aim must survive bolt launch.");
                launch.Invoke(bolt,new object[]{attack,origin,-direction,1,null,owner,false,0f});
                Assert.That(root.transform.forward.y,Is.LessThan(-.4f),"Downhill aim must survive bolt launch.");
                launch.Invoke(bolt,new object[]{attack,origin,direction,1,null,owner,false,0f});
                wall.transform.position=origin+direction*2;wall.transform.localScale=Vector3.one*1.5f;
                Physics.SyncTransforms();
                yield return new WaitForSeconds(.3f);
                Assert.That(root==null,Is.True,"A bolt must be stopped by physical scenery even when that scenery can be cut away visually.");
            }
            finally{if(root!=null)UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(attack);}
        }
        [UnityTest]
        public IEnumerator AimingAndZoomDoNotRotateTheFixedCamera()
        {
            yield return LoadTitleWithoutWorld();
            var camera=GameObject.Find("Main Camera");var rig=camera.GetComponent("PlayerCamera");
            Quaternion initial=camera.transform.rotation;
            var pad=InputSystem.AddDevice<Gamepad>();Set(pad.rightStick,Vector2.left);
            rig.GetType().GetMethod("SetZoom").Invoke(rig,new object[]{38f});
            rig.GetType().GetMethod("LookAtPoint").Invoke(rig,new object[]{Vector3.right*100});
            yield return new WaitForSeconds(.25f);
            Assert.That(Quaternion.Angle(initial,camera.transform.rotation),Is.LessThan(.01f));
            Assert.That((float)rig.GetType().GetProperty("CurrentZoom").GetValue(rig),Is.EqualTo(38));
            Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator LongFallsRemainInFrameAndSmallTeleportsResetFollow()
        {
            yield return LoadTitleWithoutWorld();
            var player=GameObject.Find("Player");
            ((Behaviour)player.GetComponent("PlayerController")).enabled=false;
            player.GetComponent<CharacterController>().enabled=false;
            var camera=GameObject.Find("Main Camera").GetComponent<Camera>();
            var rig=camera.GetComponent("PlayerCamera");
            Vector3 start=player.transform.position;
            for(int i=1;i<=10;i++)
            {
                player.transform.position=start+Vector3.down*(i*2);
                yield return new WaitForSeconds(.1f);
            }
            Vector3 screen=camera.WorldToViewportPoint(player.transform.position+Vector3.up);
            Assert.That(screen.z,Is.GreaterThan(0));
            Assert.That(screen.y,Is.InRange(.1f,.9f),"An airborne descent must not leave the fixed camera behind above the cliff.");
            player.transform.position+=Vector3.right*3;
            rig.GetType().GetMethod("ResetFollow").Invoke(rig,null);
            yield return null;yield return null;
            screen=camera.WorldToViewportPoint(player.transform.position+Vector3.up*1.3f);
            Assert.That(screen.x,Is.EqualTo(.5f).Within(.035f));
            Assert.That(screen.y,Is.EqualTo(.5f).Within(.035f));
        }
    }
}
