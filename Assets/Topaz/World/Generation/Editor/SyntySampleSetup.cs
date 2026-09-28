using System;
using System.Linq;
using Topaz.Characters;
using Topaz.Gameplay;
using Topaz.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    /// <summary>Repeatable Starter Pack sample binding. Vendor files remain a private dependency.</summary>
    public static class SyntySampleSetup
    {
        internal const string WorldRoot = "Assets/Topaz/Presentation/Art/World/";
        internal const string CharacterRoot = "Assets/Topaz/Presentation/Art/Characters/";
        const string Root = WorldRoot;
        const string Vendor = "Assets/Synty/PolygonGeneric/Prefabs/";
        const string Env = "Assets/Topaz/Presentation/Rendering/Environment/";
        static Material wood, steel;
        [MenuItem("Topaz/Generation/Bind Synty Starter Sample")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Topaz/Presentation/Art")) AssetDatabase.CreateFolder("Assets/Topaz/Presentation", "Art");
            if (!AssetDatabase.IsValidFolder(Root.TrimEnd('/'))) AssetDatabase.CreateFolder("Assets/Topaz/Presentation/Art", "World");
            if (!AssetDatabase.IsValidFolder(CharacterRoot.TrimEnd('/'))) AssetDatabase.CreateFolder("Assets/Topaz/Presentation/Art", "Characters");
            wood = Material("Wood", new Color(.32f,.21f,.12f));
            steel = Material("Steel", new Color(.48f,.53f,.55f));
            var tree = Wrap("Pine", "Environment/SM_Gen_Env_Tree_Pine_01.prefab", 7, true);
            var variant = Wrap("Pine Variant", "Environment/SM_Gen_Env_Tree_Pine_02.prefab", 8, true);
            var rock = Wrap("Rock", "Environment/SM_Gen_Env_Rock_01.prefab", 1.2f, false);
            var grass = Wrap("Grass", "Environment/SM_Gen_Env_Grass_01.prefab", .35f, false, false);
            var mushroom = Wrap("Mushroom", "Environment/SM_Gen_Env_Mushroom_01.prefab", .4f, false, false);
            var bush = Wrap("Berry Bush", "Environment/SM_Gen_Env_Bush_01.prefab", .7f, false, false);
            ConfigureTable();
            var male = Character("Wanderer", "SM_Gen_Chr_Peasent_Male_01");
            var female = Character("Wanderer Female", "SM_Gen_Chr_Peasent_Female_01");
            var skeleton = Character("Skeleton", "SM_Gen_Chr_Skeleton_01");
            var preset = AssetDatabase.LoadAssetAtPath<WoodlandPreset>(Env + "Woodland.asset");
            preset.treeVisual=tree; preset.treeVariant=variant; preset.rockVisual=rock; preset.detail=grass;
            preset.terrainMaterial = TerrainMaterial();
            preset.grass=Layer("Meadow", new Color(.32f,.43f,.19f));
            preset.path=Layer("Earth", new Color(.40f,.30f,.19f));
            EditorUtility.SetDirty(preset);
            foreach (string name in new[]{"Bootstrap", "Woodland"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/"+name+".unity");
                foreach (var t in All<HarvestTree>(scene)) ReplaceResource(t,tree,"trunkCollider");
                foreach (var r in All<MiningRock>(scene)) ReplaceResource(r,rock,"rockCollider");
                foreach (var plant in All<ForagePlant>(scene))
                {
                    var so=new SerializedObject(plant); var old=so.FindProperty("harvestVisual").objectReferenceValue as GameObject;
                    if(old!=null && old!=plant.gameObject) Object.DestroyImmediate(old);
                    var v=Instance(plant.Mushrooms?mushroom:bush,plant.transform);
                    so.FindProperty("harvestVisual").objectReferenceValue=v;so.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach(var appearance in All<PlayerAppearance>(scene))
                {
                    var so=new SerializedObject(appearance);
                    var parent=(Transform)so.FindProperty("visualRoot").objectReferenceValue;
                    var old=so.FindProperty("rogueVisual").objectReferenceValue as GameObject;
                    if(old!=null)Object.DestroyImmediate(old);
                    var model=Instance(male,parent);so.FindProperty("rogueVisual").objectReferenceValue=model;
                    var looks=so.FindProperty("looks");
                    for(int i=0;i<looks.arraySize;i++) looks.GetArrayElementAtIndex(i).FindPropertyRelative("model").objectReferenceValue=i%2==0?male:female;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach(var enemy in All<EnemyCombatant>(scene))
                {
                    var so=new SerializedObject(enemy);var parent=(Transform)so.FindProperty("visualRoot").objectReferenceValue;
                    foreach(var old in parent.GetComponentsInChildren<CharacterVisual>(true))Object.DestroyImmediate(old.gameObject);
                    var model=Instance(skeleton,parent);
                    var definition=so.FindProperty("definition").objectReferenceValue as EnemyDefinition;
                    model.GetComponent<PrototypeHumanoidMotion>().enemyEquipment=definition!=null&&definition.CrossbowAttack!=null?"crossbow":enemy.LootRole==SkeletonLootRole.Mage?"staff":"sword";
                }
                foreach(var r in All<MeshRenderer>(scene))
                {
                    if(r==null)continue;
                    if(r.name=="Prototype Rack")
                    {
                        r.enabled=false;
                        var prior=r.transform.Find("Timber Rack");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
                        var rack=new GameObject("Timber Rack").transform;rack.SetParent(r.transform,false);
                        Cube(rack,"Left Post",new Vector3(-.4f,0,0),new Vector3(.1f,1,.2f),wood);
                        Cube(rack,"Right Post",new Vector3(.4f,0,0),new Vector3(.1f,1,.2f),wood);
                        Cube(rack,"Rail",new Vector3(0,.3f,0),new Vector3(.9f,.1f,.2f),wood);
                    }
                    if(r.name=="Hearth")r.enabled=false;
                    string source=r.name=="Hearth Stone"?"Environment/SM_Gen_Env_Rock_03.prefab":r.name=="Fire Log"?"Environment/SM_Gen_Env_Log_01.prefab":r.name=="Prototype Chest"?"Props/SM_Gen_Prop_Chest_01.prefab":r.name=="Prototype Workbench"?"Props/SM_Gen_Prop_Table_01.prefab":null;
                    if(source==null)continue;
                    r.enabled=false;
                    var old=r.transform.Find("Synty Model");if(old!=null)Object.DestroyImmediate(old.gameObject);
                    var model=Instance(AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+source),r.transform);model.name="Synty Model";
                    Fit(model,new Vector3(1,1,1),new Vector3(0,-.5f,0));
                    foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                }
                // The menu backdrop also previews the shared world palette.
                foreach(var go in scene.GetRootGameObjects())
                {
                    var backdrop=go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Woodland Menu Backdrop");
                    if(backdrop==null)continue;
                    foreach(Transform child in backdrop.Cast<Transform>().ToArray())
                    {
                        if(child.GetComponent<Renderer>()!=null) { child.GetComponent<Renderer>().sharedMaterial=Material("Menu Meadow",new Color(.32f,.43f,.19f)); continue; }
                        var pos=child.localPosition;var scale=child.localScale;Object.DestroyImmediate(child.gameObject);
                        var replacement=Instance(tree,backdrop);replacement.transform.localPosition=pos;replacement.transform.localScale=scale;
                        foreach(var t in replacement.GetComponentsInChildren<Transform>())t.gameObject.layer=5;
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
        }
        static T[] All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        internal static void Set(Object o,string field,Object value)
        { var so=new SerializedObject(o);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(o.name+" missing "+field);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
        internal static GameObject Instance(GameObject prefab,Transform parent)
        { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;return go; }
        static Material Material(string name,Color color)
        {
            var path=Root+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(m);return m;
        }
        internal static void Fit(GameObject model,Vector3 size,Vector3 bottom)
        {
            // Measure in the model parent's coordinates, independent of scene placement and scale.
            var parent=model.transform.parent;var p=parent.position;var q=parent.rotation;var scale=parent.localScale;
            parent.position=Vector3.zero;parent.rotation=Quaternion.identity;parent.localScale=Vector3.one;
            var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            var factor=new Vector3(size.x/Mathf.Max(.01f,b.size.x),size.y/Mathf.Max(.01f,b.size.y),size.z/Mathf.Max(.01f,b.size.z));
            model.transform.localScale=Vector3.Scale(model.transform.localScale,factor);
            model.transform.localPosition=bottom-Vector3.Scale(new Vector3(b.center.x,b.min.y,b.center.z),factor);
            parent.position=p;parent.rotation=q;parent.localScale=scale;
        }
        static void ConfigureTable()
        {
            var root=new GameObject("Camp Table");
            var model=Instance(AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+"Props/SM_Gen_Prop_Table_01.prefab"),root.transform);
            foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            Fit(model,new Vector3(1.2f,.9f,.8f),Vector3.zero);
            var collider=root.AddComponent<BoxCollider>();collider.center=Vector3.up*.45f;collider.size=new Vector3(1.2f,.9f,.8f);
            var prefab=Save(root,"Camp Table");var settings=Resources.Load<BuildingSettings>("BuildingSettings");
            var entries=settings.visuals.Where(v=>v.id!=BuildCatalog.Table).ToList();
            entries.Add(new BuildingSettings.VisualEntry{id=BuildCatalog.Table,prefab=prefab});settings.visuals=entries.ToArray();EditorUtility.SetDirty(settings);
        }
        static Material TerrainMaterial()
        {
            var path=Root+"Terrain.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(m,path);}
            return m;
        }
        internal static TerrainLayer Layer(string name,Color color)
        {
            string texturePath=Root+name+".asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(texture==null){texture=new Texture2D(2,2,TextureFormat.RGB24,false);AssetDatabase.CreateAsset(texture,texturePath);}
            color.a=0;texture.SetPixels(Enumerable.Repeat(color,4).ToArray());texture.Apply();EditorUtility.SetDirty(texture);
            var path=Root+name+".terrainlayer";var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if(layer==null){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,path);}
            layer.diffuseTexture=texture;layer.normalMapTexture=null;layer.tileSize=Vector2.one*8;layer.smoothness=0;EditorUtility.SetDirty(layer);return layer;
        }
        internal static GameObject Save(GameObject go,string name)
        {var prefab=PrefabUtility.SaveAsPrefabAsset(go,ArtPath(name,".prefab"));Object.DestroyImmediate(go);return prefab;}
        internal static string ArtPath(string name,string extension)
        {
            bool character=name=="Wanderer"||name=="Wanderer Female"||name=="Skeleton"||
                name=="Storybook Knight"||name.StartsWith("Wilderness Skeleton ",StringComparison.Ordinal);
            return (character?CharacterRoot:WorldRoot)+name+extension;
        }
        internal static GameObject Wrap(string name,string source,float height,bool tree,bool collision=true)
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+source);if(original==null)throw new InvalidOperationException("Restore Synty Starter: "+source);
            var root=new GameObject(name);var model=Instance(original,root.transform);
            foreach(var c in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            var rs=model.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            float scale=height/Mathf.Max(.01f,bounds.size.y);model.transform.localScale*=scale;model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            if(collision) { if(tree){var c=root.AddComponent<CapsuleCollider>();c.height=height;c.center=Vector3.up*height/2;c.radius=.35f;}else{var c=root.AddComponent<SphereCollider>();c.center=Vector3.up*.4f;c.radius=.7f;} }
            return Save(root,name);
        }
        static void ReplaceResource(Component owner,GameObject prefab,string collider)
        {
            var so=new SerializedObject(owner);var old=so.FindProperty("visualRoot").objectReferenceValue as GameObject;
            if(old!=null&&old!=owner.gameObject)Object.DestroyImmediate(old);
            var model=Instance(prefab,owner.transform);so.FindProperty("visualRoot").objectReferenceValue=model;
            so.FindProperty(collider).objectReferenceValue=model.GetComponent<Collider>();
            var tint=so.FindProperty("idleTint");if(tint!=null)tint.colorValue=Color.white;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Character(string name,string source)
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+"Characters/"+source+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(original);go.name=name;
            var animator=go.GetComponent<Animator>();if(animator==null||animator.avatar==null||!animator.avatar.isHuman)throw new InvalidOperationException("Humanoid avatar required: "+source);
            Transform Bone(string n)=>go.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
            var visual=go.AddComponent<CharacterVisual>();Set(visual,"animator",animator);
            Set(visual,"bodyRenderer",go.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.gameObject.activeSelf));
            Set(visual,"rightHand",Bone("Hand_R"));Set(visual,"leftHand",Bone("Hand_L"));Set(visual,"guardUpperArm",Bone("Shoulder_L"));Set(visual,"guardLowerArm",Bone("Elbow_L"));
            var lantern=new GameObject("Lantern Anchor").transform;lantern.SetParent(Bone("Hips"),false);lantern.localPosition=new Vector3(.22f,0,0);Set(visual,"lanternAnchor",lantern);
            var motion=go.AddComponent<PrototypeHumanoidMotion>();
            var items=new System.Collections.Generic.List<PrototypeHumanoidMotion.HeldVisual>();
            foreach(string id in new[]{"sword","axe","pickaxe","combat-axe","staff","crossbow"})
            {
                var held=new GameObject(id);held.transform.SetParent(Bone("Hand_R"),false);
                if(id=="axe"||id=="combat-axe"||id=="pickaxe")
                {
                    string asset=id=="pickaxe"?"SM_Gen_Wep_Pickaxe_01":"SM_Gen_Wep_Axe_01";
                    var model=Instance(AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+"Weapons/"+asset+".prefab"),held.transform);
                    model.transform.localRotation=Quaternion.Euler(0,0,90);
                }
                else
                {
                    Cube(held.transform,"Grip",new Vector3(0,.1f,0),new Vector3(.045f,id=="staff"?1.3f:.25f,.045f),wood);
                    if(id=="sword")Cube(held.transform,"Blade",Vector3.up*.48f,new Vector3(.09f,.65f,.025f),steel);
                    if(id=="crossbow")Cube(held.transform,"Bow",Vector3.up*.28f,new Vector3(.65f,.04f,.08f),wood);
                }
                items.Add(new PrototypeHumanoidMotion.HeldVisual{id=id,model=held});held.SetActive(false);
            }
            motion.equipment=items.ToArray();
            var shield=Cube(Bone("Hand_L"),"Shield",Vector3.zero,new Vector3(.06f,.5f,.4f),wood);Set(visual,"shield",shield);shield.SetActive(false);
            return Save(go,name);
        }
        static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size,Material mat)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
    }
}
