using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Topaz.Combat;
using Topaz.Gameplay;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling;

namespace Topaz.Generation
{
    /// <summary>Owns only loaded presentation/physics. WorldSession owns all persistent state.</summary>
    public sealed partial class StreamedWilderness : MonoBehaviour
    {
        sealed class LoadedChunk
        {
            public GameObject Root;
            public Terrain Terrain;
            public TerrainData Data;
            public bool Populated;
            public bool NavigationReady;
            public int[][,] BaseDensity;
            public readonly List<GameObject> Decorations=new List<GameObject>();
            public readonly HashSet<string> Sites=new HashSet<string>();
        }
        static readonly ProfilerMarker TerrainHeightsMarker=new ProfilerMarker("Topaz.Streaming.TerrainHeights");
        static readonly ProfilerMarker TerrainPaintMarker=new ProfilerMarker("Topaz.Streaming.TerrainPaint");
        static readonly ProfilerMarker SpawnMarker=new ProfilerMarker("Topaz.Streaming.DecorationActivation");
        readonly Dictionary<GameObject,Stack<GameObject>> decorationPools=new Dictionary<GameObject,Stack<GameObject>>();
        readonly Dictionary<GameObject,GameObject> decorationSources=new Dictionary<GameObject,GameObject>();
        Transform poolRoot;
        readonly Dictionary<WildernessPlan.Chunk,LoadedChunk> loaded = new Dictionary<WildernessPlan.Chunk,LoadedChunk>();
        sealed class TerrainSlot { public Terrain Terrain; public TerrainData Data; }
        readonly Stack<TerrainSlot> terrainPool=new Stack<TerrainSlot>();
        public int PooledTerrainCount=>terrainPool.Count;
        GroundCoverPrototype[] groundCover;
        TerrainSlot CreateTerrainSlot()
        {
            var data=new TerrainData {heightmapResolution=65,size=new Vector3(128,WildernessPlan.TerrainHeight,128),alphamapResolution=64};
            data.terrainLayers=runtimeLayers;
            if(groundCover.Length>0)
            {
                data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);data.SetDetailResolution(64,16);
                data.detailPrototypes=groundCover.Select(entry=>new DetailPrototype{prototype=entry.prefab,usePrototypeMesh=true,useInstancing=true,renderMode=DetailRenderMode.VertexLit,minWidth=entry.width.x,maxWidth=entry.width.y,minHeight=entry.height.x,maxHeight=entry.height.y,healthyColor=Color.white,dryColor=Color.white}).ToArray();
            }
            var go=Terrain.CreateTerrainGameObject(data);go.SetActive(false);go.transform.SetParent(poolRoot,false);
            return new TerrainSlot{Terrain=go.GetComponent<Terrain>(),Data=data};
        }
        void ReturnTerrain(Terrain terrain,TerrainData data)
        {
            if(terrain==null){if(data!=null)Destroy(data);return;}
            terrain.SetNeighbors(null,null,null,null);terrain.gameObject.SetActive(false);terrain.transform.SetParent(poolRoot,false);
            if(terrainPool.Count<8)terrainPool.Push(new TerrainSlot{Terrain=terrain,Data=data});
            else {Destroy(terrain.gameObject);Destroy(data);}
        }
        sealed class PreparedTerrain
        {
            public readonly float[,] Heights=new float[65,65];
            public readonly float[,,] Paint=new float[64,64,4];
            public readonly int[][,] Density;
            public PreparedTerrain(int details)
            { Density=new int[details][,];for(int i=0;i<details;i++)Density[i]=new int[64,64]; }
        }
        readonly Dictionary<WildernessPlan.Chunk,PreparedTerrain> preload = new Dictionary<WildernessPlan.Chunk,PreparedTerrain>();
        readonly Dictionary<WildernessPlan.Chunk,GameObject> distant = new Dictionary<WildernessPlan.Chunk,GameObject>();
        readonly Dictionary<string,GameObject> distantDestinations = new Dictionary<string,GameObject>();
        readonly Dictionary<WildernessPlan.Chunk,GameObject> distantTrees = new Dictionary<WildernessPlan.Chunk,GameObject>();
        readonly Dictionary<WildernessPlan.Chunk,Dictionary<string,GameObject>> canopyInstances = new Dictionary<WildernessPlan.Chunk,Dictionary<string,GameObject>>();
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<EnemyCombatant> enemies = new List<EnemyCombatant>();
        static WildernessPlan cachedPlan;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPlanCache()=>cachedPlan=null;
        WoodlandRegion owner;
        WorldSession session;
        Transform player;
        WildernessPlan.Chunk center;
        NavMeshData navData;
        AsyncOperation navigationOperation;
        Coroutine worker;
        bool navigationDirty;
        bool terrainInstancing,terrainShadows=true;
        bool building;
        bool disposed,preparingDestination;
        Color[] groundColors;
        TerrainLayer[] runtimeLayers;
        Topaz.Rendering.VisualLookController look;
        float nextSurfaceUpdate,lastWetness=-1;
        int requestEpoch;
        Bounds navigationBounds;
        public float PeakTerrainCommitMs { get; private set; }
        public float PeakPreparationSliceMs { get; private set; }
        public float PeakNavigationSubmitMs { get; private set; }
        public int RequestEpoch => requestEpoch;
        public int DistantCount => distantTrees.Count;
        public int DistantTerrainCount => distant.Count;
        public int PreloadedCount => preload.Count;
        public int OwnedMeshCount => meshes.Count;
        readonly CancellationTokenSource lifetime=new CancellationTokenSource();
        readonly HashSet<string> validatedAccess=new HashSet<string>();
        public WildernessPlan Plan { get; private set; }
        public string Failure { get; private set; }
        public int LoadedCount => loaded.Count;
        public bool NavigationReady { get; private set; }
        public bool InitialReady { get; private set; }
        public bool HasTerrainAt(Vector3 point) => loaded.ContainsKey(WildernessPlan.Chunk.At(point.x,point.z));
        public bool IsReadyAt(Vector3 point) => !disposed && Failure==null && InitialReady && NavigationReady &&
            loaded.TryGetValue(WildernessPlan.Chunk.At(point.x,point.z),out var chunk) && chunk.Populated && chunk.NavigationReady && navigationBounds.Contains(new Vector3(point.x,128,point.z));
        public bool CanMoveTo(Vector3 point,float radius)
        {
            return Plan.WalkableWater(point.x,point.z) && IsReadyAt(point) && IsReadyAt(point+Vector3.right*radius) && IsReadyAt(point-Vector3.right*radius) &&
                IsReadyAt(point+Vector3.forward*radius) && IsReadyAt(point-Vector3.forward*radius);
        }

        public void Initialize(WoodlandRegion region,TopazWorldData world,Vector3 start)
        {
            poolRoot=new GameObject("Inactive scenery pool").transform;poolRoot.SetParent(transform,false);poolRoot.gameObject.SetActive(false);
            owner=region;owner.preset.ValidateContent();Plan=new WildernessPlan(world.seed,world.generationSettings);
            groundCover=owner.preset.groundCover.Length>0?owner.preset.groundCover:
                owner.preset.detail!=null?new[]{new GroundCoverPrototype{prefab=owner.preset.detail}}:Array.Empty<GroundCoverPrototype>();
            runtimeLayers=new[]{Instantiate(owner.preset.grass),Instantiate(owner.preset.path),Instantiate(owner.preset.rockLayer),Instantiate(owner.preset.forestLayer)};
            look=FindAnyObjectByType<Topaz.Rendering.VisualLookController>();
            // Authoring stores a linear average; full-resolution terrain textures need no CPU readback.
            groundColors=(Color[])owner.preset.distantGroundColors.Clone();
            var args=System.Environment.GetCommandLineArgs();
            // Native instanced Terrain renders black in the verified Metal player, including stock Terrain/Lit.
            // Keep the working native mesh path; foliage and ordinary mesh instancing remain enabled.
            terrainInstancing=args.Contains("--topaz-terrain-instancing");terrainShadows=!args.Contains("--topaz-no-terrain-shadows");
            session=FindFirstObjectByType<WorldSession>();player=session.transform;
            center=WildernessPlan.Chunk.At(start.x,start.z);
            worker=StartCoroutine(Guard(InitializeWorld()));
        }
        public string PreparationStage { get; private set; } = "starting";
        IEnumerator InitializeWorld()
        {
            PreparationStage="world data";
            var seed=Plan.Seed;var settings=Plan.Settings.Copy();var token=lifetime.Token;
            bool reusable=cachedPlan!=null && cachedPlan.Seed==seed &&
                JsonUtility.ToJson(cachedPlan.Settings)==JsonUtility.ToJson(settings);
            if(reusable)Plan=cachedPlan;
            else
            {
                var preparation=Task.Run(()=>
                {
                    var plan=new WildernessPlan(seed,settings);plan.Precompute(token);plan.Validate();return plan;
                },token);
                while(!preparation.IsCompleted)yield return null;
                if(disposed)yield break;
                Plan=preparation.GetAwaiter().GetResult();cachedPlan=Plan;
            }
            PreparationStage="shader warmup";
            foreach(var collection in owner.preset.warmupCollections)
            {
                if(collection==null || collection.graphicsDeviceType!=SystemInfo.graphicsDeviceType || collection.runtimePlatform!=Application.platform || collection.totalGraphicsStateCount==0)continue;
                int batches=Math.Max(1,(collection.totalGraphicsStateCount+31)/32+1);
                while(!collection.isWarmedUp && batches-->0)
                {
                    int before=collection.completedWarmupCount;
                    var warming=collection.WarmUpProgressively(32);
                    try {while(!warming.IsCompleted)yield return null;}
                    finally {warming.Complete();}
                    // Captures can include variants removed by the player stripper. Native 6.6
                    // need not report those as warmed; an advisory cache cannot block world entry.
                    if(collection.completedWarmupCount<=before)break;
                }
                if(!collection.isWarmedUp)Debug.LogWarning($"[Topaz/Rendering] Shader warmup stopped at {collection.completedWarmupCount}/{collection.totalGraphicsStateCount}; remaining states compile on use.");
            }
            PreparationStage="terrain";
            foreach(var key in Neighborhood(center,1))
            {yield return PrepareTerrain(key);yield return CommitTerrain(key);}
            ConnectTerrains();
            for(int i=0;i<5;i++){terrainPool.Push(CreateTerrainSlot());yield return null;}
            PreparationStage="scenery";
            int initialBatch=0;
            foreach(var key in Neighborhood(center,4)){CreateDistantChunk(key);if(++initialBatch%8==0)yield return null;}
            foreach(var key in Neighborhood(center,3))yield return CreateDistantTrees(key);
            foreach(var site in Plan.Destinations)
            {
                var source=site.Kind==DestinationKind.TitansGrave?owner.preset.titansGraveDistant:
                    site.Kind==DestinationKind.SplitPeak?owner.preset.splitPeakDistant:null;
                if(source==null)continue;
                var far=Instantiate(source,Position(site),Quaternion.Euler(0,site.Yaw,0),transform);
                if(site.Kind==DestinationKind.SplitPeak)FitSplitPeak(site,far);
                foreach(var collider in far.GetComponentsInChildren<Collider>(true))Destroy(collider);
                foreach(var renderer in far.GetComponentsInChildren<Renderer>(true))
                {renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.renderingLayerMask=Topaz.Rendering.SurfaceCacheLighting.VisualOnlyRenderingLayer;}
                distantDestinations.Add(site.Id,far);
            }
            gameObject.AddComponent<Topaz.Rendering.WildernessWater>().Initialize(Plan);
            CreateBoundary();
            // The owned skyline bake keeps distant peaks without long-range geometry or another runtime camera.
            if(Resources.Load<Cubemap>("TopazAlpineBackdrop")==null)
            for(int i=0;i<owner.preset.backgroundMountains.Length;i++)
            {
                float angle=i*Mathf.PI*.5f;float radius=Plan.Extent+600;
                var mountain=Instantiate(owner.preset.backgroundMountains[i],new Vector3(Mathf.Sin(angle)*radius,10,Mathf.Cos(angle)*radius),Quaternion.Euler(0,i*90,0),transform);
                foreach(var renderer in mountain.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.renderingLayerMask=Topaz.Rendering.SurfaceCacheLighting.VisualOnlyRenderingLayer;}
                yield return null;
            }
            if(owner.navigation==null)throw new InvalidOperationException("Wilderness navigation surface is missing.");
            ConfigureNavigation();
            navData=new NavMeshData(owner.navigation.agentTypeID);
            owner.navigation.navMeshData=navData;owner.navigation.AddData();
            PreparationStage="navigation and encounters";
            yield return Stream();
        }
        public void InvalidateNavigation() => navigationDirty=true;
        void ConfigureNavigation()
        {
            var nav=owner.navigation;
            nav.collectObjects=CollectObjects.Volume;
            nav.center=nav.transform.InverseTransformPoint(new Vector3((center.X+.5f)*128,128,(center.Z+.5f)*128));
            nav.size=new Vector3(384,320,384);
            nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        }
        IEnumerable<WildernessPlan.Chunk> Neighborhood(WildernessPlan.Chunk origin,int radius)
        {
            // Center first; then rings. Predictable priority after a teleport.
            for(int ring=0;ring<=radius;ring++)for(int z=-ring;z<=ring;z++)for(int x=-ring;x<=ring;x++)
            {
                if(Math.Max(Math.Abs(x),Math.Abs(z))!=ring)continue;
                var key=new WildernessPlan.Chunk(origin.X+x,origin.Z+z);if(Plan.ContainsChunk(key))yield return key;
            }
        }
        IEnumerator Stream()
        {
            // BindPair finishes assigning save authority before dynamic nodes are created.
            yield return null;
            RefreshBuiltArea(new Bounds(Vector3.zero,new Vector3(Plan.Settings.worldSize,200,Plan.Settings.worldSize)));
            while(!disposed)
            {
                var velocity=player.GetComponent<Topaz.Player.PlayerController>().PlanarVelocity;
                var ahead=player.position+Vector3.ClampMagnitude(velocity*8,48);
                var target=WildernessPlan.Chunk.At(ahead.x,ahead.z);
                if(Plan.ContainsChunk(target) && !building && !target.Equals(center))
                {center=target;navigationDirty=true;requestEpoch++;}
                foreach(var key in Neighborhood(center,1))
                {
                    if(!loaded.ContainsKey(key))
                    {
                        if(!preload.ContainsKey(key))yield return PrepareTerrain(key);
                        yield return CommitTerrain(key);
                        if(Failure!=null)yield break;
                        ConnectTerrains();navigationDirty=true;yield return null;
                    }
                    var chunk=loaded[key];
                    if(!chunk.Populated)
                    {
                        yield return Populate(key,chunk);
                        chunk.Populated=true;navigationDirty=true;
                    }
                }
                if(navigationDirty)
                {
                    yield return UpdateNavigation();
                    session.RefreshStreamedWorld();
                    InitialReady=true;
                }
                var keep=new HashSet<WildernessPlan.Chunk>(Neighborhood(center,1));
                foreach(var pair in loaded.ToArray())
                {
                    // Retain the departing row for 32 m, not an entire 128 m ring of gameplay objects.
                    var chunkCenter=new Vector3((pair.Key.X+.5f)*128,player.position.y,(pair.Key.Z+.5f)*128);
                    bool hysteresis=Mathf.Abs(chunkCenter.x-player.position.x)<224 && Mathf.Abs(chunkCenter.z-player.position.z)<224;
                    if(keep.Contains(pair.Key) || hysteresis || Pinned(pair.Value))continue;
                    yield return UnloadChunk(pair.Key,pair.Value);navigationDirty=true;
                }
                ConnectTerrains();
                yield return RefreshDistant();
                var reserve=new HashSet<WildernessPlan.Chunk>(Neighborhood(center,2));
                foreach(var key in preload.Keys.ToArray())if(!reserve.Contains(key))preload.Remove(key);
                foreach(var key in reserve)
                {
                    if(loaded.ContainsKey(key)||preload.ContainsKey(key))continue;
                    yield return PrepareTerrain(key);
                }
                yield return new WaitForSecondsRealtime(.1f);
            }
        }
        // Unity does not propagate exceptions from a yielded child iterator to its parent.
        // Drive nested iterators here so travel can inspect Failure and roll back safely.
        IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    object current=null;bool more=false;Exception failure=null;
                    try {more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
                    catch(Exception error){failure=error;}
                    if(failure!=null)
                    {
                        Failure=failure.Message;NavigationReady=false;building=false;
                        session.ShowHomeStatus("World loading failed. Return to the title and retry.");
                        Debug.LogException(failure);yield break;
                    }
                    if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                    if(current is IEnumerator nested)stack.Push(nested);
                    else yield return current;
                }
            }
            finally {while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
        }
        bool Pinned(LoadedChunk chunk)
        {
            foreach(var enemy in chunk.Root.GetComponentsInChildren<EnemyCombatant>(true))
                if(enemy!=null && enemy.IsAlive && ((enemy.transform.position-player.position).sqrMagnitude<60*60 || enemy.IsAttacking))return true;
            var bounds=new Bounds(chunk.Root.transform.position+new Vector3(64,0,64),new Vector3(132,200,132));
            return bounds.Contains(player.position);
        }
        IEnumerator PrepareTerrain(WildernessPlan.Chunk key)
        {
            if(preload.ContainsKey(key))yield break;
            int epoch=requestEpoch;var plan=Plan;int details=groundCover.Length;var token=lifetime.Token;
            double submit=Time.realtimeSinceStartupAsDouble;
            var work=Task.Run(()=>PrepareData(plan,key,details,token),token);
            PeakPreparationSliceMs=Mathf.Max(PeakPreparationSliceMs,(float)((Time.realtimeSinceStartupAsDouble-submit)*1000));
            while(!work.IsCompleted)yield return null;
            if(disposed)yield break;
            var prepared=work.GetAwaiter().GetResult();
            if(epoch==requestEpoch)preload[key]=prepared;
        }
        static PreparedTerrain PrepareData(WildernessPlan plan,WildernessPlan.Chunk key,int details,CancellationToken token)
        {
            var prepared=new PreparedTerrain(details);var resources=plan.Resources(key);
            for(int z=0;z<65;z++)
            {
                token.ThrowIfCancellationRequested();
                for(int x=0;x<65;x++)prepared.Heights[z,x]=plan.Height(key.X*128+x*2,key.Z*128+z*2)/WildernessPlan.TerrainHeight;
            }
            for(int z=0;z<64;z++)
            {
                token.ThrowIfCancellationRequested();
                for(int x=0;x<64;x++)
                {
                    float px=key.X*128+x*128/63f,pz=key.Z*128+z*128/63f;
                    var weights=plan.SurfaceWeights(px,pz);
                    for(int layer=0;layer<4;layer++)prepared.Paint[z,x,layer]=weights[layer];
                    if(details==0)continue;
                    // Detail cells represent areas, unlike the shared-edge alphamap samples above.
                    px=key.X*128+(x+.5f)*2;pz=key.Z*128+(z+.5f)*2;
                    bool clear=false;foreach(var r in resources)if((r.X-px)*(r.X-px)+(r.Z-pz)*(r.Z-pz)<9){clear=true;break;}
                    if(clear)continue;
                    var cover=plan.GroundCover(px,pz);
                    for(int layer=0;layer<details;layer++)
                    {
                        float expected=cover[Math.Min(layer,3)];int count=Mathf.FloorToInt(expected);
                        if(plan.Random(key.X*64+x,key.Z*64+z,(uint)(1200+layer))<expected-count)count++;
                        prepared.Density[layer][z,x]=count;
                    }
                }
            }
            return prepared;
        }
        IEnumerator CommitTerrain(WildernessPlan.Chunk key)
        {
            if(loaded.ContainsKey(key))yield break;
            var root=new GameObject(key.Id);root.transform.SetParent(transform,false);root.transform.position=new Vector3(key.X*128,0,key.Z*128);
            var slot=terrainPool.Count>0?terrainPool.Pop():CreateTerrainSlot();
            var data=slot.Data;data.name=key.Id;
            bool published=false;
            try
            {
            if(!preload.TryGetValue(key,out var prepared))throw new InvalidOperationException("Terrain must be prepared before commit: "+key.Id);
            double commitStart=Time.realtimeSinceStartupAsDouble;
            var preset=owner.preset;
            using(TerrainHeightsMarker.Auto())data.SetHeights(0,0,prepared.Heights);
            PeakTerrainCommitMs=Mathf.Max(PeakTerrainCommitMs,(float)((Time.realtimeSinceStartupAsDouble-commitStart)*1000));
            yield return null;commitStart=Time.realtimeSinceStartupAsDouble;
            using(TerrainPaintMarker.Auto())data.SetAlphamaps(0,0,prepared.Paint);
            PeakTerrainCommitMs=Mathf.Max(PeakTerrainCommitMs,(float)((Time.realtimeSinceStartupAsDouble-commitStart)*1000));
            yield return null;commitStart=Time.realtimeSinceStartupAsDouble;
            int[][,] baseline=prepared.Density;
            for(int layer=0;layer<baseline.Length;layer++)data.SetDetailLayer(0,0,layer,baseline[layer]);
            PeakTerrainCommitMs=Mathf.Max(PeakTerrainCommitMs,(float)((Time.realtimeSinceStartupAsDouble-commitStart)*1000));
            yield return null;commitStart=Time.realtimeSinceStartupAsDouble;
            preload.Remove(key);
            var terrain=slot.Terrain;var ground=terrain.gameObject;ground.name="Terrain";ground.transform.SetParent(root.transform,false);ground.transform.localPosition=Vector3.zero;
            terrain.materialTemplate=preset.terrainMaterial;terrain.drawInstanced=terrainInstancing;if(!terrainShadows)terrain.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;terrain.heightmapPixelError=2;terrain.detailObjectDistance=85;terrain.detailObjectDensity=look!=null?look.FoliageDensity:1;
            ground.SetActive(true);
            loaded.Add(key,new LoadedChunk {Root=root,Terrain=terrain,Data=data,BaseDensity=baseline});published=true;
            if(distant.TryGetValue(key,out var far))far.SetActive(false);
            PeakTerrainCommitMs=Mathf.Max(PeakTerrainCommitMs,(float)((Time.realtimeSinceStartupAsDouble-commitStart)*1000));
            }
            finally {if(!published){ReturnTerrain(slot.Terrain,data);Destroy(root);}}
        }
        IEnumerator Populate(WildernessPlan.Chunk key,LoadedChunk chunk)
        {
            var preset=owner.preset;int budget=0;double batchStart=Time.realtimeSinceStartupAsDouble;
            foreach(var site in Plan.Decorations(key,Plan.Settings.decorationCount))
            {
                if(!chunk.Sites.Add(site.Id))continue;
                GameObject prefab;
                var choices=site.Kind==0?preset.trees:site.Kind==1?preset.rocks:preset.undergrowth;
                if(site.Kind==1 && Plan.Slope(site.X,site.Z)>.35f && Plan.Height(site.X,site.Z)>40 && preset.cliffs.Length>0)choices=preset.cliffs;
                if(site.Kind==1 && Plan.Height(site.X,site.Z)>70 && preset.snowRocks.Length>0 && Plan.Random((int)site.X,(int)site.Z,904)<.35f)choices=preset.snowRocks;
                int choice=(int)(Plan.Random((int)site.X,(int)site.Z,901)*Math.Max(1,choices.Length));
                prefab=choices.Length>0?choices[choice]:site.Kind==0?preset.treeVisual:site.Kind==1?preset.rockVisual:preset.detail;
                if(prefab!=null)
                {
                    var go=RentDecoration(prefab,chunk.Root.transform,Position(site),Quaternion.Euler(0,Plan.Random((int)site.X,(int)site.Z,902)*360,0));
                    if(site.Kind==1)go.transform.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,Plan.Normal(site.X,site.Z)),.65f)*go.transform.rotation;
                    var binding=preset.Binding(prefab);
                    float scale=binding==null?1:Mathf.Lerp(binding.scaleRange.x,binding.scaleRange.y,Plan.Random((int)site.X,(int)site.Z,903));
                    go.transform.localScale*=site.Kind==0?Plan.TreeScale(site.X,site.Z,Plan.Random((int)site.X,(int)site.Z,903)):scale;
                    if(binding!=null)go.transform.position-=go.transform.TransformVector(binding.groundAnchor);
                    chunk.Decorations.Add(go);using(SpawnMarker.Auto())go.SetActive(!session.HomeBlocksResource(go.transform.position,.4f));
                    if(site.Kind==0 && canopyInstances.TryGetValue(key,out var canopy) && canopy.TryGetValue(site.Id,out var proxy))proxy.SetActive(false);
                }
                if(++budget%(InitialReady?4:16)==0 || Time.realtimeSinceStartupAsDouble-batchStart>=(InitialReady?.001:.005)){yield return null;batchStart=Time.realtimeSinceStartupAsDouble;}
            }
            RefreshBuiltArea(new Bounds(chunk.Root.transform.position+new Vector3(64,0,64),new Vector3(128,200,128)));
            foreach(var site in Plan.Resources(key))
            {
                if(!chunk.Sites.Add(site.Id))continue;
                if(site.Kind==0 && owner.rocks.Length>0)
                {
                    var node=Instantiate(owner.rocks[0],Position(site),Quaternion.identity,chunk.Root.transform);node.SetGeneratedId(site.Id);node.gameObject.SetActive(true);session.RegisterGatherable(node);
                }
                else if(site.Kind==3 && owner.plants.Length>0)
                {
                    var template=owner.plants.FirstOrDefault(p=>p.Mushrooms==(Plan.Random((int)site.X,(int)site.Z,104)<.5f))??owner.plants[0];
                    var node=Instantiate(template,Position(site),Quaternion.identity,chunk.Root.transform);node.SetGeneratedId(site.Id);node.gameObject.SetActive(true);session.RegisterGatherable(node);
                }
                else if(owner.trees.Length>0)
                {
                    var node=Instantiate(owner.trees[0],Position(site),Quaternion.identity,chunk.Root.transform);node.SetGeneratedId(site.Id);node.gameObject.SetActive(true);session.RegisterGatherable(node);
                }
                yield return null;
            }
            foreach(var crossing in Plan.Crossings)
                if(crossing.Bridge && WildernessPlan.Chunk.At(crossing.Position.x,crossing.Position.z).Equals(key) && preset.bridge!=null)
                {
                    Instantiate(preset.bridge,crossing.Position,Quaternion.LookRotation(crossing.Forward),chunk.Root.transform);
                    yield return null;
                }
            foreach(var site in Plan.Discoveries)
            {
                if(!WildernessPlan.Chunk.At(site.X,site.Z).Equals(key)||!chunk.Sites.Add(site.Id))continue;
                if(preset.discoveries.Length>0)
                {
                    var route=Plan.Routes.First(r=>r.DestinationId==site.Id);
                    var direction=route.Points[route.Points.Count-1]-route.Points[route.Points.Count-2];direction.y=0;direction.Normalize();
                    var position=Position(site)+direction*10;position.y=Plan.Height(position.x,position.z);
                    Instantiate(preset.discoveries[site.Kind%preset.discoveries.Length],position,Quaternion.LookRotation(direction),chunk.Root.transform);
                }
                if(preset.discoveryCache!=null)
                {
                    var route=Plan.Routes.First(r=>r.DestinationId==site.Id);
                    var direction=route.Points[route.Points.Count-1]-route.Points[route.Points.Count-2];direction.y=0;direction.Normalize();
                    var position=Position(site)+new Vector3(-direction.z,0,direction.x)*5;position.y=Plan.Height(position.x,position.z);
                    var cache=Instantiate(preset.discoveryCache,position,Quaternion.identity,chunk.Root.transform).GetComponent<DiscoveryCache>();
                    cache.Initialize(session,site.Id+".cache");
                }
            }
            foreach(var site in Plan.Destinations)
            {
                if(!site.Owner.Equals(key)||!chunk.Sites.Add(site.Id))continue;
                int index=(int)site.Kind;
                if(index>=preset.destinations.Length || preset.destinations[index]==null)continue;
                var landmark=Instantiate(preset.destinations[index],Position(site),Quaternion.Euler(0,site.Yaw,0),chunk.Root.transform);
                if(site.Kind==DestinationKind.SplitPeak)FitSplitPeak(site,landmark);
                if(distantDestinations.TryGetValue(site.Id,out var far))far.SetActive(false);
                yield return null;
            }
        }
        Vector3 Position(WildernessPlan.Site site) => new Vector3(site.X,Plan.Height(site.X,site.Z),site.Z);
        Vector3 Position(WildernessPlan.Destination site) => new Vector3(site.X,Plan.Height(site.X,site.Z),site.Z);
        void FitSplitPeak(WildernessPlan.Destination site,GameObject landmark)
        {
            float center=Plan.Height(site.X,site.Z);
            foreach(Transform piece in landmark.transform)
            {
                var renderers=piece.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length==0)continue;
                var bounds=renderers[0].bounds;
                for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
                piece.position+=Vector3.up*(Plan.Height(bounds.center.x,bounds.center.z)-center);
            }
        }
        IEnumerator UpdateNavigation()
        {
            navigationDirty=false;building=true;
            int epoch=requestEpoch;var ready=loaded.Values.Where(c=>c.Populated).ToArray();
            ConfigureNavigation();Physics.SyncTransforms();
            if(navData!=null) { double submit=Time.realtimeSinceStartupAsDouble;navigationOperation=owner.navigation.UpdateNavMesh(navData);PeakNavigationSubmitMs=Mathf.Max(PeakNavigationSubmitMs,(float)((Time.realtimeSinceStartupAsDouble-submit)*1000));while(!navigationOperation.isDone)yield return null;navigationOperation=null; }
            if(disposed || epoch!=requestEpoch){building=false;yield break;}
            navigationBounds=new Bounds(new Vector3((center.X+.5f)*128,128,(center.Z+.5f)*128),new Vector3(384,320,384));
            foreach(var chunk in loaded.Values)chunk.NavigationReady=false;
            foreach(var chunk in ready)chunk.NavigationReady=true;
            NavigationReady=navData!=null;building=false;
            ValidateRequiredAccess();
            SpawnEncounters();
        }
        void ValidateRequiredAccess()
        {
            // Player construction can intentionally alter access; validate the untouched generated baseline only.
            if(session.ActiveWorld.structures.Count>0)return;
            var path=new NavMeshPath();
            void Check(Vector3 from,Vector3 to,string id)
            {
                if(validatedAccess.Contains(id))return;
                from.y=Plan.Height(from.x,from.z);to.y=Plan.Height(to.x,to.z);
                if(!NavMesh.SamplePosition(from,out var start,2,NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(to,out var end,1,NavMesh.AllAreas) ||
                    !NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException($"Seed {Plan.Seed}: required {id} has no local navigation approach ({from} to {to}).");
                validatedAccess.Add(id);
            }
            foreach(var site in Plan.Discoveries)
            {
                if(Math.Abs(site.Owner.X-center.X)>1||Math.Abs(site.Owner.Z-center.Z)>1||!loaded.TryGetValue(site.Owner,out var chunk)||!chunk.Populated)continue;
                var route=Plan.Routes.First(r=>r.DestinationId==site.Id);
                Check(route.Points[route.Points.Count-2],Position(site),site.Id);
            }
            foreach(var site in Plan.RequiredResources)
            {
                if(Math.Abs(site.Owner.X-center.X)>1||Math.Abs(site.Owner.Z-center.Z)>1||!loaded.TryGetValue(site.Owner,out var chunk)||!chunk.Populated)continue;
                var from=site.Id.StartsWith("starter.")?new Vector3(site.X,0,site.Z).normalized*20:Position(Plan.Discoveries[2]);
                if(!HasTerrainAt(from))continue;
                var direction=(from-Position(site));direction.y=0;direction.Normalize();
                Check(from,Position(site)+direction*1.7f,site.Id);
            }
        }
        void SpawnEncounters()
        {
            enemies.RemoveAll(e=>e==null);
            foreach(var site in Plan.Discoveries)
            {
                var key=WildernessPlan.Chunk.At(site.X,site.Z);
                if(!loaded.TryGetValue(key,out var chunk) || !chunk.Populated)continue;
                SpawnGroup(site.Id,Position(site),Quaternion.identity,10,2,owner.preset.enemies,chunk);
            }
            foreach(var site in Plan.Destinations)
            {
                if(!loaded.TryGetValue(site.Owner,out var chunk)||!chunk.Populated)continue;
                int count=0;EnemyCombatant[] templates=null;
                switch(site.Kind)
                {
                    case DestinationKind.TitansGrave: count=3;templates=owner.preset.enemies;break;
                    case DestinationKind.Pyre: count=2;templates=owner.preset.enemies;break;
                    case DestinationKind.Statue: count=1;templates=owner.preset.enemies;break;
                    case DestinationKind.Cabin:
                    case DestinationKind.Beacon: count=2;templates=owner.preset.raiders;break;
                    case DestinationKind.WreckedBoat:
                    case DestinationKind.Crane: count=1+(Plan.Random((int)site.X,(int)site.Z,13001)>.5f?1:0);templates=owner.preset.raiders;break;
                    case DestinationKind.GoblinGate:
                    case DestinationKind.Shrine:
                    case DestinationKind.RuinArch: count=2;templates=owner.preset.goblins;break;
                    case DestinationKind.MineEntrance:
                        if(owner.preset.troll!=null && Plan.Random((int)site.X,(int)site.Z,13002)<.25f)
                        {count=1;templates=new[]{owner.preset.troll};}
                        else {count=3;templates=owner.preset.goblins;}
                        break;
                    case DestinationKind.MineTower:
                    case DestinationKind.TreeHouse: count=1;templates=owner.preset.goblins;break;
                    case DestinationKind.Effigy:
                        count=Plan.Random((int)site.X,(int)site.Z,13003)<.5f?1:0;templates=owner.preset.goblins;break;
                }
                if(count>0)
                {
                    var route=Plan.Routes.First(r=>r.DestinationId==site.Id);
                    var towardTrail=route.Points[0]-Position(site);towardTrail.y=0;
                    SpawnGroup(site.Id,Position(site),Quaternion.LookRotation(towardTrail.normalized),site.Radius,count,templates,chunk);
                }
            }
        }
        void SpawnGroup(string siteId,Vector3 center,Quaternion rotation,float radius,int count,EnemyCombatant[] templates,LoadedChunk chunk)
        {
            if(templates==null || templates.Length==0)return;
            Vector3 forward=rotation*Vector3.forward,right=rotation*Vector3.right;
            for(int n=0;n<count;n++)
            {
                string id=siteId+".enemy."+n;
                if(enemies.Any(e=>e.SpawnId==id))continue;
                Vector3 point=center+forward*(radius+4)+right*((n-(count-1)*.5f)*4);
                point.y=Plan.Height(point.x,point.z);
                if(CampSafety.IsProtected(point) || !NavMesh.SamplePosition(point,out var hit,3,NavMesh.AllAreas))continue;
                var template=templates[n%templates.Length];
                if(template==null)continue;
                var enemy=Instantiate(template,hit.position,Quaternion.identity,chunk.Root.transform);
                enemy.SetGeneratedId(id);enemy.BindTarget(session.GetComponent<PlayerVitality>(),FindFirstObjectByType<SafeZone>());
                enemy.gameObject.SetActive(true);session.RegisterEnemy(enemy);enemies.Add(enemy);
            }
        }
        void CreateBoundary()
        {
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Outer ridge collision",typeof(BoxCollider));go.transform.SetParent(transform,false);
                bool side=i<2;go.transform.position=side?new Vector3(i==0?-Plan.Extent+4:Plan.Extent-4,50,0):new Vector3(0,50,i==2?-Plan.Extent+4:Plan.Extent-4);
                go.GetComponent<BoxCollider>().size=side?new Vector3(4,100,Plan.Settings.worldSize):new Vector3(Plan.Settings.worldSize,100,4);
            }
        }
        public IEnumerator PrepareDestination(Vector3 position) => Guard(PrepareDestinationCore(position));
        IEnumerator PrepareDestinationCore(Vector3 position)
        {
            // Death recovery and diagnostic/travel requests must not populate/unload the same roots concurrently.
            while(preparingDestination && !disposed)yield return null;
            if(disposed)yield break;
            var destination=WildernessPlan.Chunk.At(position.x,position.z);
            if(!Plan.ContainsChunk(destination))
            {
                session.ShowHomeStatus("Destination lies outside the world. Current location retained.");
                yield break;
            }
            if(center.Equals(destination) && InitialReady && !building && !navigationDirty && IsReadyAt(position))
            {
                session.PrepareStreamedStructures(position);
                if(!navigationDirty)yield break;
            }
            preparingDestination=true;
            requestEpoch++;
            try
            {

            if(worker!=null)StopCoroutine(worker);
            if(navigationOperation!=null) {while(!navigationOperation.isDone)yield return null;navigationOperation=null;}
            center=destination;
            Failure=null;NavigationReady=false;InitialReady=false;
            var retain=new HashSet<WildernessPlan.Chunk>(Neighborhood(center,1));
            retain.UnionWith(Neighborhood(WildernessPlan.Chunk.At(player.position.x,player.position.z),1));
            foreach(var pair in loaded.ToArray())
            {
                if(retain.Contains(pair.Key)||Pinned(pair.Value))continue;
                yield return UnloadChunk(pair.Key,pair.Value);
            }
            foreach(var key in Neighborhood(center,1))
            {
                if(!loaded.ContainsKey(key))
                {
                    if(!preload.ContainsKey(key))yield return PrepareTerrain(key);
                    yield return CommitTerrain(key);
                        if(Failure!=null)yield break;
                }
                var chunk=loaded[key];
                if(!chunk.Populated){yield return Populate(key,chunk);chunk.Populated=true;}
                yield return null;
            }
            session.PrepareStreamedStructures(position);
            ConnectTerrains();yield return UpdateNavigation();
            yield return RefreshDistant();
            InitialReady=true;
            worker=StartCoroutine(Guard(Stream()));
                    }
            finally{preparingDestination=false;}
        }
        public void RefreshBuiltArea(Bounds area)
        {
            foreach(var chunk in loaded.Values)
            {
                var origin=chunk.Root.transform.position;
                int x0=Mathf.Max(0,Mathf.FloorToInt((area.min.x-origin.x)/2)-1),z0=Mathf.Max(0,Mathf.FloorToInt((area.min.z-origin.z)/2)-1);
                int x1=Mathf.Min(63,Mathf.CeilToInt((area.max.x-origin.x)/2)+1),z1=Mathf.Min(63,Mathf.CeilToInt((area.max.z-origin.z)/2)+1);
                if(x0>x1||z0>z1)continue;
                if(chunk.BaseDensity!=null) for(int layer=0;layer<chunk.BaseDensity.Length;layer++)
                {
                    var patch=new int[z1-z0+1,x1-x0+1];
                    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                    {
                        var point=origin+new Vector3((x+.5f)*2,0,(z+.5f)*2);point.y=Plan.Height(point.x,point.z);
                        patch[z-z0,x-x0]=session.HomeBlocksResource(point,.9f)?0:chunk.BaseDensity[layer][z,x];
                    }
                    chunk.Data.SetDetailLayer(x0,z0,layer,patch);
                }
                foreach(var visual in chunk.Decorations)
                    if(visual!=null && area.Contains(visual.transform.position))visual.SetActive(!session.HomeBlocksResource(visual.transform.position,.4f));
            }
        }
        void ConnectTerrains()
        {
            Terrain Get(int x,int z)=>loaded.TryGetValue(new WildernessPlan.Chunk(x,z),out var c)?c.Terrain:null;
            foreach(var pair in loaded){var k=pair.Key;pair.Value.Terrain.SetNeighbors(Get(k.X-1,k.Z),Get(k.X,k.Z+1),Get(k.X+1,k.Z),Get(k.X,k.Z-1));}
        }
        IEnumerator RefreshDistant()
        {
        var farKeep=new HashSet<WildernessPlan.Chunk>(Neighborhood(center,4));
        foreach(var pair in distant.ToArray())
        {
            if(farKeep.Contains(pair.Key))continue;
            var mesh=pair.Value.GetComponent<MeshFilter>().sharedMesh;meshes.Remove(mesh);Destroy(mesh);Destroy(pair.Value);distant.Remove(pair.Key);RemoveDistantCanopy(pair.Key);yield return null;
        }
        foreach(var key in farKeep)
        {
            if(!distant.ContainsKey(key)){CreateDistantChunk(key);yield return null;}
        }
        var treeKeep=new HashSet<WildernessPlan.Chunk>(Neighborhood(center,3));
        foreach(var pair in distantTrees.ToArray())if(!treeKeep.Contains(pair.Key)){RemoveDistantCanopy(pair.Key);yield return null;}
        foreach(var key in treeKeep)if(!distantTrees.ContainsKey(key))yield return CreateDistantTrees(key);
        }
        void CreateDistantChunk(WildernessPlan.Chunk key)
        {
            var material=owner.preset.distantMaterial;
            if(material==null)return;
            if(!distant.ContainsKey(key))
            {
                var heights=Plan.Heights(key,9);var vertices=new Vector3[81];var triangles=new int[8*8*6];var colors=new Color[81];int t=0;
                for(int z=0;z<9;z++)for(int x=0;x<9;x++)
                {
                    int i=z*9+x;float px=key.X*128+x*16,pz=key.Z*128+z*16;
                    vertices[i]=new Vector3(px,heights[z,x]-.08f,pz);var weights=Plan.SurfaceWeights(px,pz);colors[i]=groundColors[0]*weights.x+groundColors[1]*weights.y+groundColors[2]*weights.z+groundColors[3]*weights.w;
                    if(x==8||z==8)continue;
                    triangles[t++]=i;triangles[t++]=i+9;triangles[t++]=i+1;triangles[t++]=i+1;triangles[t++]=i+9;triangles[t++]=i+10;
                }
                var mesh=new Mesh {name=key.Id+" distant",vertices=vertices,triangles=triangles,colors=colors};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
                var go=new GameObject(key.Id+" distant",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
                go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(!loaded.ContainsKey(key));distant.Add(key,go);
            }
        }
        IEnumerator CreateDistantTrees(WildernessPlan.Chunk key)
        {
            if(distantTrees.ContainsKey(key)||!distant.TryGetValue(key,out var parent))yield break;
            var go=new GameObject(key.Id+" far canopy");go.transform.SetParent(transform,false);
            var instances=new Dictionary<string,GameObject>();canopyInstances.Add(key,instances);
            bool published=false;
            try
            {
                int number=0;
                foreach(var site in Plan.Decorations(key,Plan.Settings.decorationCount))
                {
                    if(site.Kind!=0)continue;number++;
                    var choices=owner.preset.distantTrees.Length>0?owner.preset.distantTrees:owner.preset.trees;
                    if(choices.Length==0)break;
                    int choice=(int)(Plan.Random((int)site.X,(int)site.Z,901)*choices.Length);
                    var tree=Instantiate(choices[choice],Position(site),Quaternion.Euler(0,Plan.Random((int)site.X,(int)site.Z,902)*360,0),go.transform);
                    var binding=owner.preset.Binding(owner.preset.trees[choice%owner.preset.trees.Length]);
                    tree.transform.localScale*=Plan.TreeScale(site.X,site.Z,Plan.Random((int)site.X,(int)site.Z,903));
                    if(binding!=null)tree.transform.position-=tree.transform.TransformVector(binding.groundAnchor);
                    instances.Add(site.Id,tree);
                    bool realized=loaded.TryGetValue(key,out var near) && near.Sites.Contains(site.Id);
                    tree.SetActive(!realized && !session.HomeBlocksResource(tree.transform.position,.4f));
                    foreach(var collider in tree.GetComponentsInChildren<Collider>(true))Destroy(collider);
                    foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true)){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.renderingLayerMask=Topaz.Rendering.SurfaceCacheLighting.VisualOnlyRenderingLayer;}
                    if(number%(InitialReady?4:16)==0)yield return null;
                }
                distantTrees.Add(key,go);published=true;
            }
            finally
            {
                if(!published){canopyInstances.Remove(key);Destroy(go);}
            }
        }
        void RemoveDistantCanopy(WildernessPlan.Chunk key)
        {
            if(distantTrees.TryGetValue(key,out var canopy))Destroy(canopy);
            distantTrees.Remove(key);canopyInstances.Remove(key);
        }
        GameObject RentDecoration(GameObject prefab,Transform parent,Vector3 position,Quaternion rotation)
        {
            GameObject go;
            if(decorationPools.TryGetValue(prefab,out var pool) && pool.Count>0)go=pool.Pop();
            else {using(SpawnMarker.Auto())go=Instantiate(prefab,poolRoot,false);decorationSources.Add(go,prefab);}
            go.SetActive(false);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(position,rotation);go.transform.localScale=prefab.transform.localScale;
            return go;
        }
        IEnumerator UnloadChunk(WildernessPlan.Chunk key,LoadedChunk chunk)
        {
            loaded.Remove(key);session.UnbindStreamedWorld(chunk.Root);chunk.Root.SetActive(false);
            if(distant.TryGetValue(key,out var far))far.SetActive(true);
            foreach(var site in Plan.Destinations)
                if(site.Owner.Equals(key) && distantDestinations.TryGetValue(site.Id,out var landmark))landmark.SetActive(true);
            if(canopyInstances.TryGetValue(key,out var canopy))
                foreach(var proxy in canopy.Values)proxy.SetActive(!session.HomeBlocksResource(proxy.transform.position,.4f));
            // Retire atomically before yielding. Cancellation cannot leave a half-pooled playable chunk.
            try
            {
                int batch=0;
                foreach(var go in chunk.Decorations)
                {
                    if(go==null)continue;
                    go.SetActive(false);var prefab=decorationSources[go];
                    if(!decorationPools.TryGetValue(prefab,out var pool)){pool=new Stack<GameObject>();decorationPools.Add(prefab,pool);}
                    if(pool.Count<32){go.transform.SetParent(poolRoot,false);pool.Push(go);}
                    else {decorationSources.Remove(go);Destroy(go);}
                    if(++batch%4==0)yield return null;
                }
            }
            finally
            {
                foreach(var go in chunk.Decorations)if(go!=null && go.transform.IsChildOf(chunk.Root.transform))decorationSources.Remove(go);
                ReturnTerrain(chunk.Terrain,chunk.Data);Destroy(chunk.Root);
            }
        }
        void OnDestroy()
        {
            disposed=true;lifetime.Cancel();requestEpoch++;if(worker!=null)StopCoroutine(worker);
            foreach(var c in loaded.Values)if(c.Data!=null)Destroy(c.Data);
            foreach(var slot in terrainPool)if(slot.Data!=null)Destroy(slot.Data);terrainPool.Clear();
            foreach(var mesh in meshes)Destroy(mesh);
            if(runtimeLayers!=null)foreach(var layer in runtimeLayers)Destroy(layer);
            if(navData!=null)
            {
                if(owner!=null && owner.navigation!=null && owner.navigation.navMeshData==navData)owner.navigation.RemoveData();
                var retiring=navData;
                if(navigationOperation!=null && !navigationOperation.isDone)navigationOperation.completed+=_=>Destroy(retiring);
                else Destroy(retiring);
            }
        }
        void Update()
        {
            if(look==null || runtimeLayers==null || Time.unscaledTime<nextSurfaceUpdate)return;
            nextSurfaceUpdate=Time.unscaledTime+.5f;float wetness=look.State.Wetness;
            foreach(var chunk in loaded.Values){chunk.Terrain.detailObjectDistance=look.HighQuality?85:60;chunk.Terrain.detailObjectDensity=look.FoliageDensity;}
            if(Mathf.Abs(wetness-lastWetness)<.002f)return;lastWetness=wetness;
            for(int i=0;i<runtimeLayers.Length;i++)runtimeLayers[i].smoothness=Mathf.Lerp(.04f,i==2?.38f:.22f,wetness);
        }
    }
}
