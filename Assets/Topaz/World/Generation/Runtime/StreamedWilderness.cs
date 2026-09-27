using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Topaz.Combat;
using Topaz.Gameplay;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Topaz.Generation
{
    /// <summary>Owns only loaded presentation/physics. WorldSession owns all persistent state.</summary>
    public sealed class StreamedWilderness : MonoBehaviour
    {
        sealed class LoadedChunk
        {
            public GameObject Root;
            public Terrain Terrain;
            public TerrainData Data;
            public bool Populated;
            public int[,] BaseDensity;
            public readonly List<GameObject> Decorations=new List<GameObject>();
            public readonly HashSet<string> Sites=new HashSet<string>();
        }
        readonly Dictionary<WildernessPlan.Chunk,LoadedChunk> loaded = new Dictionary<WildernessPlan.Chunk,LoadedChunk>();
        readonly Dictionary<WildernessPlan.Chunk,float[,]> preload = new Dictionary<WildernessPlan.Chunk,float[,]>();
        readonly Dictionary<WildernessPlan.Chunk,GameObject> distant = new Dictionary<WildernessPlan.Chunk,GameObject>();
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<EnemyCombatant> enemies = new List<EnemyCombatant>();
        WoodlandRegion owner;
        WorldSession session;
        Transform player;
        WildernessPlan.Chunk center;
        NavMeshData navData;
        AsyncOperation navigationOperation;
        Coroutine worker;
        bool navigationDirty;
        bool terrainInstancing=true,terrainShadows=true;
        bool building;
        bool disposed;
        public WildernessPlan Plan { get; private set; }
        public string Failure { get; private set; }
        public int LoadedCount => loaded.Count;
        public bool NavigationReady { get; private set; }
        public bool InitialReady { get; private set; }
        public bool HasTerrainAt(Vector3 point) => loaded.ContainsKey(WildernessPlan.Chunk.At(point.x,point.z));
        public bool IsReadyAt(Vector3 point) => Failure==null && InitialReady && NavigationReady && loaded.ContainsKey(WildernessPlan.Chunk.At(point.x,point.z));

        public void Initialize(WoodlandRegion region,TopazWorldData world,Vector3 start)
        {
            owner=region;Plan=new WildernessPlan(world.seed,world.generationSettings);
            var args=System.Environment.GetCommandLineArgs();
            terrainInstancing=!args.Contains("--topaz-no-terrain-instancing");terrainShadows=!args.Contains("--topaz-no-terrain-shadows");
            session=FindFirstObjectByType<WorldSession>();player=session.transform;
            center=WildernessPlan.Chunk.At(start.x,start.z);
            // Initial construction happens behind the loading screen; incremental work follows in Play.
            foreach(var key in Neighborhood(center,1))LoadTerrain(key);
            ConnectTerrains();
            CreateDistantLandscape();
            CreateBoundary();
            var surface=owner.navigation;
            if(surface!=null)
            {
                ConfigureNavigation();surface.BuildNavMesh();navData=surface.navMeshData;
                NavigationReady=navData!=null;
            }
            else throw new InvalidOperationException("Wilderness navigation surface is missing.");
            worker=StartCoroutine(Guard(Stream()));
        }
        public void InvalidateNavigation() => navigationDirty=true;
        void ConfigureNavigation()
        {
            var nav=owner.navigation;
            nav.collectObjects=CollectObjects.Volume;
            nav.center=nav.transform.InverseTransformPoint(new Vector3((center.X+.5f)*128,40,(center.Z+.5f)*128));
            nav.size=new Vector3(384,120,384);
            nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        }
        static IEnumerable<WildernessPlan.Chunk> Neighborhood(WildernessPlan.Chunk origin,int radius)
        {
            // Center first; then rings. Predictable priority after a teleport.
            for(int ring=0;ring<=radius;ring++)for(int z=-ring;z<=ring;z++)for(int x=-ring;x<=ring;x++)
            {
                if(Math.Max(Math.Abs(x),Math.Abs(z))!=ring)continue;
                var key=new WildernessPlan.Chunk(origin.X+x,origin.Z+z);if(key.Valid)yield return key;
            }
        }
        IEnumerator Stream()
        {
            // BindPair finishes assigning save authority before dynamic nodes are created.
            yield return null;
            RefreshBuiltArea(new Bounds(Vector3.zero,new Vector3(1024,200,1024)));
            while(!disposed)
            {
                var target=WildernessPlan.Chunk.At(player.position.x,player.position.z);
                if(target.Valid && !building)center=target;
                foreach(var key in Neighborhood(center,1))
                {
                    if(!loaded.ContainsKey(key))
                    {
                        if(!TryLoad(key))yield break;
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
                    if(keep.Contains(pair.Key) || Pinned(pair.Value))continue;
                    Unload(pair.Key,pair.Value);navigationDirty=true;yield return null;
                }
                ConnectTerrains();
                var reserve=new HashSet<WildernessPlan.Chunk>(Neighborhood(center,2));
                foreach(var key in preload.Keys.ToArray())if(!reserve.Contains(key))preload.Remove(key);
                foreach(var key in reserve)
                {
                    if(loaded.ContainsKey(key)||preload.ContainsKey(key))continue;
                    preload[key]=Plan.Heights(key,65);yield return null;
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
        bool TryLoad(WildernessPlan.Chunk key)
        {
            try {LoadTerrain(key);Failure=null;return true;}
            catch(Exception e){Failure=e.Message;session.ShowHomeStatus("World loading failed. Return to the title and retry: "+e.Message);Debug.LogException(e);return false;}
        }
        bool Pinned(LoadedChunk chunk)
        {
            foreach(var enemy in chunk.Root.GetComponentsInChildren<EnemyCombatant>(true))
                if(enemy!=null && enemy.IsAlive && ((enemy.transform.position-player.position).sqrMagnitude<60*60 || enemy.IsAttacking))return true;
            var bounds=new Bounds(chunk.Root.transform.position+new Vector3(64,0,64),new Vector3(132,200,132));
            return bounds.Contains(player.position);
        }
        void LoadTerrain(WildernessPlan.Chunk key)
        {
            if(loaded.ContainsKey(key))return;
            var root=new GameObject(key.Id);root.transform.SetParent(transform,false);root.transform.position=new Vector3(key.X*128,0,key.Z*128);
            var data=new TerrainData {name=key.Id,heightmapResolution=65,size=new Vector3(128,80,128),alphamapResolution=64};
            float[,] heights=preload.TryGetValue(key,out var cached)?cached:Plan.Heights(key,65);
            var normalized=new float[65,65];for(int z=0;z<65;z++)for(int x=0;x<65;x++)normalized[z,x]=heights[z,x]/80;
            data.SetHeights(0,0,normalized);
            var preset=owner.preset;
            data.terrainLayers=new[]{preset.grass,preset.path,preset.rockLayer??preset.path,preset.forestLayer??preset.grass};
            var paint=new float[64,64,4];
            for(int z=0;z<64;z++)for(int x=0;x<64;x++)
            {
                float px=key.X*128+x*128/63f,pz=key.Z*128+z*128/63f;
                float rock=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.65f,Plan.Slope(px,pz)));
                float dirt=Plan.WornGround(px,pz);
                float forest=Plan.Forest(px,pz)*.45f;
                paint[z,x,2]=rock;paint[z,x,1]=(1-rock)*dirt;paint[z,x,3]=(1-rock)*(1-dirt)*forest;paint[z,x,0]=(1-rock)*(1-dirt)*(1-forest);
            }
            data.SetAlphamaps(0,0,paint);
            int[,] baseline=null;
            if(preset.detail!=null)
            {
                data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
                data.SetDetailResolution(64,16);
                data.detailPrototypes=new[]{new DetailPrototype {prototype=preset.detail,usePrototypeMesh=true,useInstancing=true,renderMode=DetailRenderMode.VertexLit,minWidth=.5f,maxWidth=.8f,minHeight=.25f,maxHeight=.5f,healthyColor=Color.white,dryColor=Color.white}};
                var density=new int[64,64];
                var resources=Plan.Resources(key);
                for(int z=0;z<64;z++)for(int x=0;x<64;x++)
                {
                    float px=key.X*128+x*128/63f,pz=key.Z*128+z*128/63f;
                    bool clear=false;foreach(var r in resources)if((r.X-px)*(r.X-px)+(r.Z-pz)*(r.Z-pz)<9){clear=true;break;}
                    if(!clear && paint[z,x,2]<.4f && paint[z,x,1]<.4f && Plan.Random(key.X*64+x,key.Z*64+z,803)<Mathf.Lerp(.25f,.8f,Plan.Moisture(px,pz)))density[z,x]=2;
                }
                data.SetDetailLayer(0,0,0,density);baseline=density;
            }
            var ground=Terrain.CreateTerrainGameObject(data);ground.name="Terrain";ground.transform.SetParent(root.transform,false);
            var terrain=ground.GetComponent<Terrain>();terrain.materialTemplate=preset.terrainMaterial;terrain.drawInstanced=terrainInstancing;if(!terrainShadows)terrain.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;terrain.heightmapPixelError=5;terrain.detailObjectDistance=55;
            loaded.Add(key,new LoadedChunk {Root=root,Terrain=terrain,Data=data,BaseDensity=baseline});
            if(distant.TryGetValue(key,out var far))far.SetActive(false);
        }
        IEnumerator Populate(WildernessPlan.Chunk key,LoadedChunk chunk)
        {
            var preset=owner.preset;int budget=0;
            foreach(var site in Plan.Decorations(key,Plan.Settings.decorationCount))
            {
                if(!chunk.Sites.Add(site.Id))continue;
                GameObject prefab;
                var choices=site.Kind==0?preset.trees:site.Kind==1?preset.rocks:preset.undergrowth;
                int choice=(int)(Plan.Random((int)site.X,(int)site.Z,901)*Math.Max(1,choices.Length));
                prefab=choices.Length>0?choices[choice]:site.Kind==0?preset.treeVisual:site.Kind==1?preset.rockVisual:preset.detail;
                if(prefab!=null)
                {
                    var go=Instantiate(prefab,Position(site),Quaternion.Euler(0,Plan.Random((int)site.X,(int)site.Z,902)*360,0),chunk.Root.transform);
                    go.transform.localScale*=.8f+Plan.Random((int)site.X,(int)site.Z,903)*.45f;
                    chunk.Decorations.Add(go);go.SetActive(!session.HomeBlocksResource(go.transform.position,.4f));
                }
                if(++budget%12==0)yield return null;
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
            foreach(var site in Plan.Discoveries)
            {
                if(!WildernessPlan.Chunk.At(site.X,site.Z).Equals(key)||!chunk.Sites.Add(site.Id))continue;
                if(preset.discoveries.Length>0)Instantiate(preset.discoveries[site.Kind%preset.discoveries.Length],Position(site),Quaternion.identity,chunk.Root.transform);
                if(preset.discoveryCache!=null)
                {
                    var position=Position(site)+Vector3.right*3;position.y=Plan.Height(position.x,position.z);
                    var cache=Instantiate(preset.discoveryCache,position,Quaternion.identity,chunk.Root.transform).GetComponent<DiscoveryCache>();
                    cache.Initialize(session,site.Id+".cache");
                }
            }
        }
        Vector3 Position(WildernessPlan.Site site) => new Vector3(site.X,Plan.Height(site.X,site.Z),site.Z);
        IEnumerator UpdateNavigation()
        {
            navigationDirty=false;building=true;
            ConfigureNavigation();Physics.SyncTransforms();
            if(navData!=null) { navigationOperation=owner.navigation.UpdateNavMesh(navData);while(!navigationOperation.isDone)yield return null;navigationOperation=null; }
            NavigationReady=navData!=null;building=false;
            SpawnEncounters();
        }
        void SpawnEncounters()
        {
            enemies.RemoveAll(e=>e==null);
            var templates=owner.preset.enemies;
            if(templates.Length==0)return;
            foreach(var site in Plan.Discoveries)
            {
                var key=WildernessPlan.Chunk.At(site.X,site.Z);
                if(!loaded.TryGetValue(key,out var chunk) || !chunk.Populated)continue;
                for(int n=0;n<2 && enemies.Count<10;n++)
                {
                    string id=site.Id+".enemy."+n;
                    if(enemies.Any(e=>e.SpawnId==id))continue;
                    Vector3 point=Position(site)+new Vector3(12+n*3,0,0);
                    point.y=Plan.Height(point.x,point.z);
                    if(CampSafety.IsProtected(point) || !NavMesh.SamplePosition(point,out var hit,4,NavMesh.AllAreas))continue;
                    var template=templates[(site.Kind+n)%templates.Length];
                    var enemy=Instantiate(template,hit.position,Quaternion.identity,chunk.Root.transform);
                    enemy.SetGeneratedId(id);enemy.BindTarget(session.GetComponent<PlayerVitality>(),FindFirstObjectByType<SafeZone>());
                    enemy.gameObject.SetActive(true);session.RegisterEnemy(enemy);enemies.Add(enemy);
                }
            }
        }
        void CreateBoundary()
        {
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Outer ridge collision",typeof(BoxCollider));go.transform.SetParent(transform,false);
                bool side=i<2;go.transform.position=side?new Vector3(i==0?-508:508,50,0):new Vector3(0,50,i==2?-508:508);
                go.GetComponent<BoxCollider>().size=side?new Vector3(4,100,1024):new Vector3(1024,100,4);
            }
        }
        public IEnumerator PrepareDestination(Vector3 position) => Guard(PrepareDestinationCore(position));
        IEnumerator PrepareDestinationCore(Vector3 position)
        {
            if(worker!=null)StopCoroutine(worker);
            if(navigationOperation!=null) {while(!navigationOperation.isDone)yield return null;navigationOperation=null;}
            center=WildernessPlan.Chunk.At(position.x,position.z);
            if(!center.Valid){Failure="Destination lies outside the world.";yield break;}
            Failure=null;NavigationReady=false;InitialReady=false;
            foreach(var key in Neighborhood(center,1))
            {
                if(!loaded.ContainsKey(key) && !TryLoad(key))yield break;
                var chunk=loaded[key];
                if(!chunk.Populated){yield return Populate(key,chunk);chunk.Populated=true;}
                yield return null;
            }
            session.PrepareStreamedStructures(position);
            ConnectTerrains();yield return UpdateNavigation();
            InitialReady=true;
            worker=StartCoroutine(Guard(Stream()));
        }
        public void RefreshBuiltArea(Bounds area)
        {
            foreach(var chunk in loaded.Values)
            {
                var origin=chunk.Root.transform.position;
                int x0=Mathf.Max(0,Mathf.FloorToInt((area.min.x-origin.x)/2)-1),z0=Mathf.Max(0,Mathf.FloorToInt((area.min.z-origin.z)/2)-1);
                int x1=Mathf.Min(63,Mathf.CeilToInt((area.max.x-origin.x)/2)+1),z1=Mathf.Min(63,Mathf.CeilToInt((area.max.z-origin.z)/2)+1);
                if(x0>x1||z0>z1)continue;
                if(chunk.BaseDensity!=null)
                {
                    var patch=new int[z1-z0+1,x1-x0+1];
                    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                    {
                        var point=origin+new Vector3((x+.5f)*2,0,(z+.5f)*2);point.y=Plan.Height(point.x,point.z);
                        patch[z-z0,x-x0]=session.HomeBlocksResource(point,.9f)?0:chunk.BaseDensity[z,x];
                    }
                    chunk.Data.SetDetailLayer(x0,z0,0,patch);
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
        void CreateDistantLandscape()
        {
            var material=owner.preset.distantMaterial;
            if(material==null)return;
            foreach(var key in Neighborhood(new WildernessPlan.Chunk(0,0),8))
            {
                var heights=Plan.Heights(key,17);var vertices=new Vector3[289];var triangles=new int[16*16*6];var colors=new Color[289];int t=0;
                for(int z=0;z<17;z++)for(int x=0;x<17;x++)
                {
                    int i=z*17+x;float px=key.X*128+x*8,pz=key.Z*128+z*8;
                    vertices[i]=new Vector3(px,heights[z,x]-.08f,pz);colors[i]=Color.Lerp(new Color(.48f,.64f,.29f),new Color(.23f,.39f,.23f),Plan.Forest(px,pz));
                    if(x==16||z==16)continue;
                    triangles[t++]=i;triangles[t++]=i+17;triangles[t++]=i+1;triangles[t++]=i+1;triangles[t++]=i+17;triangles[t++]=i+18;
                }
                var mesh=new Mesh {name=key.Id+" distant",vertices=vertices,triangles=triangles,colors=colors};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
                var go=new GameObject(key.Id+" distant",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
                int number=0;
                foreach(var site in Plan.Decorations(key,120))
                {
                    if(site.Kind!=0 || ++number>24)continue;
                    var choices=owner.preset.trees;
                    if(choices.Length==0)break;
                    int choice=(int)(Plan.Random((int)site.X,(int)site.Z,901)*choices.Length);
                    var tree=Instantiate(choices[choice],Position(site),Quaternion.Euler(0,Plan.Random((int)site.X,(int)site.Z,902)*360,0),go.transform);
                    tree.transform.localScale*=.8f+Plan.Random((int)site.X,(int)site.Z,903)*.45f;
                    foreach(var collider in tree.GetComponentsInChildren<Collider>())Destroy(collider);
                    foreach(var renderer in tree.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                go.SetActive(!loaded.ContainsKey(key));distant.Add(key,go);
            }
        }
        void Unload(WildernessPlan.Chunk key,LoadedChunk chunk)
        {
            session.UnbindStreamedWorld(chunk.Root);chunk.Root.SetActive(false);Destroy(chunk.Root);Destroy(chunk.Data);loaded.Remove(key);
            if(distant.TryGetValue(key,out var far))far.SetActive(true);
        }
        void OnDestroy()
        {
            disposed=true;if(worker!=null)StopCoroutine(worker);
            foreach(var c in loaded.Values)if(c.Data!=null)Destroy(c.Data);
            foreach(var mesh in meshes)Destroy(mesh);
            if(navData!=null)
            {
                if(owner!=null && owner.navigation!=null && owner.navigation.navMeshData==navData)owner.navigation.RemoveData();
                var retiring=navData;
                if(navigationOperation!=null && !navigationOperation.isDone)navigationOperation.completed+=_=>Destroy(retiring);
                else Destroy(retiring);
            }
        }
    }
}
