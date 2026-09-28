using System;
using System.Collections.Generic;
using UnityEngine;

namespace Topaz.Generation
{
    public sealed partial class WildernessPlan
    {
        readonly struct HeightRouteSegment
        {
            public readonly Vector3 A,B;
            public HeightRouteSegment(Vector3 a,Vector3 b){A=a;B=b;}
        }
        readonly Dictionary<Chunk,HeightRouteSegment[]> heightRoutes;
        Dictionary<Chunk,HeightRouteSegment[]> IndexHeightRoutes()
        {
            var cells=new Dictionary<Chunk,List<HeightRouteSegment>>();
            // Height stamps have a bounded 20 m influence. Keep original segment order
            // in each cell so weighted sums retain exactly the exhaustive sampler's arithmetic.
            foreach(var route in routes)for(int i=1;i<route.Points.Count;i++)
            {
                var a=route.Points[i-1];var b=route.Points[i];
                var min=Chunk.At(Math.Min(a.x,b.x)-20,Math.Min(a.z,b.z)-20);
                var max=Chunk.At(Math.Max(a.x,b.x)+20,Math.Max(a.z,b.z)+20);
                for(int z=min.Z;z<=max.Z;z++)for(int x=min.X;x<=max.X;x++)
                {
                    var key=new Chunk(x,z);
                    if(!cells.TryGetValue(key,out var segments)){segments=new List<HeightRouteSegment>();cells.Add(key,segments);}
                    segments.Add(new HeightRouteSegment(a,b));
                }
            }
            var result=new Dictionary<Chunk,HeightRouteSegment[]>();
            foreach(var cell in cells)result.Add(cell.Key,cell.Value.ToArray());
            return result;
        }
    }
}
