using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Topaz.Tests
{
    public sealed class WoodlandGenerationTests
    {
        static Type T(string name)=>Type.GetType("Topaz.Generation."+name+", Assembly-CSharp",true);
        static object Field(object obj,string name)=>obj.GetType().GetField(name).GetValue(obj);
        static void Set(object obj,string name,object value)=>obj.GetType().GetField(name).SetValue(obj,value);
        static object Plan(int seed,object settings)=>Activator.CreateInstance(T("WildernessPlan"),new[]{(object)seed,settings});
        [Test]
        public void HundredSeedsHaveTraversableDiscoveryApproaches()
        {
            for(int seed=0;seed<100;seed++)T("WildernessPlan").GetMethod("Validate").Invoke(Plan(seed,Activator.CreateInstance(T("WoodlandSettings"))),null);
        }
        [Test]
        public void InvalidSettingsAndUnknownVersionsFailExplicitly()
        {
            var settings=Activator.CreateInstance(T("WoodlandSettings"));Set(settings,"version",999);
            Assert.Throws<TargetInvocationException>(()=>Plan(1,settings));
            settings=Activator.CreateInstance(T("WoodlandSettings"));Set(settings,"size",float.NaN);
            Assert.Throws<TargetInvocationException>(()=>Plan(1,settings));
            settings=Activator.CreateInstance(T("WoodlandSettings"));Set(settings,"resolution",129);
            Assert.Throws<TargetInvocationException>(()=>Plan(1,settings));
        }
        [Test]
        public void SavedWorldRetainsSeedSettingsAndTerrainAfterRoundTrip()
        {
            var type=Type.GetType("Topaz.Gameplay.TopazWorldData, Assembly-CSharp",true);var world=Activator.CreateInstance(type);
            Set(world,"seed",-12345);Set(Field(world,"generationSettings"),"relief",6f);
            var loaded=JsonUtility.FromJson(JsonUtility.ToJson(world),type);
            Assert.That(Field(loaded,"seed"),Is.EqualTo(-12345));Assert.That(Field(Field(loaded,"generationSettings"),"relief"),Is.EqualTo(6f));
            var a=Plan(-12345,Field(world,"generationSettings"));var b=Plan(-12345,Field(loaded,"generationSettings"));
            var height=T("WildernessPlan").GetMethod("Height");
            for(float z=-500;z<=500;z+=20)for(float x=-500;x<=500;x+=20)
                Assert.That(height.Invoke(a,new object[]{x,z}),Is.EqualTo(height.Invoke(b,new object[]{x,z})));
            Set(Field(world,"generationSettings"),"relief",18f);
            Assert.That(Field(Field(a,"Settings"),"relief"),Is.EqualTo(6f),"Generated plans own an immutable settings snapshot.");
        }
        [Test]
        public void DifferentSeedsChangeLandscapes()
        {
            var height=T("WildernessPlan").GetMethod("Height");
            Assert.That(height.Invoke(Plan(12,Activator.CreateInstance(T("WoodlandSettings"))),new object[]{80f,80f}),
                Is.Not.EqualTo(height.Invoke(Plan(13,Activator.CreateInstance(T("WoodlandSettings"))),new object[]{80f,80f})));
        }
    }
}
