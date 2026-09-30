using System;
using System.Collections.Generic;
using Topaz.Generation;

namespace Topaz.Gameplay
{
    /// <summary>Indexes saved storage; material availability never depends on loaded chest visuals.</summary>
    public sealed class WorldStorageIndex
    {
        public const float Radius = 30f;
        readonly Dictionary<WildernessPlan.Chunk,List<StructureStateRecord>> cells = new Dictionary<WildernessPlan.Chunk,List<StructureStateRecord>>();
        public void Rebuild(IEnumerable<StructureStateRecord> structures)
        {
            cells.Clear();
            foreach(var record in structures)
            {
                if(record.definitionId!=BuildCatalog.Chest)continue;
                var key=WildernessPlan.Chunk.At(record.x,record.z);
                if(!cells.TryGetValue(key,out var list))cells.Add(key,list=new List<StructureStateRecord>());
                list.Add(record);
            }
        }
        public IEnumerable<StructureStateRecord> Nearby(float x,float z,string areaId=TopazSaveData.HomeRegion)
        {
            var min=WildernessPlan.Chunk.At(x-Radius,z-Radius);var max=WildernessPlan.Chunk.At(x+Radius,z+Radius);
            for(int cz=min.Z;cz<=max.Z;cz++)for(int cx=min.X;cx<=max.X;cx++)
                if(cells.TryGetValue(new WildernessPlan.Chunk(cx,cz),out var list))
                    foreach(var r in list)if(r.regionId==areaId && (r.x-x)*(r.x-x)+(r.z-z)*(r.z-z)<=Radius*Radius)yield return r;
        }
    }
}
