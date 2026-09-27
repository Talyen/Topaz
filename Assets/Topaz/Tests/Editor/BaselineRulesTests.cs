using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace Topaz.Tests.Editor
{
    public sealed class BaselineRulesTests
    {
        static Type T(string n) => Type.GetType("Topaz.Gameplay."+n+", Assembly-CSharp",true);
        static object Get(object o,string n)=>o.GetType().GetField(n).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n).SetValue(o,v);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n).Invoke(o,a);
        [TestCase(-20,0,20,0,true)]
        [TestCase(-20,13,20,13,false)]
        [TestCase(0,0,20,0,true)]
        [TestCase(0,0,1,0,true)]
        public void CampBoundaryBlocksCrossingAndInsideAttacks(float ax,float az,float bx,float bz,bool blocked)
        {
            var type=Type.GetType("Topaz.Combat.CampSafety, Assembly-CSharp",true);
            Assert.That(type.GetMethod("Intersects").Invoke(null,new object[]{new Vector3(ax,0,az),new Vector3(bx,0,bz),Vector3.zero,12f}),Is.EqualTo(blocked));
        }
        [Test]
        public void WorldStructuresAcrossChunksKeepHeightContentsAndNewestQueueWins()
        {
            string dir=Path.Combine(Path.GetTempPath(),"TopazBaseline-"+Guid.NewGuid().ToString("N"));
            try
            {
                object profile=Activator.CreateInstance(T("TopazProfileData"));
                object character=Call(profile,"CreateCharacter","rogue"), world=Call(profile,"CreateWorld");
                Call(profile,"GetOrCreateVisit",Get(character,"id"),Get(world,"id"));
                var structures=(IList)Get(world,"structures");
                foreach(float worldX in new[]{4.5f,132.75f})
                {
                    object r=Activator.CreateInstance(T("StructureStateRecord"));
                    Set(r,"instanceId",Guid.NewGuid().ToString("N"));Set(r,"definitionId","structure.storage_chest");
                    Set(r,"regionId","wilderness");Set(r,"x",worldX);Set(r,"y",3.75f);Set(r,"z",8.25f);
                    object slot=Activator.CreateInstance(T("ItemStackRecord"));Set(slot,"itemId","wood");Set(slot,"count",7);
                    ((IList)Get(r,"slots")).Add(slot);structures.Add(r);
                }
                var repo=Activator.CreateInstance(T("ProfileRepository"),dir);
                Call(repo,"QueueSave",profile);Set(world,"worldHours",80d);Call(repo,"QueueSave",profile);
                object loaded=Call(repo,"Load");object saved=((IList)Get(loaded,"worlds"))[0];
                Assert.That(Get(saved,"worldHours"),Is.EqualTo(80d));
                var records=(IList)Get(saved,"structures");Assert.That(records.Count,Is.EqualTo(2));
                Assert.That(Get(records[1],"regionId"),Is.EqualTo("wilderness"));
                Assert.That(Get(records[1],"x"),Is.EqualTo(132.75f));
                Assert.That(Get(records[1],"y"),Is.EqualTo(3.75f));
                Assert.That(Get(((IList)Get(records[1],"slots"))[0],"count"),Is.EqualTo(7));
            }
            finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test]
        public void InvalidStructureRegionOrHeightIsRejected()
        {
            var p=Activator.CreateInstance(T("TopazProfileData"));var w=Call(p,"CreateWorld");
            var r=Activator.CreateInstance(T("StructureStateRecord"));Set(r,"instanceId","test");Set(r,"definitionId","structure.campfire");Set(r,"regionId","deleted-dungeon");
            ((IList)Get(w,"structures")).Add(r);
            Assert.Throws<TargetInvocationException>(()=>Call(p,"Validate"));
            Set(r,"regionId","wilderness");Set(r,"y",float.NaN);
            Assert.Throws<TargetInvocationException>(()=>Call(p,"Validate"));
        }
        [Test]
        public void BuildingMaterialsHaveExplicitUrpShaders()
        {
            var settings=UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/Topaz/Gameplay/Building/Resources/BuildingSettings.asset");
            Assert.That(settings,Is.Not.Null);
            var data=new UnityEditor.SerializedObject(settings);
            foreach(string name in new[]{"surfaceMaterial","emberMaterial"})
            {
                var material=data.FindProperty(name).objectReferenceValue as Material;
                Assert.That(material,Is.Not.Null,name);
                Assert.That(material.shader.name,Does.StartWith("Universal Render Pipeline/"));
            }
        }
    }
}
