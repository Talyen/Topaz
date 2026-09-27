using System;
using System.Collections.Generic;
using Topaz.Gameplay;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Generation
{
    /// <summary>Scene composition for a generated region. Rules and save ownership stay in WorldSession.</summary>
    public sealed class WoodlandRegion : MonoBehaviour
    {
        public WoodlandPreset preset;
        public string regionId = TopazSaveData.WildernessRegion;
        public Transform arrival;
        public Transform exit;
        public Transform cache;
        public Transform[] encounterActors = Array.Empty<Transform>();
        public HarvestTree[] trees = Array.Empty<HarvestTree>();
        public MiningRock[] rocks = Array.Empty<MiningRock>();
        public ForagePlant[] plants = Array.Empty<ForagePlant>();
        public NavMeshSurface navigation;
        public Vector3 regionOffset;
        public bool Ready { get; private set; }
        public string Failure { get; private set; }
        public double GenerationMilliseconds { get; private set; }
        int seed;
        string worldId;

        public StreamedWilderness Streaming { get; private set; }
        public WildernessPlan Wilderness => Streaming != null ? Streaming.Plan : null;
        public void Generate(TopazWorldData world, Vector3 start = default)
        {
            if (Ready && seed == world.seed && worldId == world.id && Streaming != null && Streaming.Failure == null) return;
            Ready = false;
            if (world.generatorVersion != WildernessPlan.Version)
                throw new InvalidOperationException("Unsupported wilderness generator version.");
            if (Streaming != null) { Streaming.gameObject.SetActive(false); Destroy(Streaming.gameObject); }
            foreach (var tree in trees) if (tree != null) tree.gameObject.SetActive(false);
            foreach (var rock in rocks) if (rock != null) rock.gameObject.SetActive(false);
            foreach (var plant in plants) if (plant != null) plant.gameObject.SetActive(false);
            if (exit != null) exit.gameObject.SetActive(false);
            var root = new GameObject("Streamed Wilderness");root.transform.SetParent(transform,false);
            Streaming = root.AddComponent<StreamedWilderness>();
            var watch=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Streaming.Initialize(this,world,start);
                seed=world.seed;worldId=world.id;Ready=true;Failure=null;
            }
            catch(Exception error) { Failure=error.Message;throw; }
            finally {GenerationMilliseconds=watch.Elapsed.TotalMilliseconds;}
        }
        public static float GroundHeight(Vector3 position)
        {
            foreach (var terrain in Terrain.activeTerrains)
            {
                Vector3 p = position - terrain.transform.position;
                if (p.x >= 0 && p.z >= 0 && p.x <= terrain.terrainData.size.x && p.z <= terrain.terrainData.size.z)
                    return terrain.SampleHeight(position) + terrain.transform.position.y;
            }
            return 0;
        }
    }
}
