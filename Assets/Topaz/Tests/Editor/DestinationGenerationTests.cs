using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;

namespace Topaz.Tests
{
    public sealed class DestinationGenerationTests
    {
        static readonly Type PlanType=Type.GetType("Topaz.Generation.WildernessPlan, Assembly-CSharp",true);
        static readonly Type SettingsType=Type.GetType("Topaz.Generation.WoodlandSettings, Assembly-CSharp",true);
        static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
        static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
        static object Create(int seed,bool large)
        {
            if(!large)return Activator.CreateInstance(PlanType,seed);
            object settings=SettingsType.GetMethod("LargeWorld").Invoke(null,null);
            return Activator.CreateInstance(PlanType,seed,settings);
        }
        static object[] Sites(object plan)=>((IEnumerable)Property(plan,"Destinations")).Cast<object>().ToArray();

        [TestCase(0),TestCase(1),TestCase(7),TestCase(42),TestCase(63),TestCase(99)]
        public void LargeWorldDestinationsKeepReachableRoutesAndStableIdentities(int seed)
        {
            var first=Create(seed,true);
            Call(first,"Validate");
            var sites=Sites(first);
            Assert.That(sites.Length,Is.EqualTo(18));
            Assert.That(Field(sites[0],"Kind").ToString(),Is.EqualTo("TitansGrave"));
            Assert.That(Field(sites[1],"Kind").ToString(),Is.EqualTo("SplitPeak"));
            Assert.That(sites.Select(site=>Field(site,"Id")).Distinct().Count(),Is.EqualTo(sites.Length));
            foreach(var site in sites)
            {
                float x=(float)Field(site,"X"),z=(float)Field(site,"Z");
                Assert.That((bool)Call(first,"Contains",x,z),Is.True,Field(site,"Id").ToString());
                Assert.That((bool)Call(first,"ContainsChunk",Property(site,"Owner")),Is.True,Field(site,"Id").ToString());
            }
            var again=Sites(Create(seed,true));
            for(int i=0;i<sites.Length;i++)
            {
                foreach(var name in new[]{"Id","Kind","X","Z","Yaw"})
                    Assert.That(Field(sites[i],name),Is.EqualTo(Field(again[i],name)),$"Seed {seed} site {i} {name}");
            }
        }

        [Test]
        public void CompactTestWorldKeepsItsRequiredRoutesWithoutMegastructures()
        {
            var plan=Create(42,false);
            Call(plan,"Validate");
            Assert.That(Sites(plan).Any(site=>Field(site,"Kind").ToString() is "TitansGrave" or "SplitPeak"),Is.False);
        }

        [Test,Category("Stress")]
        public void OneHundredLargeWorldSeedsHaveValidDestinationApproaches()
        {
            for(int seed=0;seed<100;seed++)
            {
                var plan=Create(seed,true);
                Call(plan,"Validate");
                var sites=Sites(plan);
                Assert.That(sites.Length,Is.InRange(2,18),$"Seed {seed}");
                Assert.That(Field(sites[0],"Kind").ToString(),Is.EqualTo("TitansGrave"),$"Seed {seed}");
                Assert.That(Field(sites[1],"Kind").ToString(),Is.EqualTo("SplitPeak"),$"Seed {seed}");
            }
        }
    }
}
