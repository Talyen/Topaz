using System;
using System.Linq;
using Topaz.Combat;
using Topaz.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    public static class StorybookSetup
    {
        const string Root=SyntySampleSetup.WorldRoot;
        const string Vendor="Assets/Synty/PolygonGeneric/Prefabs/";
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        [MenuItem("Topaz/Generation/Configure Storybook Wilderness")]
        public static void ApplyAll()
        {
            Apply();
            StorybookCharacterSetup.Apply();
            Topaz.Editor.StorybookAudioSetup.Apply();
            Topaz.Editor.MenuSurfacePolishSetup.Apply();
            Topaz.Editor.StorybookUiSetup.Apply();
        }
        public static void Apply()
        {
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            // Match URP's TerrainLitShaderGUI: the numeric default alone does not author the shader variant.
            preset.terrainMaterial.enableInstancing=true;
            preset.terrainMaterial.SetFloat("_EnableInstancedPerPixelNormal",1);
            UnityEngine.Rendering.CoreUtils.SetKeyword(preset.terrainMaterial,"_TERRAIN_INSTANCED_PERPIXEL_NORMAL",true);
            EditorUtility.SetDirty(preset.terrainMaterial);
            GameObject Wrap(string name,string file,float height,bool tree=false,bool collision=true)=>SyntySampleSetup.Wrap(name,"Environment/"+file+".prefab",height,tree,collision);
            preset.trees=new[]{Wrap("Birch","SM_Gen_Env_Tree_01",7,true),Wrap("Oak","SM_Gen_Env_Tree_02",8,true),Wrap("Young Oak","SM_Gen_Env_Tree_03",6,true),preset.treeVisual,preset.treeVariant};
            preset.rocks=new[]{preset.rockVisual,Wrap("Moss Rock","SM_Gen_Env_Rock_03",1.3f),Wrap("High Rock","SM_Gen_Env_Rock_05",2.3f)};
            preset.undergrowth=new[]{Wrap("Fern","SM_Gen_Env_Fern_01",.55f,false,false),Wrap("Fern Tall","SM_Gen_Env_Fern_02",.7f,false,false),Wrap("Shrub","SM_Gen_Env_Bush_03",.8f,false,false),Wrap("Meadow Grass","SM_Gen_Env_Grass_04",.4f,false,false)};
            preset.detail=Detail();
            preset.grass=SyntySampleSetup.Layer("Meadow",new Color(.40f,.54f,.27f));
            preset.path=SyntySampleSetup.Layer("Earth",new Color(.50f,.39f,.26f));
            preset.rockLayer=SyntySampleSetup.Layer("Stone Ground",new Color(.52f,.54f,.47f));
            preset.forestLayer=SyntySampleSetup.Layer("Forest Floor",new Color(.32f,.42f,.23f));
            preset.settings=new WoodlandSettings {decorationCount=560};
            WorldShaderSetup.ConfigureFoliageObjectSpace();
            foreach(var prefab in preset.trees.Concat(preset.undergrowth).Append(preset.detail))ApplyWind(prefab);
            var far=AssetDatabase.LoadAssetAtPath<Material>(Root+"Distant Landscape.mat");
            if(far==null){far=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(far,Root+"Distant Landscape.mat");}
            far.SetColor("_BaseColor",new Color(.4f,.53f,.28f));far.SetFloat("_Smoothness",0);EditorUtility.SetDirty(far);preset.distantMaterial=far;
            preset.discoveries=new[]{Discovery("Old Orchard",0,preset),Discovery("Forgotten Wall",1,preset),Discovery("Stone Circle",2,preset)};
            var cache=SyntySampleSetup.Wrap("Discovery Cache","Props/SM_Gen_Prop_Chest_01.prefab",.8f,false,true);
            var cachePath=AssetDatabase.GetAssetPath(cache);var cacheRoot=PrefabUtility.LoadPrefabContents(cachePath);
            if(cacheRoot.GetComponent<DiscoveryCache>()==null)cacheRoot.AddComponent<DiscoveryCache>();
            preset.discoveryCache=PrefabUtility.SaveAsPrefabAsset(cacheRoot,cachePath);PrefabUtility.UnloadPrefabContents(cacheRoot);
            ConfigureBuildings();
            var woodland=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Woodland.unity");
            preset.enemies=All<EnemyCombatant>(woodland).GroupBy(e=>e.LootRole).Select(group=>group.First()).Select((e,i)=>
            {
                var copy=Object.Instantiate(e.gameObject);copy.name="Wilderness Skeleton "+i;copy.SetActive(false);
                var originalData=new SerializedObject(e);
                var originalTell=originalData.FindProperty("telegraph").objectReferenceValue as LineRenderer;
                if(originalTell==null)throw new InvalidOperationException("Enemy telegraph is missing: "+e.name);
                var tell=Object.Instantiate(originalTell,copy.transform,false);tell.name="Attack Telegraph";
                SyntySampleSetup.Set(copy.GetComponent<EnemyCombatant>(),"telegraph",tell);
                SyntySampleSetup.Set(copy.GetComponent<EnemyCombatant>(),"target",null);
                SyntySampleSetup.Set(copy.GetComponent<EnemyCombatant>(),"safeZone",null);
                return SyntySampleSetup.Save(copy,copy.name).GetComponent<EnemyCombatant>();
            }).ToArray();
            EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            foreach(var crossing in All<TrailCrossing>(scene))crossing.gameObject.SetActive(false);
            foreach(var cam in All<Camera>(scene))cam.farClipPlane=900;
            foreach(var camera in All<Topaz.Player.PlayerCamera>(scene))
            {var data=new SerializedObject(camera);data.FindProperty("maximumZoom").floatValue=9;data.ApplyModifiedPropertiesWithoutUndo();}
            foreach(var region in All<WoodlandRegion>(scene))
            {
                region.regionOffset=Vector3.zero;region.regionId=TopazSaveData.WildernessRegion;
                if(region.navigation==null)region.navigation=region.gameObject.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            }
            var journal=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Topaz/UI/Art/JournalBackground.png");
            if(journal==null)throw new InvalidOperationException("The selected Journal backdrop must remain a Sprite.");
            foreach(var hud in All<LoopHud>(scene))SyntySampleSetup.Set(hud,"homeJournalBackground",journal);
            foreach(var fire in All<Campfire>(scene))fire.Configure(fire.StableId,TopazSaveData.WildernessRegion,fire.StableId==Campfire.HomeId?"First Hearth":fire.TravelLabel);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Topaz/World/Scenes/Bootstrap.unity",true)};
            Topaz.Editor.CampfireTravelCatalogSetup.Refresh();
            AssetDatabase.SaveAssets();
        }
        static GameObject Detail()
        {
            // Terrain mesh details need a root MeshFilter/Renderer, not an empty wrapper parent.
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/Synty/PolygonGeneric/Models/SM_Gen_Env_Grass_04.fbx");
            if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+"Environment/SM_Gen_Env_Grass_04.prefab");
            var filter=source.GetComponentInChildren<MeshFilter>();var renderer=filter.GetComponent<MeshRenderer>();
            var root=new GameObject("Grass Detail",typeof(MeshFilter),typeof(MeshRenderer));
            root.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;root.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            return SyntySampleSetup.Save(root,"Grass Detail");
        }
        static void ApplyWind(GameObject prefab)
        {
            if(prefab==null)return;
            var path=AssetDatabase.GetAssetPath(prefab);var go=PrefabUtility.LoadPrefabContents(path);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var original=renderer.sharedMaterial;if(original==null)continue;
                var texture=original.HasProperty("_Albedo_Map")?original.GetTexture("_Albedo_Map"):original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):null;
                string materialPath=Root+"Wind "+original.name.Replace("Wind ","")+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Topaz/Presentation/Rendering/Environment/Foliage.shadergraph"));AssetDatabase.CreateAsset(material,materialPath);}
                material.enableInstancing=true;
                material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.05f);EditorUtility.SetDirty(material);renderer.sharedMaterial=material;
            }
            PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
        }
        static GameObject Discovery(string name,int kind,WoodlandPreset preset)
        {
            var root=new GameObject(name);
            void Place(GameObject prefab,Vector3 p,float rotation,float scale=1)
            {
                var go=SyntySampleSetup.Instance(prefab,root.transform);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(0,rotation,0);go.transform.localScale*=scale;
            }
            if(kind==0)
            {
                for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Place(preset.trees[i%3],new Vector3(Mathf.Cos(a)*8,0,Mathf.Sin(a)*8),i*43);}
                Place(AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+"Props/SM_Gen_Prop_Table_01.prefab"),new Vector3(3,0,2),25);
            }
            else if(kind==1)
            {
                var wall=AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+"Base/SM_Bld_Base_Wall_Destroyed_01.prefab");
                for(int i=0;i<3;i++)Place(wall,new Vector3((i-1)*3,0,4),i==2?90:0);
                Place(preset.rocks[2],new Vector3(-3,0,1),55);
            }
            else for(int i=0;i<7;i++) {float a=i*Mathf.PI*2/7;Place(preset.rocks[2],new Vector3(Mathf.Cos(a)*6,0,Mathf.Sin(a)*6),i*51,.8f+(i%3)*.2f);}
            for(int i=0;i<6;i++)Place(preset.undergrowth[i%preset.undergrowth.Length],new Vector3(-5+i*2,0,6),i*71);
            return SyntySampleSetup.Save(root,name);
        }
        static void ConfigureBuildings()
        {
            var settings=Resources.Load<BuildingSettings>("BuildingSettings");var entries=settings.visuals.ToList();
            void Bind(string id,string name,string source,Vector3 size,Vector3 bottom)
            {
                var root=new GameObject(name);var model=SyntySampleSetup.Instance(AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+source+".prefab"),root.transform);
                foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                SyntySampleSetup.Fit(model,size,bottom);var collider=root.AddComponent<BoxCollider>();collider.size=size;collider.center=bottom+Vector3.up*size.y*.5f;
                if(id==BuildCatalog.Roof)root.AddComponent<BuildingVisualBinding>().roof=true;
                var prefab=SyntySampleSetup.Save(root,name);entries.RemoveAll(v=>v.id==id);entries.Add(new BuildingSettings.VisualEntry{id=id,prefab=prefab});
            }
            Bind(BuildCatalog.Floor,"Stone Foundation","Base/SM_Bld_Base_Floor_01",new Vector3(1.5f,.14f,1.5f),Vector3.zero);
            Bind(BuildCatalog.Wall,"Timber Wall","Base/SM_Bld_Base_Wall_01",new Vector3(1.5f,2,.17f),Vector3.zero);
            Bind(BuildCatalog.Roof,"Shingled Roof","Base/SM_Bld_Base_Roof_Quarter_01",new Vector3(1.6f,.25f,1.6f),Vector3.up*2.3f);
            GameObject root=new GameObject("Synty Campfire");
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;
                StorybookCharacterSetup.AddProp(root.transform,Vendor+"Environment/SM_Gen_Env_Rock_03.prefab",new Vector3(.3f,.22f,.3f),new Vector3(Mathf.Cos(angle)*.48f,0,Mathf.Sin(angle)*.48f));
            }
            for(int i=0;i<3;i++)
            {
                var log=StorybookCharacterSetup.AddProp(root.transform,Vendor+"Environment/SM_Gen_Env_Log_01.prefab",new Vector3(.65f,.14f,.17f),Vector3.up*.1f);
                log.transform.localRotation=Quaternion.Euler(0,i*60,0);
            }
            var fire=root.AddComponent<Campfire>();var light=root.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.6f,.25f);light.intensity=3;light.range=8;
            var flame=new GameObject("Embers");flame.transform.SetParent(root.transform,false);flame.transform.localPosition=Vector3.up*.25f;
            var particles=flame.AddComponent<ParticleSystem>();var main=particles.main;main.startLifetime=1.1f;main.startSpeed=.65f;main.startSize=.06f;main.startColor=new Color(1,.48f,.12f);main.maxParticles=40;
            var emission=particles.emission;emission.rateOverTime=15;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.18f;shape.angle=8;shape.rotation=new Vector3(-90,0,0);
            flame.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Topaz/Presentation/Effects/Resources/TopazEffectsParticles.mat");
            SyntySampleSetup.Set(fire,"embers",particles);
            entries.RemoveAll(v=>v.id==BuildCatalog.Camp);entries.Add(new BuildingSettings.VisualEntry{id=BuildCatalog.Camp,prefab=SyntySampleSetup.Save(root,"Synty Campfire")});
            root=new GameObject("Synty Bed");
            StorybookCharacterSetup.AddProp(root.transform,Vendor+"Props/SM_Gen_Prop_Plank_01.prefab",new Vector3(1,.12f,1.8f),Vector3.up*.14f);
            StorybookCharacterSetup.AddProp(root.transform,Vendor+"Props/SM_Gen_Prop_Sack_01.prefab",new Vector3(.85f,.22f,1.55f),Vector3.up*.25f);
            var bedCollider=root.AddComponent<BoxCollider>();bedCollider.center=Vector3.up*.28f;bedCollider.size=new Vector3(1,.5f,1.8f);
            entries.RemoveAll(v=>v.id==BuildCatalog.Bed);entries.Add(new BuildingSettings.VisualEntry{id=BuildCatalog.Bed,prefab=SyntySampleSetup.Save(root,"Synty Bed")});
            root=new GameObject("Synty Doorway");
            StorybookCharacterSetup.AddProp(root.transform,"Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Buildings_DoorFrame_01P.prefab",new Vector3(1.5f,2,.2f),Vector3.zero);
            foreach(float x in new[]{-.65f,.65f}){var post=root.AddComponent<BoxCollider>();post.center=new Vector3(x,1,0);post.size=new Vector3(.2f,2,.2f);}
            var pivot=new GameObject("Door Hinge");pivot.transform.SetParent(root.transform,false);pivot.transform.localPosition=Vector3.left*.53f;
            StorybookCharacterSetup.AddProp(pivot.transform,"Assets/Synty/PolygonStarter/Prefabs/SM_Bld_Door_01.prefab",new Vector3(1.06f,1.8f,.09f),Vector3.right*.53f);
            var panel=pivot.AddComponent<BoxCollider>();panel.center=new Vector3(.53f,.9f,0);panel.size=new Vector3(1.06f,1.8f,.09f);
            var binding=root.AddComponent<BuildingVisualBinding>();binding.doorPanel=pivot.transform;binding.doorCollider=panel;
            entries.RemoveAll(v=>v.id==BuildCatalog.Doorway);entries.Add(new BuildingSettings.VisualEntry{id=BuildCatalog.Doorway,prefab=SyntySampleSetup.Save(root,"Synty Doorway")});
            settings.visuals=entries.ToArray();EditorUtility.SetDirty(settings);
        }
    }
}
