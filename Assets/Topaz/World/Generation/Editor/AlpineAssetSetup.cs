using System;
using System.Collections.Generic;
using System.Linq;
using Topaz.Characters;
using Topaz.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    /// <summary>Authors owned wrappers only; the purchased library remains unchanged.</summary>
    public static class AlpineAssetSetup
    {
        const string Root = "Assets/Topaz/Presentation/Art/World/";
        const string Characters = "Assets/Topaz/Presentation/Art/Characters/";
        const string Viking = "Assets/Synty/PolygonVikingRealm/Prefabs/";
        const string Alpine = "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/";
        static GameObject Source(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException("Missing purchased asset: " + path);
        static GameObject Save(GameObject go, string path)
        { var prefab = PrefabUtility.SaveAsPrefabAsset(go, path); Object.DestroyImmediate(go); return prefab; }
        static void StripColliders(GameObject go)
        { foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c); }
        static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) throw new InvalidOperationException("No visible geometry: " + go.name);
            var bounds = rs[0].bounds; foreach (var r in rs) bounds.Encapsulate(r.bounds); return bounds;
        }
        static Material SummerMaterial(Material source)
        {
            if(source==null)return null;
            string path=Root+"Alpine Summer "+source.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,path);}
            foreach(string property in new[]{"_Enable_Frosting","_FrostingSwitch"})if(material.HasProperty(property))material.SetFloat(property,0);
            material.enableInstancing=true;material.SetShaderPassEnabled("MotionVectors",true);EditorUtility.SetDirty(material);return material;
        }
        static GameObject Wrap(string name, string source, float height, WoodlandRole role, List<WoodlandAssetRole> roles)
        {
            var root = new GameObject(name); var model = Object.Instantiate(Source(source), root.transform, false);
            StripColliders(model);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if(name.StartsWith("Alpine Cliff"))renderer.sharedMaterials=Enumerable.Repeat(AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Materials/MossRock_Triplanar.mat"),renderer.sharedMaterials.Length).ToArray();
                else if(role==WoodlandRole.Canopy)renderer.sharedMaterials=renderer.sharedMaterials.Select(SummerMaterial).ToArray();
            }
            var b = BoundsOf(model); float scale = height / Mathf.Max(.01f, b.size.y);
            model.transform.localScale *= scale; model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z) * scale;
            if (role == WoodlandRole.Canopy)
            { var c = root.AddComponent<CapsuleCollider>(); c.radius = .32f; c.height = height * .72f; c.center = Vector3.up * c.height / 2; }
            if (role == WoodlandRole.Rock)
            { var c = root.AddComponent<BoxCollider>(); c.size = Vector3.Scale(b.size, Vector3.one * scale * .85f); c.center = Vector3.up * c.size.y / 2; }
            var prefab = Save(root, Root + name + ".prefab");
            roles.Add(new WoodlandAssetRole { prefab = prefab, role = role, footprint = role == WoodlandRole.Canopy ? 3 : role == WoodlandRole.Rock ? 3 : 1,
                clearance = 1, maximumSlope = role == WoodlandRole.Rock ? .8f : .35f,
                collision = role == WoodlandRole.Undergrowth ? WoodlandCollision.None : WoodlandCollision.Solid,
                groundAnchor = role == WoodlandRole.Rock ? Vector3.up * height * .18f : Vector3.zero,
                scaleRange = new Vector2(.85f, 1.15f), visualSize = b.size * scale });
            return prefab;
        }
        [MenuItem("Topaz/Generation/Ground Existing Alpine Rocks")]
        public static void ConfigureRockGrounding()
        {
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            if(preset==null)throw new InvalidOperationException("Woodland preset is missing.");
            var rocks=new HashSet<GameObject>(preset.rocks.Concat(preset.cliffs).Concat(preset.snowRocks));
            foreach(var binding in preset.roles)
                if(binding.role==WoodlandRole.Rock && rocks.Contains(binding.prefab))
                    // Bury the irregular lowest tip so broad rock bodies meet the sloping soil.
                    binding.groundAnchor=Vector3.up*binding.visualSize.y*.18f;
            EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();
        }

        [MenuItem("Topaz/Generation/Configure Viking Alpine")]
        public static void Apply()
        {
            var preset = AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            var roles = new List<WoodlandAssetRole>();
            preset.trees = Enumerable.Range(1, 5).Select(i => Wrap("Alpine Pine " + i, Alpine + "SM_Env_Pine_0" + i + ".prefab", 9 + i, WoodlandRole.Canopy, roles)).ToArray();
            preset.distantTrees = preset.trees.Select(FarTree).ToArray();
            preset.bridge = Bridge();
            preset.rocks = Enumerable.Range(1, 7).Select(i => Wrap("Alpine Rock " + i, Alpine + "SM_Env_Rock_0" + i + ".prefab", 1.1f + i * .22f, WoodlandRole.Rock, roles)).ToArray();
            preset.cliffs=Enumerable.Range(1,4).Select(i=>Wrap("Alpine Cliff "+i,Alpine+"SM_Env_Rock_Cliff_0"+i+".prefab",3,WoodlandRole.Rock,roles)).ToArray();
            preset.snowRocks=Enumerable.Range(1,3).Select(i=>Wrap("Alpine Snow "+i,Alpine+"SM_Env_Snow_Mound_0"+i+".prefab",.8f,WoodlandRole.Undergrowth,roles)).ToArray();
            preset.backgroundMountains=Enumerable.Range(1,4).Select(i=>Wrap("Alpine Mountain "+i,Alpine+"SM_Env_MountainRange_0"+i+".prefab",220,WoodlandRole.Undergrowth,new List<WoodlandAssetRole>())).ToArray();
            var plants = new List<GameObject>();
            foreach (var name in new[] { "Bush_01", "Bush_02", "Bush_Flower_01_Alt", "GroundCover_01", "GroundCover_02", "GroundCover_03", "Moss_Lumps_01", "Moss_Lumps_02", "Branch_01", "Branch_03", "Pine_Stump_01" })
                plants.Add(Wrap("Alpine " + name, Alpine + "SM_Env_" + name + ".prefab", name.StartsWith("Bush") ? .8f : .3f, WoodlandRole.Undergrowth, roles));
            preset.undergrowth = plants.ToArray(); preset.treeVisual = preset.trees[0]; preset.treeVariant = preset.trees[1]; preset.rockVisual = preset.rocks[0];
            preset.settings = WoodlandSettings.LargeWorld();
            // Existing grass mesh retains its validated instancing/wind shader; larger plants supply biome variety.
            ConfigureBuildings();
            preset.discoveries = new[] { Composition("Viking Runestones", 0), Composition("Viking Abandoned Hall", 1), Composition("Viking Quarry Camp", 2) };
            for (int i = 0; i < 3; i++) roles.Add(new WoodlandAssetRole { prefab = preset.discoveries[i], role = new[] { WoodlandRole.Landmark, WoodlandRole.Camp, WoodlandRole.ResourceSite }[i], footprint = 8, clearance = 1, scaleRange = Vector2.one, randomYaw = false, collision = WoodlandCollision.Solid });
            roles.Add(new WoodlandAssetRole { role = WoodlandRole.ShallowWater, footprint = 17, clearance = 10, collision = WoodlandCollision.WalkableSurface });
            preset.roles = roles.ToArray(); preset.ValidateContent(); EditorUtility.SetDirty(preset);
            var looks = ConfigureCharacters();
            ConfigureItems();
            var scene = EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            foreach (var appearance in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerAppearance>(true)))
            {
                var so = new SerializedObject(appearance); var parent = (Transform)so.FindProperty("visualRoot").objectReferenceValue;
                var prior = so.FindProperty("rogueVisual").objectReferenceValue as GameObject;
                if (prior != null) Object.DestroyImmediate(prior);
                so.FindProperty("rogueVisual").objectReferenceValue = SyntySampleSetup.Instance(looks[0], parent);
                var choices = so.FindProperty("looks"); choices.arraySize = looks.Length;
                for (int i = 0; i < looks.Length; i++)
                { choices.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = CharacterLooks.All[i]; choices.GetArrayElementAtIndex(i).FindPropertyRelative("model").objectReferenceValue = looks[i]; }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var region in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WoodlandRegion>(true)))
            {
                foreach (var tree in region.trees) ReplaceResource(tree, preset.trees[0], "trunkCollider");
                foreach (var rock in region.rocks) ReplaceResource(rock, preset.rocks[0], "rockCollider");
            }
            foreach(var chest in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StorageChest>(true)))
                ReplaceProp(chest.gameObject,"Props/SM_Prop_Chest_01",new Vector3(1,.8f,.8f),Vector3.down*.4f);
            foreach(var fire in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Campfire>(true)))
                ReplaceProp(fire.gameObject,"Props/SM_Prop_Fire_Pit_01",new Vector3(1.3f,.4f,1.3f),Vector3.zero);
            foreach(var point in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RestSpot>(true)))
                ReplaceProp(point.gameObject,"Props/SM_Prop_Pelt_01",Vector3.one,Vector3.down*.5f);
            foreach(var node in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
            {
                if(node==null)continue;
                if(node.name=="Prototype Workbench")ReplaceProp(node.gameObject,"Props/SM_Prop_Table_01",Vector3.one,Vector3.down*.5f);
                else if(node.name=="Prototype Rack")ReplaceProp(node.gameObject,"Props/SM_Prop_Weapon_Rack_01",Vector3.one,Vector3.down*.5f);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            SurfaceCacheSetup.ConfigureModelReadability();
            ConfigureNeutralMenu();
            Debug.Log("[Topaz] Viking Alpine assets configured.");
        }
        [MenuItem("Topaz/Generation/Configure Neutral Viking Preview")]
        public static void ConfigureNeutralMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            var stage = scene.GetRootGameObjects().Single(g => g.name == "Main Menu Stage");
            var data = new SerializedObject(stage.GetComponent<Topaz.Menus.MainMenuStage>());
            var camera = (Camera)data.FindProperty("menuCamera").objectReferenceValue;
            var anchor = (Transform)data.FindProperty("actorAnchor").objectReferenceValue;
            var key = stage.transform.Find("Character Warm Key");
            if (key == null) key = stage.transform.Find("Character Neutral Key");
            if (camera == null || anchor == null || key == null) throw new InvalidOperationException("Missing menu preview references.");
            // Only the obsolete decorative camp is removed; menu controls and model bindings live elsewhere.
            foreach (Transform child in stage.transform.Cast<Transform>().ToArray())
                if (child != camera.transform && child != anchor && child != key) Object.DestroyImmediate(child.gameObject);
            camera.orthographic = false;
            camera.fieldOfView = 35;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.08f, .1f, .13f);
            camera.transform.position = anchor.position + new Vector3(-1.15f, 1.6f, -5.8f);
            camera.transform.LookAt(anchor.position + new Vector3(-1.15f, 1.25f, 0));
            var light = key.GetComponent<Light>();
            key.name = "Character Neutral Key";
            key.rotation = Quaternion.Euler(25, -25, 0);
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << 5;
            light.renderingLayerMask = 4;
            data.FindProperty("volumeTrigger").objectReferenceValue = anchor;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static void ReplaceResource(Component owner, GameObject prefab, string collider)
        {
            var so = new SerializedObject(owner); var old = so.FindProperty("visualRoot").objectReferenceValue as GameObject;
            if (old != null && old != owner.gameObject) Object.DestroyImmediate(old);
            var visual = SyntySampleSetup.Instance(prefab, owner.transform);
            so.FindProperty("visualRoot").objectReferenceValue = visual; so.FindProperty(collider).objectReferenceValue = visual.GetComponent<Collider>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Topaz/Generation/Frame Runestone Approach")]
        public static void ConfigureRuneGateway()
        {
            string path=Root+"Viking Runestones.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try { AddRuneGateway(root.transform);PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static void AddRuneGateway(Transform parent)
        {
            const string path="Assets/Synty/PolygonGoblinWarCamp/Prefabs/Props/SM_Prop_Ruins_Archway_01.prefab";
            foreach(var dependency in AssetDatabase.GetDependencies(path,true))
                if(dependency.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase) &&
                    AssetImporter.GetAtPath(dependency) is ModelImporter importer && !importer.isReadable)
                {importer.isReadable=true;importer.SaveAndReimport();}
            var old=parent.Find("Rune Approach Arch");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var model=Object.Instantiate(Source(path),parent,false);model.name="Rune Approach Arch";
            // The damaged arch has an asymmetric opening; align the passage rather than its bounds.
            StripColliders(model);SyntySampleSetup.Fit(model,new Vector3(8,6,2),new Vector3(.65f,-.35f,-13));
            var group=model.GetComponentInChildren<LODGroup>();
            var surfaces=group!=null?group.GetLODs()[0].renderers:model.GetComponentsInChildren<Renderer>();
            // Surface collision preserves the walk-through opening; a bounding box would seal it.
            foreach(var surface in surfaces)
            {
                var filter=surface.GetComponent<MeshFilter>();
                if(filter!=null && filter.sharedMesh!=null)surface.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            }
        }
        public static void ConfigureCompositions(WoodlandPreset preset)
        {
            preset.discoveries=new[]{Composition("Viking Runestones",0),Composition("Viking Abandoned Hall",1),Composition("Viking Quarry Camp",2)};
            EditorUtility.SetDirty(preset);
        }
        static GameObject Composition(string name, int kind)
        {
            var root = new GameObject(name);
            void Place(string path, Vector3 position, float angle, float height, Vector3 fit=default)
            {
                var part=new GameObject(System.IO.Path.GetFileName(path));part.transform.SetParent(root.transform,false);
                var go=Object.Instantiate(Source(Viking+path+".prefab"),part.transform,false);StripColliders(go);
                var bounds=BoundsOf(go);
                float uniform=height/Mathf.Max(.01f,bounds.size.y);
                Vector3 scale=fit==Vector3.zero?Vector3.one*uniform:new Vector3(fit.x/bounds.size.x,fit.y/bounds.size.y,fit.z/bounds.size.z);
                go.transform.localScale=Vector3.Scale(go.transform.localScale,scale);
                go.transform.localPosition-=Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
                var collider=part.AddComponent<BoxCollider>();collider.size=Vector3.Scale(bounds.size,scale);collider.center=Vector3.up*collider.size.y*.5f;
                part.transform.localPosition=position;part.transform.localRotation=Quaternion.Euler(0,angle,0);
            }
            if (kind == 0)
            {
                Place("Props/SM_Prop_RuneStone_01", new Vector3(-3.5f, 0, 2), 0, 4);
                Place("Props/SM_Prop_RuneStone_02", new Vector3(3.5f, 0, 2), 0, 2.8f);
                Place("Props/SM_Prop_Statue_01", new Vector3(0, 0, 5), 180, 4.5f);
            }
            else if (kind == 1)
            {
                Place("Buildings/SM_Bld_Wall_Logs_01", new Vector3(-3, 0, 3), 90, 2.8f);
                Place("Buildings/SM_Bld_Pillar_01", new Vector3(3, 0, 1), 0, 3.0f);
                Place("Buildings/SM_Bld_Pillar_01", new Vector3(3, 0, 5), 0, 3.0f);
                // Roof reads on the approach; open sides preserve navigation and the abandoned silhouette.
                Place("Buildings/SM_Bld_House_Roof_01", new Vector3(-1.7f, 2.8f, 3), 0, 2.2f,new Vector3(3.6f,2.2f,6));
                Place("Buildings/SM_Bld_House_Roof_01", new Vector3(1.7f, 2.8f, 3), 180, 2.2f,new Vector3(3.6f,2.2f,6));
                Place("Buildings/SM_Bld_Wall_Logs_Half_01", new Vector3(3, 0, 4), 0, 1.3f);
                Place("Props/SM_Prop_Table_01", new Vector3(1, 0, 2), 0, .8f);
                Place("Props/SM_Prop_Bed_01", new Vector3(-3, 0, 0), 0, .65f);
                AddPorchLantern(root);
            }
            else
            {
                Place("Props/SM_Prop_Anvil_01", new Vector3(-3, 0, 2), 0, .8f);
                Place("Props/SM_Prop_Weapon_Rack_01", new Vector3(3, 0, 3), 0, 1.5f);
                Place("Props/SM_Prop_Chest_01", new Vector3(0, 0, 4), 0, .7f);
            }
            if(kind==0)AddRuneGateway(root.transform);
            return Save(root, Root + name + ".prefab");
        }
        public static void ConfigureHallLantern()
        {
            string path=Root+"Viking Abandoned Hall.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{AddPorchLantern(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        static void AddPorchLantern(GameObject root)
        {
            var previous=root.transform.Find("Porch Lantern");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var lantern=(GameObject)PrefabUtility.InstantiatePrefab(Source("Assets/Topaz/Presentation/Rendering/Environment/Prototype Lantern.prefab"),root.transform);
            lantern.name="Porch Lantern";lantern.transform.localPosition=new Vector3(3,2.1f,.25f);
            foreach(var collider in lantern.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            var glow=lantern.GetComponentInChildren<Topaz.Player.LanternVisual>();
            if(glow==null)throw new InvalidOperationException("Porch lantern needs its authored flame binding.");
            var anchor=new GameObject("Porch Light");anchor.transform.SetParent(lantern.transform,false);
            anchor.transform.position=glow.LightPosition;
            var night=anchor.AddComponent<HomeNightLight>();var serialized=new SerializedObject(night);
            serialized.FindProperty("glow").objectReferenceValue=glow;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject FarTree(GameObject source)
        {
            var root=new GameObject(source.name+" Distant");
            var group=source.GetComponentInChildren<LODGroup>();
            var renderers=group!=null?group.GetLODs().Last().renderers:source.GetComponentsInChildren<Renderer>();
            foreach(var renderer in renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
                var part=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(root.transform,false);
                part.transform.localPosition=source.transform.InverseTransformPoint(renderer.transform.position);
                part.transform.localRotation=Quaternion.Inverse(source.transform.rotation)*renderer.transform.rotation;
                part.transform.localScale=renderer.transform.lossyScale;
                part.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;part.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                part.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return Save(root,Root+root.name+".prefab");
        }
        static GameObject Bridge()
        {
            var root=new GameObject("Viking River Bridge");
            var alignment=new GameObject("Bridge Geometry");alignment.transform.SetParent(root.transform,false);
            var model=Object.Instantiate(Source(Viking+"Buildings/SM_Bld_Bridge_01.prefab"),alignment.transform,false);
            StripColliders(model);
            // Rotate inside an unrotated fitting frame so non-uniform scale uses the crossing axes.
            if(BoundsOf(model).size.x>BoundsOf(model).size.z)model.transform.localRotation=Quaternion.Euler(0,90,0)*model.transform.localRotation;
            // Submerge the footings; the measured deck lip stays within the controller's 0.25 m step.
            SyntySampleSetup.Fit(alignment,new Vector3(3.5f,1,16),Vector3.down*.6f);
            var group=model.GetComponentInChildren<LODGroup>();
            var surfaces=group!=null?group.GetLODs()[0].renderers:model.GetComponentsInChildren<Renderer>();
            foreach(var surface in surfaces)
            {
                var mesh=surface.GetComponent<MeshFilter>();if(mesh==null)continue;
                surface.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
            }
            return Save(root,Root+root.name+".prefab");
        }
        static void ReplaceProp(GameObject root,string source,Vector3 size,Vector3 bottom)
        {
            var prior=root.transform.Find("Viking Model");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
            var model=Object.Instantiate(Source(Viking+source+".prefab"),root.transform,false);model.name="Viking Model";
            StripColliders(model);SyntySampleSetup.Fit(model,size,bottom);
        }
        static void ConfigureItems()
        {
            var visuals=new Dictionary<string,GameObject>();
            GameObject Visual(string name,string source,float height)
            {
                if(visuals.TryGetValue(name,out var existing))return existing;
                var root=new GameObject("Viking Item "+name);var model=Object.Instantiate(Source(source),root.transform,false);StripColliders(model);
                var b=BoundsOf(model);float scale=height/Mathf.Max(.01f,b.size.y);model.transform.localScale*=scale;model.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z)*scale;
                var prefab=Save(root,Root+root.name+".prefab");visuals[name]=prefab;return prefab;
            }
            foreach(var guid in AssetDatabase.FindAssets("t:ItemDefinition",new[]{"Assets/Topaz"}))
            {
                var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));string id=item.StableId;GameObject model=null;
                if(id.Contains("sword"))model=Visual("Sword",Viking+"Weapons/SM_Wep_Sword_01.prefab",.95f);
                else if(id.Contains("axe")&&!id.Contains("pickaxe"))model=Visual("Axe",Viking+"Weapons/SM_Wep_Axe_01.prefab",.75f);
                else if(id.Contains("shield"))model=Visual("Shield",Viking+"Weapons/SM_Wep_Shield_01.prefab",.65f);
                else if(id.Contains("helm"))model=Visual("Helm",Viking+"Characters/Chr_Attach/SM_Chr_Attach_Helmet_01.prefab",.3f);
                else if(id=="material.wood")model=Visual("Wood",Alpine+"SM_Env_Branch_01.prefab",.2f);
                else if(id=="material.stone")model=Visual("Stone",Alpine+"SM_Env_Rock_Small_02.prefab",.25f);
                else if(id=="food.mushroom-stew")model=Visual("Stew",Viking+"Props/SM_Prop_Bowl_01.prefab",.2f);
                if(model==null)continue;
                SyntySampleSetup.Set(item,"worldVisual",model);
                if(item.Weapon!=null)SyntySampleSetup.Set(item.Weapon,"heldModel",model);
                EditorUtility.SetDirty(item);
            }
        }
        static void ConfigureBuildings()
        {
            var settings = BuildingSettings.Current; var entries = settings.visuals.ToList();
            void Bind(string id, string source, Vector3 size, Vector3 bottom = default)
            {
                var root = new GameObject("Viking " + id); var model = Object.Instantiate(Source(Viking + source + ".prefab"), root.transform, false);
                StripColliders(model); SyntySampleSetup.Fit(model, size, bottom);
                var c = root.AddComponent<BoxCollider>(); c.size = size; c.center = bottom + Vector3.up * size.y / 2;
                if (id == BuildCatalog.Roof) root.AddComponent<BuildingVisualBinding>().roof = true;
                if (id == BuildCatalog.Lantern) root.AddComponent<HomeNightLight>();
                var prefab = Save(root, Root + "Viking " + id + ".prefab");
                entries.RemoveAll(e => e.id == id); entries.Add(new BuildingSettings.VisualEntry { id = id, prefab = prefab });
            }
            Bind(BuildCatalog.Wall, "Buildings/SM_Bld_Wall_Logs_01", new Vector3(1.5f, 2.3f, .17f));
            Bind(BuildCatalog.Roof, "Buildings/SM_Bld_House_Roof_Quarter_01", new Vector3(1.6f, .28f, 1.6f), Vector3.up * 2.3f);
            Bind(BuildCatalog.Bed, "Props/SM_Prop_Bed_01", new Vector3(1, .55f, 1.8f));
            Bind(BuildCatalog.Table, "Props/SM_Prop_Table_01", new Vector3(1.2f, .75f, .7f));
            Bind(BuildCatalog.Lantern, "Props/SM_Prop_Lantern_01", new Vector3(.28f, .5f, .28f));
            Bind(BuildCatalog.TimberFloor, "Buildings/SM_Bld_Dock_Floor_01", new Vector3(1.5f, .14f, 1.5f));
            Bind(BuildCatalog.HalfWall, "Buildings/SM_Bld_Wall_Logs_Half_01", new Vector3(1.5f, 1.1f, .17f));
            Bind(BuildCatalog.Beam, "Buildings/SM_Bld_Pillar_02", new Vector3(.2f, 2.3f, .2f));
            Bind(BuildCatalog.Fence, "Buildings/SM_Bld_Fence_01", new Vector3(1.5f, 1, .16f));
            Bind(BuildCatalog.Bench, "Props/SM_Prop_Bench_01", new Vector3(1.2f, .5f, .4f));
            Bind(BuildCatalog.Chair, "Props/SM_Prop_Chair_01", new Vector3(.55f, 1, .55f));
            Bind(BuildCatalog.Shelf, "Props/SM_Prop_Shelf_01", new Vector3(1, 1.5f, .4f));
            Bind(BuildCatalog.WeaponRack, "Props/SM_Prop_Weapon_Rack_01", new Vector3(1.2f, 1.4f, .4f));
            var campSource=settings.VisualFor(BuildCatalog.Camp);
            if(campSource!=null)
            {
                string campPath=AssetDatabase.GetAssetPath(campSource);var camp=PrefabUtility.LoadPrefabContents(campPath);
                ReplaceProp(camp,"Props/SM_Prop_Fire_Pit_01",new Vector3(1.3f,.4f,1.3f),Vector3.zero);
                PrefabUtility.SaveAsPrefabAsset(camp,campPath);PrefabUtility.UnloadPrefabContents(camp);
            }
            var doorwaySource=settings.VisualFor(BuildCatalog.Doorway);
            if(doorwaySource!=null)
            {
                string path=AssetDatabase.GetAssetPath(doorwaySource);var door=PrefabUtility.LoadPrefabContents(path);
                var binding=door.GetComponent<BuildingVisualBinding>();
                foreach(var renderer in door.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
                var oldFrame=door.transform.Find("Viking Door Frame");if(oldFrame!=null)Object.DestroyImmediate(oldFrame.gameObject);
                var oldPanel=binding.doorPanel.Find("Viking Door Panel");if(oldPanel!=null)Object.DestroyImmediate(oldPanel.gameObject);
                var frame=Object.Instantiate(Source(Viking+"Buildings/SM_Bld_Wall_Logs_Door_01.prefab"),door.transform,false);frame.name="Viking Door Frame";StripColliders(frame);
                SyntySampleSetup.Fit(frame,new Vector3(1.5f,2.3f,.2f),Vector3.zero);
                var panel=Object.Instantiate(Source(Viking+"Buildings/SM_Bld_Door_02.prefab"),binding.doorPanel,false);panel.name="Viking Door Panel";StripColliders(panel);
                SyntySampleSetup.Fit(panel,new Vector3(1.06f,1.8f,.09f),Vector3.right*.53f);
                PrefabUtility.SaveAsPrefabAsset(door,path);PrefabUtility.UnloadPrefabContents(door);
            }
            settings.visuals = entries.ToArray(); EditorUtility.SetDirty(settings);
        }
        static GameObject[] ConfigureCharacters()
        {
            var template = Source(Characters + "Storybook Knight.prefab");
            var driverSource = template.GetComponent<CharacterAnimationDriver>();
            var templateData = new SerializedObject(driverSource);
            var result = new List<GameObject>();
            foreach (string category in new[] { "Peasant", "Warrior", "Leader" })
                for (int variant = 1; variant <= (category == "Leader" ? 1 : 2); variant++)
                    foreach (string sex in new[] { "Male", "Female" })
                    {
                        var go = Object.Instantiate(Source(Viking + "Characters/SM_Chr_" + category + "_" + sex + "_0" + variant + ".prefab"));
                        string label = CharacterLooks.Label(CharacterLooks.All[result.Count]); go.name = label;
                        var animator = go.GetComponent<Animator>();
                        if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid) throw new InvalidOperationException("Invalid Viking rig: " + label);
                        animator.runtimeAnimatorController = template.GetComponent<Animator>().runtimeAnimatorController; animator.applyRootMotion = false;
                        var visual = go.AddComponent<CharacterVisual>();
                        void Set(string field, Object value) => SyntySampleSetup.Set(visual, field, value);
                        Transform Bone(HumanBodyBones bone) => animator.GetBoneTransform(bone);
                        Set("animator", animator); Set("bodyRenderer", go.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.StartsWith("SM_Chr_" + category)));
                        Set("rightHand", Bone(HumanBodyBones.RightHand)); Set("leftHand", Bone(HumanBodyBones.LeftHand));
                        Set("guardUpperArm", Bone(HumanBodyBones.LeftUpperArm)); Set("guardLowerArm", Bone(HumanBodyBones.LeftLowerArm));
                        var lantern = new GameObject("Lantern Anchor").transform; lantern.SetParent(Bone(HumanBodyBones.Hips), false); lantern.position = new Vector3(go.transform.position.x+.28f,visual.BodyRenderer.bounds.min.y+visual.BodyRenderer.bounds.size.y*.55f,go.transform.position.z-.05f); Set("lanternAnchor", lantern);
                        var driver = go.AddComponent<CharacterAnimationDriver>(); EditorUtility.CopySerialized(driverSource, driver);
                        var data = new SerializedObject(driver);
                        foreach (string field in new[] { "player", "playerCombat", "enemy", "enemyAgent" }) data.FindProperty(field).objectReferenceValue = null;
                        foreach (string field in new[] { "swordVisual", "axeVisual", "pickaxeVisual", "combatAxeVisual", "staffVisual", "crossbowVisual", "shieldVisual" })
                        {
                            var old = templateData.FindProperty(field).objectReferenceValue as GameObject;
                            if (old == null) throw new InvalidOperationException("Missing equipment template: " + field);
                            var held = Object.Instantiate(old, Bone(field == "shieldVisual" ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand), false);
                            string asset = field == "swordVisual" ? "SM_Wep_Sword_01" : field == "axeVisual" ? "SM_Wep_Axe_01" : field == "combatAxeVisual" ? "SM_Wep_Axe_04" : field == "shieldVisual" ? "SM_Wep_Shield_01" : null;
                            if (asset != null)
                            {
                                foreach (Transform child in held.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                                foreach (var filter in held.GetComponents<MeshFilter>()) Object.DestroyImmediate(filter);
                                foreach (var renderer in held.GetComponents<MeshRenderer>()) Object.DestroyImmediate(renderer);
                                var model = Object.Instantiate(Source(Viking + "Weapons/" + asset + ".prefab"), held.transform, false); StripColliders(model);
                                SyntySampleSetup.Fit(model, field == "shieldVisual" ? new Vector3(.6f, .65f, .12f) : field == "swordVisual" ? new Vector3(.16f, .95f, .045f) : new Vector3(.42f, field == "combatAxeVisual" ? 1.1f : .7f, .12f), Vector3.down * .12f);
                            }
                            held.SetActive(false); data.FindProperty(field).objectReferenceValue = held;
                            if (field == "shieldVisual") Set("shield", held);
                        }
                        data.ApplyModifiedPropertiesWithoutUndo(); go.AddComponent<ShieldGuardPose>();
                        foreach(var unused in go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>!r.gameObject.activeInHierarchy).ToArray())Object.DestroyImmediate(unused.gameObject);
                        result.Add(Save(go, Characters + label + ".prefab"));
                    }
            return result.ToArray();
        }
    }
}
