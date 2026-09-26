using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace Topaz.Tests
{
    public sealed class WoodlandGenerationTests
    {
        static Type Type(string name)=>System.Type.GetType("Topaz.Generation."+name+", Assembly-CSharp",true);
        static object Settings()=>Activator.CreateInstance(Type("WoodlandSettings"));
        static object Field(object obj,string field)=>obj.GetType().GetField(field).GetValue(obj);
        static void Set(object obj,string field,object value)=>obj.GetType().GetField(field).SetValue(obj,value);
        static object Generate(int seed,string region,object settings)=>Type("WoodlandPlan").GetMethod("Generate").Invoke(null,new[]{(object)seed,region,settings});
        [Test]
        public void IdenticalSeedsReproduceTerrainAndPersistentResourceIds()
        {
            var a=Generate(42,"home",Settings());var b=Generate(42,"home",Settings());
            CollectionAssert.AreEqual((float[,])Field(a,"Heights"),(float[,])Field(b,"Heights"));
            var ar=(IList)Field(a,"Resources");var br=(IList)Field(b,"Resources");
            for(int i=0;i<ar.Count;i++) {Assert.That(Field(ar[i],"id"),Is.EqualTo(Field(br[i],"id")));Assert.That(Field(ar[i],"point"),Is.EqualTo(Field(br[i],"point")));}
        }
        [Test]
        public void DecorationDensityCannotMoveResourcesOrTerrain()
        {
            var settings=Settings();var a=Generate(-721,"expedition.clearing",settings);Set(settings,"decorationCount",0);var b=Generate(-721,"expedition.clearing",settings);
            CollectionAssert.AreEqual((float[,])Field(a,"Heights"),(float[,])Field(b,"Heights"));
            var ar=(IList)Field(a,"Resources");var br=(IList)Field(b,"Resources");
            for(int i=0;i<ar.Count;i++)Assert.That(Field(ar[i],"point"),Is.EqualTo(Field(br[i],"point")));
            Assert.That(((IList)Field(b,"Decorations")).Count,Is.Zero);
        }
        [Test]
        public void SeedBatchHasTraversableRoutesAndFlatHomeBuildingGround()
        {
            for(int seed=0;seed<50;seed++)foreach(string region in new[]{"home","expedition.clearing"})
            {
                var p=Generate(seed,region,Settings());p.GetType().GetMethod("Validate").Invoke(p,null);
                if(region=="home")for(int x=-12;x<=12;x+=3)for(int z=-12;z<=12;z+=3)
                    Assert.That((float)p.GetType().GetMethod("Height").Invoke(p,new object[]{(float)x,(float)z}),Is.LessThan(.05f));
            }
        }
        [Test]
        public void ResourcePadsKeepStandingAndInteractionPositionsLevel()
        {
            for(int seed=0;seed<100;seed++)
            {
                var plan=Generate(seed,"home",Settings());var height=plan.GetType().GetMethod("Height");
                foreach(var resource in (IList)Field(plan,"Resources"))
                {
                    var point=Field(resource,"point");float x=(float)Field(point,"x"),z=(float)Field(point,"z");
                    float center=(float)height.Invoke(plan,new object[]{x,z});
                    foreach(var offset in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
                        Assert.That((float)height.Invoke(plan,new object[]{x+offset.x*1.5f,z+offset.y*1.5f}),Is.EqualTo(center).Within(.03f),"seed "+seed+" "+Field(resource,"id"));
                }
            }
        }
        [Test]
        public void DifferentSeedsChangeLandscapes()
        {
            var a=(float[,])Field(Generate(12,"home",Settings()),"Heights");var b=(float[,])Field(Generate(13,"home",Settings()),"Heights");
            Assert.That(a[10,10],Is.Not.EqualTo(b[10,10]));
        }
        [Test]
        public void InvalidSettingsAndUnknownGeneratorVersionsFailExplicitly()
        {
            var settings=Settings();Set(settings,"version",999);
            Assert.Throws<TargetInvocationException>(()=>Generate(1,"home",settings));
            settings=Settings();Set(settings,"size",float.NaN);
            Assert.Throws<TargetInvocationException>(()=>Generate(1,"home",settings));
            Assert.Throws<TargetInvocationException>(()=>Generate(1,"missing-region",Settings()));
        }
        [Test]
        public void SavedWorldRetainsSeedSettingsAndHarvestState()
        {
            var type=System.Type.GetType("Topaz.Gameplay.TopazWorldData, Assembly-CSharp",true);var world=Activator.CreateInstance(type);
            Set(world,"seed",-12345);Set(Field(world,"generationSettings"),"relief",6f);
            var loaded=JsonUtility.FromJson(JsonUtility.ToJson(world),type);
            Assert.That(Field(loaded,"seed"),Is.EqualTo(-12345));Assert.That(Field(Field(loaded,"generationSettings"),"relief"),Is.EqualTo(6f));
            CollectionAssert.AreEqual((float[,])Field(Generate(-12345,"home",Field(world,"generationSettings")),"Heights"),(float[,])Field(Generate(-12345,"home",Field(loaded,"generationSettings")),"Heights"));
        }
    }
}
