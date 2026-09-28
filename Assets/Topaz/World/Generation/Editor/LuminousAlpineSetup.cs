using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    /// <summary>Repeatable authoring of owned wrappers; source geometry and textures remain private dependencies.</summary>
    public static class LuminousAlpineSetup
    {
        const string Root="Assets/Topaz/Presentation/Art/World/";
        const string Cover=Root+"GroundCover/";
        [MenuItem("Topaz/Generation/Configure Luminous Alpine")]
        public static void Apply()
        {
            FantasyPresentationSetup.Configure(false);
            if(!AssetDatabase.IsValidFolder(Cover.TrimEnd('/')))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"GroundCover");
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            BindTerrain(preset);
            preset.groundCover=new[]{
                Detail("Low Grass","Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_Grass_01.prefab",new Vector2(.85f,1.25f),new Vector2(.38f,.6f)),
                Detail("Upright Grass","Assets/Synty/PNB_Core/Prefabs/SM_Env_Grass_01.prefab",new Vector2(.40f,.7f),new Vector2(.65f,1.05f)),
                Detail("Shade Grass","Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_Grass_01.prefab",new Vector2(.65f,1f),new Vector2(.55f,.85f)),
                Detail("Meadow Flowers","Alpine Bush_Flower_01_Alt",new Vector2(.45f,.75f),new Vector2(.30f,.5f))};
            foreach(var prefab in preset.trees.Concat(preset.distantTrees).Concat(preset.undergrowth).Concat(preset.rocks).Concat(preset.cliffs).Distinct())
            {
                string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>OwnMaterial(m,Cover)).ToArray();
                    foreach(var group in root.GetComponentsInChildren<LODGroup>())
                    {
                        group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=false;
                        var lods=group.GetLODs();for(int i=0;i<lods.Length;i++)lods[i].fadeTransitionWidth=.18f;group.SetLODs(lods);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            ConfigureTreeLods();
            AlpineAssetSetup.ConfigureCompositions(preset);
            ConfigureCarriedLantern();
            ConfigureCharacterMotion();
            ConfigureCharacterStudies();
            AlpineBackdropSetup.Apply();
            ConfigureStarterCamp();
            preset.settings=WoodlandSettings.LargeWorld();preset.ValidateContent();EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();SurfaceCacheSetup.ConfigureModelReadability();SurfaceCacheSetup.ConfigureVolumeDefaults();
            Debug.Log("[Topaz/Art] Authored Alpine-v5 cover, grove materials and landmark compositions.");
        }
        [MenuItem("Topaz/Generation/Configure Native Tree LODs")]
        public static void ConfigureTreeLods()
        {
            var preset=AssetDatabase.LoadAssetAtPath<WoodlandPreset>("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            var shader=WorldShaderSetup.NativeFoliageLods();
            var grassShader=WorldShaderSetup.NativeFoliageLods(true);
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Cover.TrimEnd('/')}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(material.shader!=shader && AssetDatabase.GetAssetPath(material.shader)!="Assets/Synty/PNB_Core/Shaders/Foliage.shadergraph")continue;
                foreach(string feature in new[]{"_Enable_Emission","_Enable_Frosting","_Enable_Pulse","_Enable_Back_Face_Lighting"})
                    if(material.HasProperty(feature) && material.GetFloat(feature)>.5f)
                        throw new InvalidOperationException(material.name+" requires a feature excluded from the specialized foliage shader: "+feature);
                material.shader=shader;material.DisableKeyword("LOD_FADE_CROSSFADE");
                material.SetShaderPassEnabled("MotionVectors",true);material.SetShaderPassEnabled("MOTIONVECTORS",true);
                material.SetFloat("_TopazGroundCover",0);EditorUtility.SetDirty(material);
            }
            foreach(var prefab in preset.trees.Concat(preset.distantTrees).Concat(preset.groundCover.Select(v=>v.prefab)).Distinct())
            {
                string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                        foreach(var material in renderer.sharedMaterials)
                            if(material!=null && (material.shader==shader || material.shader==grassShader || material.shader.name.Contains("Foliage")))
                            {
                                bool grass=preset.groundCover.Take(3).Any(v=>v.prefab==prefab);
                                material.shader=grass?grassShader:shader;material.DisableKeyword("LOD_FADE_CROSSFADE");material.SetShaderPassEnabled("MotionVectors",true);material.SetShaderPassEnabled("MOTIONVECTORS",true);
                                material.SetFloat("_TopazGroundCover",preset.groundCover.Any(v=>v.prefab==prefab)?1:0);EditorUtility.SetDirty(material);
                            }
                    foreach(var group in root.GetComponentsInChildren<LODGroup>())
                    {
                        var original=group.GetLODs();if(original.Length<2)continue;
                        int Vertices(LOD lod)=>lod.renderers.Where(r=>r!=null).Sum(r=>r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount??0);
                        var kept=new System.Collections.Generic.List<LOD>{original[0]};
                        for(int i=1;i<original.Length-1;i++)
                            if(Vertices(original[i])<Vertices(kept[kept.Count-1])*.7f)kept.Add(original[i]);
                        kept.Add(original[original.Length-1]);
                        for(int i=0;i<kept.Count;i++)
                        {
                            var lod=kept[i];lod.screenRelativeTransitionHeight=i==kept.Count-1?.005f:i==kept.Count-2?.32f:.60f;
                            lod.fadeTransitionWidth=.20f;kept[i]=lod;
                        }
                        var retained=kept.SelectMany(l=>l.renderers).ToHashSet();
                        foreach(var renderer in original.SelectMany(l=>l.renderers).Where(r=>r!=null))renderer.enabled=retained.Contains(renderer);
                        // Native SCGI 17.6 registers enabled renderers independently of LOD selection.
                        // One stable 3D representation contributes; overlapping visual LODs/cards must not all occlude GI.
                        var contributors=kept[kept.Count-2].renderers.ToHashSet();
                        foreach(var renderer in retained)
                            if(renderer!=null)renderer.renderingLayerMask=contributors.Contains(renderer)?1u:Topaz.Rendering.SurfaceCacheLighting.VisualOnlyRenderingLayer;
                        group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=false;group.SetLODs(kept.ToArray());
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
        }
        static void ConfigureStarterCamp()
        {
            const string path="Assets/Topaz/World/Scenes/Bootstrap.unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var lantern in root.GetComponentsInChildren<Topaz.Player.PlayerLantern>(true))
                    {var data=new SerializedObject(lantern);data.FindProperty("lightIntensity").floatValue=2f;data.ApplyModifiedPropertiesWithoutUndo();}
                    foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if(t.name=="Prototype Bed" && t.GetComponent<MeshRenderer>()!=null)
                        {
                            t.GetComponent<MeshRenderer>().enabled=false;
                            if(t.Find("Viking Bed")==null)
                            {
                                var bed=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonVikingRealm/Prefabs/Props/SM_Prop_Bed_01.prefab"),t,false);bed.name="Viking Bed";
                                foreach(var collider in bed.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                                SyntySampleSetup.Fit(bed,Vector3.one,Vector3.down*.5f);
                            }
                        }
                        if(t.name=="Activation Ring" && t.TryGetComponent<LineRenderer>(out var line))
                        {
                            const string materialPath="Assets/Topaz/Presentation/Effects/Materials/Hearth Presence.mat";
                            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,materialPath);}
                            material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
                            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=(int)RenderQueue.Transparent;material.SetColor("_BaseColor",new Color(1,.66f,.25f,.28f));
                            line.sharedMaterial=material;line.startColor=line.endColor=Color.white;line.widthMultiplier=.035f;EditorUtility.SetDirty(material);
                        }
                    }
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
        }
        static void ConfigureCharacterStudies()
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonVikingRealm/Materials/Alts/PolygonVikingRealm_01_A.mat");
            foreach(bool rich in new[]{false,true})
            {
                string path="Assets/Topaz/Presentation/Effects/Resources/TopazCharacter"+(rich?"Rich":"Soft")+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Topaz/Sheltered Lit"));AssetDatabase.CreateAsset(material,path);}
                material.SetTexture("_BaseMap",source.GetTexture("_Base_Texture"));material.SetColor("_BaseColor",Color.white);
                material.SetTexture("_BumpMap",source.GetTexture("_Normal_Texture"));material.SetFloat("_BumpScale",rich?.9f:.45f);material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_MetallicGlossMap",source.GetTexture("_Metallic_Smoothness_Texture"));material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",rich?.6f:.3f);material.enableInstancing=true;
                EditorUtility.SetDirty(material);
            }
        }
        public static void ConfigureCharacterMotion()
        {
            const string folder="Assets/Topaz/Presentation/Art/Characters/";
            foreach(string id in Topaz.Gameplay.CharacterLooks.All)
            {
                string path=folder+Topaz.Gameplay.CharacterLooks.Label(id)+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if(renderer is SkinnedMeshRenderer skin)skin.skinnedMotionVectors=true;
                        renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>
                        {
                            if(source==null || source.FindPass("MotionVectors")<0)return source;
                            string materialPath=AssetDatabase.GetAssetPath(source);
                            Material owned=source;
                            if(!materialPath.StartsWith(folder+"Motion ",StringComparison.Ordinal))
                            {
                                materialPath=folder+"Motion "+source.name+".mat";
                                owned=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                                if(owned==null){owned=new Material(source);AssetDatabase.CreateAsset(owned,materialPath);}
                            }
                            owned.SetShaderPassEnabled("MotionVectors",true);owned.SetShaderPassEnabled("MOTIONVECTORS",true);
                            owned.DisableKeyword("LOD_FADE_CROSSFADE");EditorUtility.SetDirty(owned);return owned;
                        }).ToArray();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            ConfigureStarterCamp();AssetDatabase.SaveAssets();
        }
        static void ConfigureCarriedLantern()
        {
            const string path="Assets/Topaz/Presentation/Rendering/Environment/Prototype Lantern.prefab";
            var lantern=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(Transform child in lantern.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonVikingRealm/Prefabs/Props/SM_Prop_Lantern_01.prefab"),lantern.transform,false);
                foreach(var collider in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                SyntySampleSetup.Fit(model,new Vector3(.18f,.28f,.18f),Vector3.down*.28f);
                var glow=GameObject.CreatePrimitive(PrimitiveType.Sphere);glow.name="Warm Lantern Flame";glow.transform.SetParent(lantern.transform,false);
                Object.DestroyImmediate(glow.GetComponent<Collider>());glow.transform.localPosition=Vector3.down*.14f;glow.transform.localScale=new Vector3(.065f,.095f,.065f);
                const string materialPath="Assets/Topaz/Presentation/Effects/Materials/Carried Lantern Warmth.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,materialPath);}
                material.SetColor("_BaseColor",new Color(3.4f,1.55f,.32f));EditorUtility.SetDirty(material);
                glow.GetComponent<Renderer>().sharedMaterial=material;
                var binding=lantern.GetComponent<Topaz.Player.LanternVisual>();
                var data=new SerializedObject(binding);data.FindProperty("ember").objectReferenceValue=glow.GetComponent<Renderer>();data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lantern,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(lantern);}
            foreach(string id in Topaz.Gameplay.CharacterLooks.All)
            {
                string characterPath="Assets/Topaz/Presentation/Art/Characters/"+Topaz.Gameplay.CharacterLooks.Label(id)+".prefab";
                var root=PrefabUtility.LoadPrefabContents(characterPath);
                try
                {
                    var visual=root.GetComponent<Topaz.Characters.CharacterVisual>();
                    var bounds=visual.BodyRenderer.bounds;
                    visual.LanternAnchor.position=new Vector3(root.transform.position.x+.28f,bounds.min.y+bounds.size.y*.55f,root.transform.position.z-.05f);
                    PrefabUtility.SaveAsPrefabAsset(root,characterPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        static void BindTerrain(WoodlandPreset preset)
        {
            var layers=new[]{preset.grass,preset.path,preset.rockLayer,preset.forestLayer};
            var names=new[]{"Grass_01","Dirt_01","RiverRocks_01","Pine_Dirt"};
            // Owned TerrainLayer tints keep the ground underneath fading foliage in the same palette.
            // The supplied pine layers are autumn-brown; retain their texture detail as mossy floor.
            var tints=new[]{new Vector4(.65f,1,.8f,1),Vector4.one,Vector4.one,new Vector4(.34f,1,.55f,1)};
            preset.distantGroundColors=new Color[4];
            for(int i=0;i<layers.Length;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Terrain/"+names[i]+".terrainlayer");
                if(source==null)throw new InvalidOperationException("Missing Alpine terrain layer: "+names[i]);
                string name=layers[i].name;EditorUtility.CopySerialized(source,layers[i]);layers[i].name=name;
                layers[i].tileSize=Vector2.one*(i==2?4:3);layers[i].normalScale=.65f;layers[i].smoothness=.08f;
                layers[i].diffuseRemapMin=Vector4.zero;layers[i].diffuseRemapMax=tints[i];
                EditorUtility.SetDirty(layers[i]);
                var rt=RenderTexture.GetTemporary(16,16,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
                var previous=RenderTexture.active;var sample=new Texture2D(16,16,TextureFormat.RGBAFloat,false,true);
                try
                {
                    Graphics.Blit(source.diffuseTexture,rt);RenderTexture.active=rt;sample.ReadPixels(new Rect(0,0,16,16),0,0);sample.Apply();
                    var pixels=sample.GetPixels();Color average=Color.clear;foreach(var pixel in pixels)average+=pixel;average/=pixels.Length;average.a=1;
                    preset.distantGroundColors[i]=new Color(average.r*tints[i].x,average.g*tints[i].y,average.b*tints[i].z,1);
                }
                finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(sample);}
            }
        }
        static GroundCoverPrototype Detail(string name,string source,Vector2 width,Vector2 height)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source.StartsWith("Assets/")?source:Root+source+".prefab");
            var filters=prefab.GetComponentsInChildren<MeshFilter>();
            // Tiny repeated plants use the artist-authored reduced mesh; silhouettes and alpha cards remain intact.
            int budget=name=="Meadow Flowers"?600:64;
            var filter=filters.Where(f=>f.sharedMesh.vertexCount<=budget).OrderByDescending(f=>f.sharedMesh.vertexCount).FirstOrDefault()??filters.First();
            var sourceMesh=filter.sharedMesh;var bounds=sourceMesh.bounds;
            // Derived meshes stay ignored because they contain licensed source geometry.
            var mesh=Object.Instantiate(sourceMesh);mesh.name=name;
            var vertices=mesh.vertices;
            float horizontal=Mathf.Max(.001f,Mathf.Max(bounds.size.x,bounds.size.z)),vertical=Mathf.Max(.001f,bounds.size.y);
            for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3((vertices[i].x-bounds.center.x)/horizontal,(vertices[i].y-bounds.min.y)/vertical,(vertices[i].z-bounds.center.z)/horizontal);
            mesh.vertices=vertices;
            // Inverse transpose for the nonuniform normalization; preserve the source's faceted normals.
            var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=Vector3.Scale(normals[i],new Vector3(horizontal,vertical,horizontal)).normalized;mesh.normals=normals;
            mesh.RecalculateBounds();mesh.RecalculateTangents();
            string meshPath=Cover+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            try
            {
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var material=OwnMaterial(filter.GetComponent<Renderer>().sharedMaterial,Cover);
                {
                    string materialPath=Cover+name+" Surface.mat";
                    var variant=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(variant==null){variant=new Material(material);AssetDatabase.CreateAsset(variant,materialPath);}
                    Color color=name=="Upright Grass"?new Color(.24f,.46f,.21f):name=="Shade Grass"?new Color(.13f,.33f,.19f):new Color(.18f,.4f,.19f);
                    if(name!="Meadow Flowers")GrassPalette(variant,color);material=variant;
                }
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;
                var result=PrefabUtility.SaveAsPrefabAsset(go,Cover+name+".prefab");
                return new GroundCoverPrototype{prefab=result,width=width,height=height};
            }
            finally{Object.DestroyImmediate(go);}
        }
        static void GrassPalette(Material material,Color color)
        {
            if(material.HasProperty("_Leaf_Flat_Color"))material.SetFloat("_Leaf_Flat_Color",1);
            if(material.HasProperty("_Leaf_Base_Color"))material.SetColor("_Leaf_Base_Color",color);
            if(material.HasProperty("_Use_Color_Noise"))material.SetFloat("_Use_Color_Noise",1);
            if(material.HasProperty("_Color_Noise_Small_Freq"))material.SetFloat("_Color_Noise_Small_Freq",.8f);
            if(material.HasProperty("_Color_Noise_Large_Freq"))material.SetFloat("_Color_Noise_Large_Freq",.06f);
            if(material.HasProperty("_Leaf_Noise_Color"))material.SetColor("_Leaf_Noise_Color",color*1.15f);
            if(material.HasProperty("_Leaf_Noise_Large_Color"))material.SetColor("_Leaf_Noise_Large_Color",color*.85f);
            material.enableInstancing=true;EditorUtility.SetDirty(material);
        }
        static Material OwnMaterial(Material source,string folder)
        {
            if(source==null)throw new InvalidOperationException("Missing foliage material.");
            string path=AssetDatabase.GetAssetPath(source);
            if(!path.StartsWith(folder,StringComparison.Ordinal))
            {
                path=folder+source.name+".mat";
                var owned=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(owned==null){owned=new Material(source);AssetDatabase.CreateAsset(owned,path);}source=owned;
            }
            if(source.HasProperty("_Color_Tint"))source.SetColor("_Color_Tint",new Color(.78f,.87f,.83f,1));
            source.enableInstancing=true;source.SetShaderPassEnabled("MotionVectors",true);
            if(source.HasProperty("_BaseColor"))source.SetColor("_BaseColor",new Color(.86f,1,.91f));
            if(source.HasProperty("_Leaf_Base_Color"))source.SetColor("_Leaf_Base_Color",
                source.name.StartsWith("Alpine_Grass",StringComparison.Ordinal)?new Color(.22f,.68f,.36f):new Color(.72f,.92f,.82f));
            if(source.name.StartsWith("Alpine_Grass",StringComparison.Ordinal))
            {
                source.SetFloat("_Leaf_Flat_Color",1);source.SetColor("_Leaf_Base_Color",new Color(.18f,.40f,.19f));
                source.SetFloat("_Use_Color_Noise",1);source.SetFloat("_Color_Noise_Small_Freq",.8f);source.SetFloat("_Color_Noise_Large_Freq",.06f);
                source.SetColor("_Leaf_Noise_Color",new Color(.25f,.48f,.19f));source.SetColor("_Leaf_Noise_Large_Color",new Color(.12f,.32f,.19f));
            }
            foreach(string property in new[]{"_Enable_Frosting","_FrostingSwitch"})if(source.HasProperty(property))source.SetFloat(property,0);
            EditorUtility.SetDirty(source);return source;
        }
    }
}
