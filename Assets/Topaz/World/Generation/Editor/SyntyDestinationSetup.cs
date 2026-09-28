using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    /// <summary>Owns playable landmark compositions; never edits the purchased prefabs.</summary>
    public static class SyntyDestinationSetup
    {
        const string Root = "Assets/Topaz/Presentation/Art/World/Destinations/";
        const string Alpine = "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/";
        const string Viking = "Assets/Synty/PolygonVikingRealm/Prefabs/";
        const string Goblin = "Assets/Synty/PolygonGoblinWarCamp/Prefabs/";

        readonly struct Entry
        {
            public readonly DestinationKind Kind;
            public readonly string Source;
            public readonly float Height;
            public Entry(DestinationKind kind,string source,float height){Kind=kind;Source=source;Height=height;}
        }

        static readonly Entry[] entries =
        {
            new Entry(DestinationKind.CliffArch,Alpine+"SM_Env_Rock_Cliff_Arch_01.prefab",11),
            new Entry(DestinationKind.Lookout,Alpine+"Props/SM_Prop_Lookout_01.prefab",7),
            new Entry(DestinationKind.Cabin,Alpine+"Props/SM_Prop_Cabin_01.prefab",5),
            new Entry(DestinationKind.Jetty,Alpine+"Props/SM_Prop_Jetty_01.prefab",2),
            new Entry(DestinationKind.Fossil,Alpine+"Props/SM_Prop_Fossil_01.prefab",2.4f),
            new Entry(DestinationKind.Canoe,Alpine+"Props/SM_Prop_Canoe_01.prefab",1.1f),
            new Entry(DestinationKind.Beacon,Viking+"Props/SM_Prop_Beacon_01.prefab",8),
            new Entry(DestinationKind.Cairn,Viking+"Props/SM_Prop_Cairn_01.prefab",2.3f),
            new Entry(DestinationKind.Pyre,Viking+"Props/SM_Prop_Pyre_01.prefab",3.5f),
            new Entry(DestinationKind.WreckedBoat,Viking+"Props/SM_Prop_Boat_Destroyed_01.prefab",3.2f),
            new Entry(DestinationKind.Dock,Viking+"Buildings/SM_Bld_Dock_Wood_Straight_01.prefab",1.5f),
            new Entry(DestinationKind.Crane,Viking+"Props/SM_Prop_Crane_01.prefab",7),
            new Entry(DestinationKind.Statue,Viking+"Props/SM_Prop_Statue_02.prefab",6),
            new Entry(DestinationKind.GoblinGate,Goblin+"Buildings/SM_Bld_Gate_01.prefab",6),
            new Entry(DestinationKind.MineEntrance,Goblin+"Buildings/SM_Bld_Mine_Entrance_01.prefab",6),
            new Entry(DestinationKind.MineTower,Goblin+"Buildings/SM_Bld_Mine_Tower_01.prefab",13),
            new Entry(DestinationKind.TreeHouse,Goblin+"Buildings/SM_Bld_Tree_House_01.prefab",13),
            new Entry(DestinationKind.Effigy,Goblin+"Props/SM_Prop_Effigy_01.prefab",5),
            new Entry(DestinationKind.Shrine,Goblin+"Props/SM_Prop_Shrine_01.prefab",7),
            new Entry(DestinationKind.RuinArch,Goblin+"Props/SM_Prop_Ruins_Archway_01.prefab",6)
        };

        static GameObject Source(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
            throw new InvalidOperationException("Missing purchased landmark: "+path);

        static void EnsureReadable(string source)
        {
            foreach(var path in AssetDatabase.GetDependencies(source,true))
            {
                if(!path.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))continue;
                if(AssetImporter.GetAtPath(path) is not ModelImporter importer || importer.isReadable)continue;
                importer.isReadable=true;
                importer.SaveAndReimport();
            }
        }

        static Bounds BoundsOf(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)throw new InvalidOperationException("Landmark has no renderers: "+go.name);
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static void AddSurfaceCollision(GameObject model)
        {
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            var group=model.GetComponentInChildren<LODGroup>(true);
            var renderers=group!=null?group.GetLODs()[0].renderers:model.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)
            {
                var mesh=renderer.GetComponent<MeshFilter>();
                if(mesh==null || mesh.sharedMesh==null)continue;
                mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
            }
        }

        static GameObject Add(string source,Transform parent,Vector3 position,float height,float yaw=0,float pitch=0,float roll=0,bool collision=true)
        {
            var model=Object.Instantiate(Source(source),parent,false);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true).ToArray())
                if(renderer.name.EndsWith("_Shadow",StringComparison.Ordinal) &&
                    renderer.sharedMaterials.Any(material=>material!=null && material.FindPass("Meta")<0))
                    Object.DestroyImmediate(renderer.gameObject);
            if(source.StartsWith(Alpine,StringComparison.Ordinal) && source.Contains("Rock_Cliff"))
            {
                var moss=AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Materials/MossRock_Triplanar.mat");
                if(moss==null)throw new InvalidOperationException("Moss rock material is missing.");
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials=Enumerable.Repeat(moss,renderer.sharedMaterials.Length).ToArray();
            }
            var bounds=BoundsOf(model);
            float scale=height/Mathf.Max(.01f,bounds.size.y);
            model.transform.localScale*=scale;
            model.transform.localPosition=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            model.transform.localRotation=Quaternion.Euler(pitch,yaw,roll)*model.transform.localRotation;
            if(collision)AddSurfaceCollision(model);
            else foreach(var collider in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            return model;
        }

        static GameObject Save(GameObject root,string name)
        {
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+name+".prefab");
            Object.DestroyImmediate(root);
            if(prefab==null)throw new InvalidOperationException("Could not save destination "+name);
            return prefab;
        }

        static GameObject Grave(bool far)
        {
            var root=new GameObject(far?"Titan's Grave Distant":"Titan's Grave");
            string rib=Goblin+"Buildings/Parts/SM_Bld_Part_Bone_Rib_01.prefab";
            string spine=Goblin+"Buildings/Parts/SM_Bld_Part_Bone_Spine_01.prefab";
            string skull=Goblin+"Buildings/Parts/SM_Bld_Part_Bone_Skull_01.prefab";
            Add(spine,root.transform,new Vector3(0,-1,-13),13,90,0,0,!far);
            Add(spine,root.transform,new Vector3(0,-1,6),13,90,0,0,!far);
            foreach(int z in new[]{-8,6,20})
            {
                Add(rib,root.transform,new Vector3(-8,0,z),21,0,0,-18,!far);
                Add(rib,root.transform,new Vector3(8,0,z),21,180,0,18,!far);
            }
            Add(skull,root.transform,new Vector3(0,-4,-30),11,0,0,0,!far);
            if(!far)
            {
                Add(Alpine+"SM_Env_Rock_Cliff_02.prefab",root.transform,new Vector3(-18,-4,12),12,45,0,0,false);
                Add(Alpine+"SM_Env_Rock_Cliff_03.prefab",root.transform,new Vector3(19,-4,-10),10,220,0,0,false);
            }
            return Save(root,root.name);
        }

        static GameObject Peak(bool far)
        {
            var root=new GameObject(far?"Split Peak Distant":"Destination SplitPeak");
            Add(Alpine+"SM_Env_Rock_Cliff_Arch_01.prefab",root.transform,new Vector3(0,-5,-32),24,32,0,0,!far);
            Add(Alpine+"SM_Env_Rock_Cliff_04.prefab",root.transform,new Vector3(-48,-8,2),45,15,0,-12,!far);
            Add(Alpine+"SM_Env_Rock_Cliff_06.prefab",root.transform,new Vector3(48,-9,0),50,185,0,10,!far);
            return Save(root,root.name);
        }

        static GameObject Dock()
        {
            var root=new GameObject("Destination Dock");
            for(int i=0;i<3;i++)
                Add(Viking+"Buildings/SM_Bld_Dock_Floor_01.prefab",root.transform,
                    new Vector3(0,.18f,1.25f+i*2.5f),.22f);
            Add(Viking+"Buildings/SM_Bld_Dock_Wood_Straight_01.prefab",root.transform,
                new Vector3(0,-.1f,6.3f),2.4f);
            foreach(float x in new[]{-1.2f,1.2f})
                Add(Viking+"Buildings/SM_Bld_Dock_Pillar_01.prefab",root.transform,
                    new Vector3(x,-1.8f,6.3f),2.4f);
            return Save(root,root.name);
        }

        [MenuItem("Topaz/Generation/Author Synty Destinations")]
        public static void Apply()
        {
            if(!AssetDatabase.IsValidFolder(Root.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/Topaz/Presentation/Art/World","Destinations");
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            if(preset==null)throw new InvalidOperationException("Woodland preset is missing.");
            foreach(var entry in entries)EnsureReadable(entry.Source);
            foreach(var path in new[]{Viking+"Buildings/SM_Bld_Dock_Floor_01.prefab",
                Viking+"Buildings/SM_Bld_Dock_Pillar_01.prefab",
                Goblin+"Buildings/Parts/SM_Bld_Part_Bone_Rib_01.prefab",
                Goblin+"Buildings/Parts/SM_Bld_Part_Bone_Spine_01.prefab",
                Goblin+"Buildings/Parts/SM_Bld_Part_Bone_Skull_01.prefab",
                Alpine+"SM_Env_Rock_Cliff_Arch_01.prefab",Alpine+"SM_Env_Rock_Cliff_04.prefab",
                Alpine+"SM_Env_Rock_Cliff_06.prefab"})EnsureReadable(path);
            var catalog=new GameObject[(int)DestinationKind.SplitPeak+1];
            foreach(var entry in entries)
            {
                if(entry.Kind==DestinationKind.Dock)
                {catalog[(int)entry.Kind]=Dock();continue;}
                var root=new GameObject("Destination "+entry.Kind);
                Add(entry.Source,root.transform,Vector3.zero,entry.Height);
                catalog[(int)entry.Kind]=Save(root,root.name);
            }
            catalog[(int)DestinationKind.TitansGrave]=Grave(false);
            catalog[(int)DestinationKind.SplitPeak]=Peak(false);
            preset.destinations=catalog;
            preset.titansGraveDistant=Grave(true);
            preset.splitPeakDistant=Peak(true);
            preset.settings.version=WildernessPlan.Version;
            preset.settings.profileId=preset.settings.worldSize==2048?"alpine-2048-v6":"alpine-1024-v6";
            preset.settings.contentId="viking-alpine-v6";
            EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();
            Debug.Log("[Topaz/Generation] Authored 20 destination wrappers and two visitable megastructures.");
        }
    }
}
