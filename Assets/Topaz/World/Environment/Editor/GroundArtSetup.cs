using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Places licensed KayKit floor and grass art over the existing collision floors.</summary>
    public static class GroundArtSetup
    {
        const string FloorPath = "Assets/ThirdParty/KayKit/Dungeon/Models/floor_dirt_large.fbx";
        const string GrassAPath = "Assets/ThirdParty/KayKit/Forest/Models/Grass_1_A_Color1.fbx";
        const string GrassBPath = "Assets/ThirdParty/KayKit/Forest/Models/Grass_2_A_Color1.fbx";
        const string HomeGrassAPath = "Assets/ThirdParty/KayKit/Forest/Models/Grass_1_C_Color1.fbx";
        const string HomeGrassBPath = "Assets/ThirdParty/KayKit/Forest/Models/Grass_2_C_Color1.fbx";
        const string DungeonMaterialPath = "Assets/Topaz/Presentation/Art/Materials/Dungeon.mat";
        const string ForestMaterialPath = "Assets/Topaz/Presentation/Art/Materials/Forest.mat";

        static readonly Vector2[] HomeGrass =
        {
            new(-12, -10), new(-10, -7), new(-8, -13), new(-3, -12),
            new(3, -12), new(7, -10), new(12, -12), new(10, -6),
            new(13, -2), new(12, 4), new(10, 10), new(5, 12),
            new(-2, 13), new(-8, 11), new(-12, 6), new(-13, 0),
            new(-9, 2), new(-5, -10), new(2, 10), new(8, 5),
            new(-7, -3), new(7, -2)
        };

        static readonly Vector2[] ClearingGrass =
        {
            new(-14, -19), new(-11, -16), new(-7, -20), new(8, -20),
            new(13, -18), new(-14, -12), new(-9, -10), new(9, -12),
            new(14, -9), new(-13, -4), new(-8, -3), new(8, -2),
            new(13, 0), new(-14, 4), new(-10, 7), new(10, 7),
            new(14, 11), new(-14, 14), new(-10, 19), new(-5, 18),
            new(5, 17), new(11, 19), new(14, 16), new(-7, 13),
            new(7, 12)
        };

        [MenuItem("Topaz/Apply KayKit Ground Art")]
        public static void Apply()
        {
            ApplyHome();
            ApplyExpedition();
        }

        public static void ApplyHome()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity", OpenSceneMode.Single);
            Transform root = GameObject.Find("Feel Study")?.transform;
            if (root == null || root.Find("Navigation Geometry/Ground") == null)
                throw new InvalidOperationException("Home collision ground is missing.");
            Build(root, 32f, 32f, 8, 8, HomeGrass.Concat(AdditionalHomeGrass()).ToArray(), true);
            root.Find("Navigation Geometry/Ground").GetComponent<MeshRenderer>().enabled = false;
            EditorSceneManager.SaveScene(scene);
        }

        public static void ApplyExpedition()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Expedition.unity", OpenSceneMode.Single);
            Transform root = GameObject.Find("Expedition Clearing")?.transform;
            Transform art = root?.Find("KayKit Clearing Art");
            if (art == null || root.Find("Navigation Geometry/Ground") == null)
                throw new InvalidOperationException("Clearing art or collision ground is missing.");
            Build(art, 34f, 44f, 9, 11, ClearingGrass, false);
            root.Find("Navigation Geometry/Ground").GetComponent<MeshRenderer>().enabled = false;
            EditorSceneManager.SaveScene(scene);
        }

        static Vector2[] AdditionalHomeGrass()
        {
            // A repeatable loose ring leaves the whole 5.5 m homestead circle readable.
            var positions = new Vector2[60];
            for (int i = 0; i < positions.Length; i++)
            {
                float angle = i * 2.399963f;
                float radius = Mathf.Sqrt(Mathf.Lerp(6.6f * 6.6f, 14.4f * 14.4f,
                    (i + .5f) / positions.Length));
                positions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return positions;
        }

        static void Build(Transform parent, float width, float depth,
            int columns, int rows, Vector2[] grassPositions, bool home)
        {
            Transform prior = parent.Find("KayKit Ground Art");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
            Transform root = new GameObject("KayKit Ground Art").transform;
            root.SetParent(parent, false);
            Transform floor = new GameObject("Dirt Floor").transform;
            floor.SetParent(root, false);
            Transform grass = new GameObject("Grass Clumps").transform;
            grass.SetParent(root, false);

            GameObject floorAsset = Load<GameObject>(FloorPath);
            GameObject grassA = Load<GameObject>(home ? HomeGrassAPath : GrassAPath);
            GameObject grassB = Load<GameObject>(home ? HomeGrassBPath : GrassBPath);
            Material dungeon = Load<Material>(DungeonMaterialPath);
            Material forest = Load<Material>(ForestMaterialPath);
            float tileWidth = width / columns;
            float tileDepth = depth / rows;
            for (int z = 0; z < rows; z++)
            for (int x = 0; x < columns; x++)
            {
                GameObject tile = Place(floorAsset, floor, dungeon, $"Dirt {x + 1}-{z + 1}");
                tile.transform.localPosition = new Vector3(
                    -width * .5f + tileWidth * (x + .5f), -.105f,
                    -depth * .5f + tileDepth * (z + .5f));
                tile.transform.localRotation = Mathf.Approximately(tileWidth, tileDepth)
                    ? Quaternion.Euler(0f, 90f * ((x + z) % 4), 0f)
                    : Quaternion.identity;
                tile.transform.localScale = new Vector3(tileWidth / 4f, 1f, tileDepth / 4f);
            }

            for (int i = 0; i < grassPositions.Length; i++)
            {
                GameObject clump = Place(i % 3 == 0 ? grassB : grassA,
                    grass, forest, $"Grass {i + 1}");
                clump.transform.localPosition = new Vector3(grassPositions[i].x, .025f,
                    grassPositions[i].y);
                clump.transform.localRotation = Quaternion.Euler(0f, (i * 137) % 360, 0f);
                clump.transform.localScale = Vector3.one * (home
                    ? 1.12f + .08f * (i % 4) : .72f + .06f * (i % 4));
            }
        }

        static GameObject Place(GameObject asset, Transform parent, Material material, string name)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null) throw new InvalidOperationException("Cannot instantiate " + asset.name);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material,
                    Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            return instance;
        }

        static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Missing asset: " + path);
    }
}
