using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace Topaz.Tests
{
    public sealed class WildernessGenerationTests
    {
        static Type Plan => Type.GetType("Topaz.Generation.WildernessPlan, Assembly-CSharp",true);
        static Type Chunk => Plan.GetNestedType("Chunk");
        static object NewPlan(int seed) => Activator.CreateInstance(Plan,new object[]{seed});
        static object NewChunk(int x,int z) => Activator.CreateInstance(Chunk,new object[]{x,z});
        static object Call(object plan,string method,params object[] args) => Plan.GetMethod(method).Invoke(plan,args);
        static object Field(object obj,string name) => obj.GetType().GetField(name).GetValue(obj);
        [Test]
        public void HundredSeedsHaveMatchingChunkEdgesAndStableResources()
        {
            for(int seed=0;seed<100;seed++)
            {
                var plan=NewPlan(seed);var copy=NewPlan(seed);
                for(int z=-4;z<4;z++)for(int x=-4;x<4;x++)
                {
                    var chunk=NewChunk(x,z);
                    var heights=(float[,])Call(plan,"Heights",chunk,17);
                    CollectionAssert.AreEqual(heights,(float[,])Call(copy,"Heights",chunk,17));
                    if(x<3)
                    {
                        var right=(float[,])Call(plan,"Heights",NewChunk(x+1,z),17);
                        for(int n=0;n<17;n++)Assert.That(heights[n,16],Is.EqualTo(right[n,0]));
                    }
                    if(z<3)
                    {
                        var top=(float[,])Call(plan,"Heights",NewChunk(x,z+1),17);
                        for(int n=0;n<17;n++)Assert.That(heights[16,n],Is.EqualTo(top[0,n]));
                    }
                    var resources=(IList)Call(plan,"Resources",chunk);
                    Call(plan,"Decorations",chunk,0);Call(plan,"Decorations",chunk,300);
                    var again=(IList)Call(plan,"Resources",chunk);
                    CollectionAssert.AreEqual(resources,again);
                    foreach(var resource in resources)
                    {
                        float px=(float)Field(resource,"X"),pz=(float)Field(resource,"Z");
                        Assert.That(px,Is.InRange(x*128f,(x+1)*128f));
                        Assert.That(pz,Is.InRange(z*128f,(z+1)*128f));
                    }
                }
                Assert.That((float)Call(plan,"Height",0f,0f),Is.Zero);
                foreach(var site in (IList)Plan.GetField("Discoveries").GetValue(plan))
                {
                    float x=(float)Field(site,"X"),z=(float)Field(site,"Z");
                    Assert.That((float)Call(plan,"Height",x+2,z),Is.EqualTo((float)Call(plan,"Height",x,z)).Within(.001));
                }
            }
        }
        [Test]
        public void ChunkCoordinatesUseFloorAcrossNegativeBoundaries()
        {
            foreach(var sample in new[]{(-.01f,-1),(0f,0),(127.99f,0),(128f,1),(-128f,-1),(-128.01f,-2)})
            {
                var chunk=Chunk.GetMethod("At").Invoke(null,new object[]{sample.Item1,0f});
                Assert.That(Field(chunk,"X"),Is.EqualTo(sample.Item2));
            }
        }
    }
}
