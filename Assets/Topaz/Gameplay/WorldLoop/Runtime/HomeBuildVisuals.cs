using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>Small owned placeholder kit; each root can later become an authored prefab.</summary>
    public static class HomeBuildVisuals
    {
        public static GameObject Create(string id, Transform parent, Transform player)
        {
            var root = new GameObject(id);
            root.transform.SetParent(parent, false);
            if (id == HomeBuildCatalog.Floor)
                Cube(root.transform, "Stone foundation", new Vector3(0f, .07f, 0f),
                    new Vector3(1.5f, .14f, 1.5f), new Color(.43f, .39f, .35f));
            else if (id == HomeBuildCatalog.Wall)
                Cube(root.transform, "Timber wall", new Vector3(0f, 1f, 0f),
                    new Vector3(1.5f, 2f, .17f), new Color(.47f, .31f, .19f));
            else if (id == HomeBuildCatalog.Doorway)
            {
                Cube(root.transform, "Left post", new Vector3(-.64f, 1f, 0f),
                    new Vector3(.2f, 2f, .2f), new Color(.47f, .31f, .19f));
                Cube(root.transform, "Right post", new Vector3(.64f, 1f, 0f),
                    new Vector3(.2f, 2f, .2f), new Color(.47f, .31f, .19f));
                Cube(root.transform, "Lintel", new Vector3(0f, 1.9f, 0f),
                    new Vector3(1.4f, .2f, .2f), new Color(.47f, .31f, .19f));
                GameObject panel = Cube(root.transform, "Door", new Vector3(-.53f, .9f, 0f),
                    new Vector3(1.06f, 1.8f, .09f), new Color(.34f, .23f, .14f));
                root.AddComponent<HomeDoor>().Bind(panel.transform,
                    panel.GetComponent<Collider>(), player);
            }
            else if (id == HomeBuildCatalog.Roof)
            {
                GameObject tile = Cube(root.transform, "Low roof tile",
                    new Vector3(0f, 2.42f, 0f), new Vector3(1.6f, .14f, 1.6f),
                    new Color(.36f, .23f, .18f));
                tile.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                root.AddComponent<HomeRoofVisibility>().Bind(player);
            }
            else if (id == HomeBuildCatalog.Bed)
                Cube(root.transform, "Bed", new Vector3(0f, .28f, 0f),
                    new Vector3(1f, .5f, 1.8f), new Color(.62f, .45f, .3f));
            else if (id == HomeBuildCatalog.Table)
                Cube(root.transform, "Table", new Vector3(0f, .55f, 0f),
                    new Vector3(1.2f, .15f, .7f), new Color(.48f, .31f, .19f));
            else if (id == HomeBuildCatalog.Lantern)
            {
                Cube(root.transform, "Lantern", new Vector3(0f, .7f, 0f),
                    new Vector3(.28f, .5f, .28f), new Color(.85f, .62f, .34f));
                root.AddComponent<HomeNightLight>();
            }
            return root;
        }

        static GameObject Cube(Transform parent, string label, Vector3 position,
            Vector3 scale, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = label;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            cube.GetComponent<Renderer>().SetPropertyBlock(block);
            return cube;
        }
    }
}
