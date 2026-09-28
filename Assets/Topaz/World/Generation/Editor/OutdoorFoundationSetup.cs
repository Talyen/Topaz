using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering.Universal;

namespace Topaz.Generation.Editor
{
    /// <summary>Idempotent owned-asset authoring; never edits vendor assets.</summary>
    public static class OutdoorFoundationSetup
    {
        [MenuItem("Topaz/Generation/Configure Outdoor Foundation")]
        public static void Apply()
        {
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            preset.settings.version=WildernessPlan.Version;
            preset.settings.profileId="alpine-1024-v5";preset.settings.contentId="viking-alpine-v5";preset.settings.worldSize=1024;
            ConfigureCompositions(preset);
            var roles=new List<WoodlandAssetRole>();
            void Add(GameObject prefab,WoodlandRole role,float footprint,WoodlandCollision collision,Vector3 anchor=default)
            {
                var renderers=prefab.GetComponentsInChildren<Renderer>(true);
                var bounds=renderers.Length>0?renderers[0].bounds:new Bounds();
                foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                bool composition=role==WoodlandRole.Landmark||role==WoodlandRole.Camp||role==WoodlandRole.ResourceSite;
                roles.Add(new WoodlandAssetRole{prefab=prefab,role=role,footprint=footprint,clearance=role==WoodlandRole.Canopy?3:1,
                    collision=collision,groundAnchor=anchor,maximumSlope=role==WoodlandRole.Rock?.8f:.35f,
                    visualSize=bounds.size,randomYaw=!composition,scaleRange=composition?Vector2.one:new Vector2(.8f,1.2f),interactionAnchor=composition?Vector3.right*5:Vector3.zero});
            }
            foreach(var tree in preset.trees)Add(tree,WoodlandRole.Canopy,5,WoodlandCollision.Solid);
            foreach(var rock in preset.rocks)Add(rock,WoodlandRole.Rock,3,WoodlandCollision.Solid);
            foreach(var plant in preset.undergrowth)Add(plant,WoodlandRole.Undergrowth,1,WoodlandCollision.None);
            Add(preset.discoveries[0],WoodlandRole.Landmark,8,WoodlandCollision.Solid);
            Add(preset.discoveries[1],WoodlandRole.Camp,8,WoodlandCollision.Solid);
            Add(preset.discoveries[2],WoodlandRole.ResourceSite,8,WoodlandCollision.Solid);
            roles.Add(new WoodlandAssetRole{role=WoodlandRole.ShallowWater,footprint=17,clearance=10,collision=WoodlandCollision.WalkableSurface});
            preset.roles=roles.ToArray();preset.ValidateContent();EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();
            ConfigureRendering();
            WorldShaderSetup.ConfigureDistantGround();
            preset.distantMaterial.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Topaz/Presentation/Rendering/Environment/DistantGround.shadergraph");
            preset.distantMaterial.enableInstancing=true;
            EditorUtility.SetDirty(preset.distantMaterial);AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Outdoor v3 profile and semantic variants configured.");
        }
        static void ConfigureCompositions(WoodlandPreset preset)
        {
            const string root=SyntySampleSetup.WorldRoot;
            var landmark=new GameObject("Ridge Waystones");
            for(int i=0;i<3;i++)
            {
                var stone=(GameObject)PrefabUtility.InstantiatePrefab(preset.rocks[2],landmark.transform);
                stone.name="Waystone "+i;
                stone.transform.localPosition=new Vector3((i-1)*3,0,i==1?2:0);
                stone.transform.localRotation=Quaternion.Euler(0,i*53,0);stone.transform.localScale*=i==1?2.4f:1.5f;
            }
            preset.discoveries[0]=PrefabUtility.SaveAsPrefabAsset(landmark,root+"Ridge Waystones.prefab");UnityEngine.Object.DestroyImmediate(landmark);
            var camp=new GameObject("Abandoned Camp");
            var ruins=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Forgotten Wall.prefab");
            PrefabUtility.InstantiatePrefab(ruins,camp.transform);
            var settings=Topaz.Gameplay.BuildingSettings.Current;
            foreach(var id in new[]{Topaz.Gameplay.BuildCatalog.Bed,Topaz.Gameplay.BuildCatalog.Table})
            {
                var source=settings.VisualFor(id);if(source==null)throw new System.InvalidOperationException("Missing owned camp composition visual: "+id);
                var prop=(GameObject)PrefabUtility.InstantiatePrefab(source,camp.transform);
                prop.transform.localPosition=new Vector3(id==Topaz.Gameplay.BuildCatalog.Bed?-2:2,0,-2);
            }
            // Decoration only: no Campfire/RestPoint component and no additional protection zone.
            preset.discoveries[1]=PrefabUtility.SaveAsPrefabAsset(camp,root+"Abandoned Camp.prefab");UnityEngine.Object.DestroyImmediate(camp);
        }
        static void ConfigureRendering()
        {
            const string root="Assets/Topaz/Presentation/Rendering/Environment/";
            Material Material(string name,string shaderPath)
            {
                string path="Assets/Topaz/Presentation/Effects/Resources/"+name+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Shader>(root+shaderPath));AssetDatabase.CreateAsset(material,path);}
                return material;
            }
            var fog=Material("TopazOutdoorFog","OutdoorFog.shader");
            Material("TopazOutdoorSky","OutdoorSky.shader");Material("TopazOutdoorRain","OutdoorRain.shader");Material("TopazShallowWater","ShallowWater.shader");
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(root+"Woodland Renderer.asset");
            var feature=renderer.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f=>f.name=="Outdoor height fog");
            if(feature==null){feature=ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();feature.name="Outdoor height fog";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
            feature.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
            feature.fetchColorBuffer=false;feature.requirements=ScriptableRenderPassInput.Depth;feature.passMaterial=fog;
            feature.SetActive(true);feature.Create();EditorUtility.SetDirty(feature);EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
        }
    }
}
