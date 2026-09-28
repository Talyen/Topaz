using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Topaz.Generation.Editor
{
    public static class OutdoorFoundationDiagnostics
    {
        [Serializable] sealed class Report
        {
            public int seed,version,worldSize,resources,routes,waters,requiredNodes;
            public string profile,content,ownership="floor-based half-open chunks; stable candidate IDs before rejection";
            public string[] requiredIds;
        }
        [MenuItem("Topaz/Generation/Export Outdoor Maps")]
        public static void Export()
        {
            const string directory="TestResults/OutdoorFoundation";Directory.CreateDirectory(directory);
            var plan=new WildernessPlan(42);plan.Validate();
            foreach(bool adjacent in new[]{false,true})
            {
                const int resolution=512;float span=adjacent?256:1024,minimum=-span*.5f;
                foreach(string mode in new[]{"biomes","height","routes-water-reservations"})
                {
                    var texture=new Texture2D(resolution,resolution,TextureFormat.RGB24,false);
                    for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
                    {
                        float px=minimum+x*span/(resolution-1),pz=minimum+z*span/(resolution-1);var biome=plan.Biomes(px,pz);
                        Color color=new Color(.62f,.72f,.35f)*biome.Meadow+new Color(.12f,.37f,.19f)*biome.Woodland+new Color(.6f,.57f,.5f)*biome.Highland;
                        if(mode=="height")color=Color.Lerp(new Color(.08f,.12f,.08f),Color.white,plan.Height(px,pz)/50);
                        if(mode=="routes-water-reservations")
                        {
                            color*=.6f;if(plan.Reserved(px,pz))color=Color.Lerp(color,new Color(.8f,.45f,.2f),.45f);
                            if(plan.RouteDistance(px,pz)<4)color=new Color(.95f,.82f,.48f);
                            if(plan.WaterDepth(px,pz)>0)color=new Color(.13f,.56f,.85f);
                        }
                        if(adjacent && (x==256||z==256))color=Color.white;
                        texture.SetPixel(x,z,color);
                    }
                    texture.Apply();File.WriteAllBytes(Path.Combine(directory,(adjacent?"four-tiles-":"world-")+mode+".png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            int resources=0;for(int z=plan.MinChunk;z<plan.MaxChunk;z++)for(int x=plan.MinChunk;x<plan.MaxChunk;x++)resources+=plan.Resources(new WildernessPlan.Chunk(x,z)).Count;
            File.WriteAllText(Path.Combine(directory,"seed-42.json"),JsonUtility.ToJson(new Report{seed=42,version=WildernessPlan.Version,worldSize=plan.Settings.worldSize,
                profile=plan.Settings.profileId,content=plan.Settings.contentId,resources=resources,routes=plan.Routes.Count,waters=plan.Waters.Count,requiredNodes=plan.RequiredResources.Count,
                requiredIds=plan.Discoveries.Select(s=>s.Id).Concat(plan.RequiredResources.Select(s=>s.Id)).ToArray()},true));
        }
    }
}
