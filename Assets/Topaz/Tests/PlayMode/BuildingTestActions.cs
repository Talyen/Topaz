using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace Topaz.Tests
{
    internal static class BuildingTestActions
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static object Field(object o,string n) => o.GetType().GetField(n,Private|BindingFlags.Public).GetValue(o);
        public static Vector3 Place(Component session, string id)
        {
            var builder=(Component)Field(session,"homeBuilds");
            Assert.That((bool)session.GetType().GetMethod("BeginHomeBuild").Invoke(session,new object[]{id}),Is.True);
            Assert.That(((GameObject)Field(builder,"_preview")).GetComponentsInChildren<Renderer>().Length,Is.GreaterThan(0),"Placement needs a visible ghost.");
            Vector3 origin=session.transform.position;
            var can=builder.GetType().GetMethod("CanPlace",Private);
            var height=builder.GetType().GetMethod("PlacementHeight",Private);
            for(float radius=3;radius<=48;radius+=1.5f) for(int step=0;step<32;step++)
            {
                float angle=step*Mathf.PI/16;
                Vector3 p=origin+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                p.y=(float)height.Invoke(builder,new object[]{p,id});
                if(!(bool)can.Invoke(builder,new object[]{id,p,0,null}))continue;
                builder.GetType().GetField("_position",Private).SetValue(builder,p);
                builder.GetType().GetMethod("ConfirmPlacement",Private).Invoke(builder,null);
                Assert.That((bool)builder.GetType().GetProperty("Active").GetValue(builder),Is.False,"Placement must commit successfully.");
                return p;
            }
            Assert.Fail("No valid site found for "+id);
            return default;
        }
        public static void Teleport(GameObject player,Vector3 p)
        {
            var c=player.GetComponent<CharacterController>();c.enabled=false;
            player.transform.position=p+Vector3.up*.1f;c.enabled=true;Physics.SyncTransforms();
        }
    }
}
