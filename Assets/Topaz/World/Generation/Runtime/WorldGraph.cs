using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Topaz.Generation
{
    public enum AreaKind { ForestBasin, RiverTerraces, RuinedSettlement, HighlandPass, Lakeshore, Landmark }
    public enum PassageKind { WoodedBend, RockCleft, Saddle, Bridge, RuinedGate, ShorePath }

    [Serializable]
    public sealed class AreaRecord
    {
        public string id, label;
        public int seed;
        public AreaKind kind;
        public DestinationKind landmark;
        public WoodlandSettings generationSettings;
        public List<AreaConnection> exits = new List<AreaConnection>();
    }

    [Serializable]
    public sealed class AreaConnection
    {
        public string id, destinationAreaId, destinationExitId;
        public PassageKind kind;
        public float angle;
        public Vector3 Position(float radius) => new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
    }

    /// <summary>Persistent logical topology. Local geometry and decoration use independent seeds.</summary>
    [Serializable]
    public sealed class WorldGraph
    {
        public const int Version = 2;
        public int version = Version;
        public List<AreaRecord> areas = new List<AreaRecord>();
        public AreaRecord Find(string id) => areas?.Find(a => a.id == id);
        public static WorldGraph Create(int seed)
        {
            var result = new WorldGraph();
            string[] ids = {"wilderness","area.river","area.ruins","area.highland","area.shore","area.pines","area.grave","area.peak"};
            string[] labels = {"Pine Hollow","River Terraces","Old Timber Ruins","Highland Basin","Sheltered Lakeshore","Deep Pines","Titan's Grave","Split Sky Peak"};
            AreaKind[] kinds = {AreaKind.ForestBasin,AreaKind.RiverTerraces,AreaKind.RuinedSettlement,AreaKind.HighlandPass,AreaKind.Lakeshore,AreaKind.ForestBasin,AreaKind.Landmark,AreaKind.Landmark};
            for(int i=0;i<ids.Length;i++)result.areas.Add(new AreaRecord {id=ids[i],label=labels[i],kind=kinds[i],seed=AreaSeed(seed,i),landmark=i==7?DestinationKind.SplitPeak:DestinationKind.TitansGrave});
            var rng = new System.Random(AreaSeed(seed,99));
            var ring = new List<int> {0,1,2,3,4,5};
            // Keep the first forest/river journey; vary the rest of the network by seed.
            for(int i=5;i>2;i--){int j=rng.Next(2,i+1);(ring[i],ring[j])=(ring[j],ring[i]);}
            void Link(int a,int b,PassageKind kind)
            {
                var left=result.areas[a];var right=result.areas[b];string edge="pass."+Math.Min(a,b)+"."+Math.Max(a,b);
                left.exits.Add(new AreaConnection{id=edge+".a",destinationAreaId=right.id,destinationExitId=edge+".b",kind=kind});
                right.exits.Add(new AreaConnection{id=edge+".b",destinationAreaId=left.id,destinationExitId=edge+".a",kind=kind});
            }
            for(int i=0;i<ring.Count;i++)Link(ring[i],ring[(i+1)%ring.Count],(PassageKind)(i%6));
            Link(2,6,PassageKind.RuinedGate);Link(3,7,PassageKind.Saddle);
            foreach(var area in result.areas)
            {
                float start=(area.seed & 1023)/1024f*Mathf.PI*2;
                for(int i=0;i<area.exits.Count;i++)area.exits[i].angle=start+i*Mathf.PI*2/area.exits.Count;
                // JsonUtility materializes inline null records on reload. Always record complete recipes.
                area.generationSettings=AreaPlan.ForArea(area,WoodlandSettings.BoundedWorld());
            }
            result.Validate();return result;
        }
        static int AreaSeed(int seed,int index)
        {unchecked{uint v=(uint)seed^((uint)index+1)*0x9e3779b9u;v^=v>>16;v*=0x7feb352du;v^=v>>15;return (int)v;}}
        public void Validate()
        {
            if(version!=Version || areas==null || areas.Count!=8 || areas.Any(a=>a==null || string.IsNullOrEmpty(a.id) || string.IsNullOrEmpty(a.label) || !Enum.IsDefined(typeof(AreaKind),a.kind) || a.exits==null || a.exits.Count<1 || a.exits.Count>4) || areas.Select(a=>a.id).Distinct().Count()!=areas.Count || Find("wilderness")==null)
                throw new ArgumentException("Invalid area graph.");
            var ids=new HashSet<string>();
            foreach(var area in areas)foreach(var exit in area.exits)
            {
                if(exit==null || string.IsNullOrEmpty(exit.id) || !ids.Add(exit.id) || !float.IsFinite(exit.angle) || !Enum.IsDefined(typeof(PassageKind),exit.kind))throw new ArgumentException("Invalid area exit.");
                var peer=Find(exit.destinationAreaId)?.exits.Find(e=>e.id==exit.destinationExitId);
                if(peer==null || peer.destinationAreaId!=area.id || peer.destinationExitId!=exit.id || peer.kind!=exit.kind || exit.destinationAreaId==area.id)throw new ArgumentException("Area exits must have matching return passages.");
            }
            foreach(var area in areas)if(area.generationSettings!=null)
            {
                area.generationSettings.Validate();
                if(!area.generationSettings.boundedAreas || area.generationSettings.areaId!=area.id || area.generationSettings.areaKind!=area.kind)
                    throw new ArgumentException("Recorded area settings do not match their owner.");
            }
            var reached=new HashSet<string>{"wilderness"};var pending=new Queue<string>();pending.Enqueue("wilderness");
            while(pending.Count>0)foreach(var exit in Find(pending.Dequeue()).exits)if(reached.Add(exit.destinationAreaId))pending.Enqueue(exit.destinationAreaId);
            if(reached.Count!=areas.Count || ids.Count/2<areas.Count)throw new ArgumentException("Area graph must be connected with an exploration loop.");
        }
    }
}
