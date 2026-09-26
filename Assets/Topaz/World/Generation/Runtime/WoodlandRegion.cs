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
        public string regionId = "home";
        public Transform arrival;
        public Transform exit;
        public Transform cache;
        public Transform[] encounterActors = Array.Empty<Transform>();
        public HarvestTree[] trees = Array.Empty<HarvestTree>();
        public MiningRock[] rocks = Array.Empty<MiningRock>();
        public ForagePlant[] plants = Array.Empty<ForagePlant>();
        public NavMeshSurface navigation;
        public Vector3 regionOffset;
        public WoodlandPlan Plan { get; private set; }
        public bool Ready { get; private set; }
        public string Failure { get; private set; }
        public double GenerationMilliseconds { get; private set; }
        GameObject generated;
        TerrainData terrainData;
        UnityEngine.AI.NavMeshData generatedNavigation;
        int seed;
        string worldId;

        public void Generate(TopazWorldData world)
        {
            world.generationSettings.Validate();
            if (Ready && seed == world.seed && worldId == world.id && world.generatorVersion == WoodlandPlan.Version) return;
            Ready = false;
            if (world.generatorVersion != WoodlandPlan.Version)
                throw new InvalidOperationException("This world requires woodland generator " + world.generatorVersion);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Plan = WoodlandPlan.Generate(world.seed, regionId, world.generationSettings);
                if (preset == null || preset.grass == null || preset.path == null || preset.terrainMaterial == null)
                    throw new InvalidOperationException("Woodland terrain assets are not configured.");
                if (generated != null) { generated.SetActive(false); Destroy(generated); }
                if (terrainData != null) Destroy(terrainData);
                generated = new GameObject("Generated Woodland " + regionId);
                SceneManager.MoveGameObjectToScene(generated, gameObject.scene);
                generated.transform.SetParent(transform, false);
                var s = Plan.Settings;
                terrainData = new TerrainData { name = "Seed " + world.seed, heightmapResolution = s.resolution,
                    size = new Vector3(s.size, 16, s.size), alphamapResolution = 128 };
                var heights = new float[s.resolution, s.resolution];
                for (int z = 0; z < s.resolution; z++)
                for (int x = 0; x < s.resolution; x++) heights[z, x] = Plan.Heights[z, x] / 16;
                terrainData.SetHeights(0, 0, heights);
                terrainData.terrainLayers = new[] { preset.grass, preset.path };
                var paint = new float[128, 128, 2];
                for (int z = 0; z < 128; z++)
                for (int x = 0; x < 128; x++)
                {
                    var point = new WoodlandPlan.Point((x / 127f - .5f) * s.size, (z / 127f - .5f) * s.size);
                    float path = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.2f, 3.2f, Plan.DistanceToRoute(point)));
                    paint[z, x, 0] = 1 - path; paint[z, x, 1] = path;
                }
                terrainData.SetAlphamaps(0, 0, paint);
                if (preset.detail != null)
                {
                    terrainData.SetDetailResolution(128, 16);
                    terrainData.detailPrototypes = new[] { new DetailPrototype { prototype = preset.detail, usePrototypeMesh = true, useInstancing = true, renderMode = DetailRenderMode.VertexLit, minWidth = .7f, maxWidth = 1.2f, minHeight = .7f, maxHeight = 1.2f, healthyColor = Color.white, dryColor = Color.white } };
                    var density = new int[128,128];
                    for (int z=0;z<128;z++) for(int x=0;x<128;x++)
                    {
                        float px=(x/127f-.5f)*s.size,pz=(z/127f-.5f)*s.size;
                        if (paint[z,x,1] < .1f && px*px+pz*pz > 24*24 && ((x*73+z*139+world.seed)&7)<2) density[z,x]=1;
                    }
                    terrainData.SetDetailLayer(0,0,0,density);
                }

                var ground = Terrain.CreateTerrainGameObject(terrainData);
                ground.name = "Seeded Terrain";
                ground.transform.SetParent(generated.transform, false);
                ground.transform.position = regionOffset + new Vector3(-s.size / 2, 0, -s.size / 2);
                var terrain = ground.GetComponent<Terrain>();
                terrain.materialTemplate = preset.terrainMaterial;
                terrain.drawInstanced = true; terrain.heightmapPixelError = 8; terrain.detailObjectDistance = 45;
                foreach (var p in Plan.Decorations)
                {
                    var prefab = p.rock ? preset.rockVisual : preset.treeVariant != null && p.point.x > 0 ? preset.treeVariant : preset.treeVisual;
                    if (prefab == null) continue;
                    var visual = Instantiate(prefab, Position(p.point), Quaternion.Euler(0, p.point.x * 17, 0), generated.transform);
                    visual.name = p.id; visual.transform.localScale *= p.scale;
                }
                var treeSites=Plan.Resources.FindAll(p=>!p.rock);
                for(int i=0;i<treeSites.Count && trees.Length>0;i++)
                {
                    HarvestTree tree=i<trees.Length?trees[i]:Instantiate(trees[0],generated.transform);
                    if(tree==null)continue;
                    tree.transform.position=Position(treeSites[i].point);
                    tree.SetGeneratedId(regionId+".tree."+i);
                }
                var rockSites=Plan.Resources.FindAll(p=>p.rock);
                for(int i=0;i<rockSites.Count && rocks.Length>0;i++)
                {
                    MiningRock rock=i<rocks.Length?rocks[i]:Instantiate(rocks[0],generated.transform);
                    if(rock==null)continue;
                    rock.transform.position=Position(rockSites[i].point);
                    rock.SetGeneratedId(regionId+".rock."+i);
                }
                for (int i = 0; i < plants.Length; i++)
                    if (plants[i] != null) plants[i].transform.position = Position(new WoodlandPlan.Point(5 + i * 2, -8));
                if (arrival != null) arrival.position = Position(Plan.Entry) + Vector3.forward * 3;
                if (exit != null) exit.position = Position(regionId == "home" ? Plan.Exit : Plan.Entry);
                if(cache!=null)cache.position=Position(new WoodlandPlan.Point(Plan.Encounter.x-3,Plan.Encounter.z));
                for (int i = 0; i < encounterActors.Length; i++)
                    if (encounterActors[i] != null) encounterActors[i].position = Position(new WoodlandPlan.Point(Plan.Encounter.x+i*3,Plan.Encounter.z));
                CreateBoundary(s.size);
                Physics.SyncTransforms();
                if (navigation != null)
                {
                    navigation.RemoveData();
                    navigation.collectObjects = CollectObjects.Volume;
                    navigation.center = navigation.transform.InverseTransformPoint(regionOffset + Vector3.up * 8);
                    navigation.size = new Vector3(s.size, 40, s.size);
                    navigation.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
                    if (generatedNavigation != null) Destroy(generatedNavigation);
                    navigation.BuildNavMesh();
                    generatedNavigation = navigation.navMeshData;
                    if (navigation.navMeshData == null) throw new InvalidOperationException("Navigation generation failed.");
                }
                seed = world.seed; worldId = world.id; Ready = true; Failure = null;
            }
            catch (Exception error) { Failure = error.Message; throw; }
            finally { GenerationMilliseconds = watch.Elapsed.TotalMilliseconds; }
        }
        Vector3 Position(WoodlandPlan.Point p) => regionOffset + new Vector3(p.x, Plan.Height(p.x, p.z), p.z);
        void CreateBoundary(float size)
        {
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Region Boundary", typeof(BoxCollider));
                go.transform.SetParent(generated.transform, false);
                bool side = i < 2;
                go.transform.position = regionOffset + (side ? new Vector3((i == 0 ? -1 : 1) * size / 2, 8, 0) : new Vector3(0, 8, (i == 2 ? -1 : 1) * size / 2));
                go.GetComponent<BoxCollider>().size = side ? new Vector3(2, 24, size) : new Vector3(size, 24, 2);
            }
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
        void OnDestroy() { if (terrainData != null) Destroy(terrainData); if (generatedNavigation != null) Destroy(generatedNavigation); }
    }
}
