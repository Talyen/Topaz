using System;
using NUnit.Framework;

namespace Topaz.Tests
{
    public sealed class WildernessGenerationTests
    {
        static Type Plan => Type.GetType("Topaz.Generation.WildernessPlan, Assembly-CSharp",true);
        static Type Chunk => Plan.GetNestedType("Chunk");
        static object Field(object obj,string name) => obj.GetType().GetField(name).GetValue(obj);
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
