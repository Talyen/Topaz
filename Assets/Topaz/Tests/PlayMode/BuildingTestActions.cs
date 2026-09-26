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
            Vector3 origin=(Vector3)builder.GetType().GetProperty("Origin",Private).GetValue(builder);
            var can=builder.GetType().GetMethod("CanPlace",Private);
            var height=builder.GetType().GetMethod("PlacementHeight",Private);
            for(float z=-48;z<=48;z+=1.5f) for(float x=-48;x<=48;x+=1.5f)
            {
                Vector3 p=origin+new Vector3(x,0,z);
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
