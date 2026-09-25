#if UNITY_EDITOR
using System.IO;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Idempotent first-food authoring over the existing home scene.</summary>
    public static class SurvivalSetup
    {
        const string ResourceDir = "Assets/Topaz/Gameplay/WorldLoop/Resources";
        const string ArtDir = "Assets/Topaz/UI/Art/Survival";
        const string MaterialDir = "Assets/Topaz/Gameplay/WorldLoop/Materials";

        public static void Apply()
        {
            Directory.CreateDirectory(ResourceDir);
            Directory.CreateDirectory(ArtDir);
            Directory.CreateDirectory(MaterialDir);
            Item("RedBerries", SurvivalRules.BerriesId, "Red Berries", 20,
                Icon("RedBerries", new Color32(190, 38, 50, 255)));
            Item("Mushrooms", SurvivalRules.MushroomsId, "Mushrooms", 20,
                Icon("Mushrooms", new Color32(154, 91, 55, 255)));
            Item("MushroomStew", SurvivalRules.StewId, "Mushroom Stew", 10,
                Icon("MushroomStew", new Color32(172, 105, 55, 255)));

            Scene scene = EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity",
                OpenSceneMode.Single);
            GameObject parent = GameObject.Find("Survival Forage");
            if (parent == null) parent = new GameObject("Survival Forage");
            Add(parent.transform, "berries.home.west", false, new Vector3(-7f, 0f, -3f));
            Add(parent.transform, "berries.home.east", false, new Vector3(7f, 0f, -3f));
            Add(parent.transform, "mushrooms.home.west", true, new Vector3(-8f, 0f, 3f));
            Add(parent.transform, "mushrooms.home.east", true, new Vector3(8f, 0f, 3f));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void Item(string assetName, string id, string label, int stack, Sprite icon)
        {
            string path = ResourceDir + "/" + assetName + ".asset";
            ItemDefinition asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            var fields = new SerializedObject(asset);
            fields.FindProperty("stableId").stringValue = id;
            fields.FindProperty("displayName").stringValue = label;
            fields.FindProperty("maxStack").intValue = stack;
            fields.FindProperty("journalIcon").objectReferenceValue = icon;
            fields.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        static Sprite Icon(string name, Color32 color)
        {
            string path = ArtDir + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                var pixels = new Color32[64 * 64];
                for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 24f;
                    float dy = (y - 31.5f) / 24f;
                    float radius = dx * dx + dy * dy;
                    pixels[y * 64 + x] = radius <= 1f ?
                        new Color32((byte)Mathf.Clamp(color.r + (y - 32) / 3, 0, 255),
                            (byte)Mathf.Clamp(color.g + (y - 32) / 3, 0, 255),
                            (byte)Mathf.Clamp(color.b + (y - 32) / 3, 0, 255), 255) :
                        new Color32(0, 0, 0, 0);
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Add(Transform parent, string id, bool mushrooms, Vector3 position)
        {
            if (parent.Find(id) != null) return;
            var root = new GameObject(id);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var visual = new GameObject("Forage Visual");
            visual.transform.SetParent(root.transform, false);
            Material leaf = Material("Forage Leaves", new Color(.21f, .46f, .23f));
            Material red = Material("Red Berries", new Color(.72f, .07f, .1f));
            Material stem = Material("Mushroom Stem", new Color(.8f, .71f, .55f));
            Material cap = Material("Mushroom Cap", new Color(.48f, .23f, .14f));
            if (mushrooms)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 offset = new Vector3((i - 1) * .22f, 0f, i % 2 * .12f);
                    Primitive(visual.transform, PrimitiveType.Cylinder, "Stem", offset +
                        Vector3.up * .16f, new Vector3(.1f, .16f, .1f), stem);
                    Primitive(visual.transform, PrimitiveType.Sphere, "Cap", offset +
                        Vector3.up * .36f, new Vector3(.28f, .12f, .28f), cap);
                }
            }
            else
            {
                Primitive(visual.transform, PrimitiveType.Sphere, "Bush", Vector3.up * .35f,
                    new Vector3(.9f, .65f, .85f), leaf);
                for (int i = 0; i < 5; i++)
                    Primitive(visual.transform, PrimitiveType.Sphere, "Berry",
                        new Vector3((i - 2) * .15f, .52f + (i % 2) * .08f,
                            -.35f + (i % 3) * .08f), Vector3.one * .13f, red);
            }
            var plant = root.AddComponent<ForagePlant>();
            var fields = new SerializedObject(plant);
            fields.FindProperty("stableObjectId").stringValue = id;
            fields.FindProperty("mushrooms").boolValue = mushrooms;
            fields.FindProperty("harvestVisual").objectReferenceValue = visual;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material Material(string name, Color color)
        {
            string path = MaterialDir + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void Primitive(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 size, Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = size;
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
#endif
