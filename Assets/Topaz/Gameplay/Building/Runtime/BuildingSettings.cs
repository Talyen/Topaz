using UnityEngine;
namespace Topaz.Gameplay
{
    [CreateAssetMenu(menuName = "Topaz/Building Settings")]
    public sealed class BuildingSettings : ScriptableObject
    {
        [Min(.1f)] public float grid = .75f;
        [Min(0)] public float maximumHeightDifference = .25f;
        [Range(0,45)] public float maximumSlope = 12f;
        [Min(2)] public float campRadius = 12f;
        [Min(0)] public float enemyClearance = 5f;
        [Min(0)] public int campWood = 5;
        [Min(0)] public int campStone = 5;
        [System.Serializable] public struct VisualEntry { public string id; public GameObject prefab; }
        public VisualEntry[] visuals = System.Array.Empty<VisualEntry>();
        public GameObject VisualFor(string id)
        {
            foreach(var entry in visuals) if(entry.id==id) return entry.prefab;
            return null;
        }
        public Material surfaceMaterial;
        public Material emberMaterial;
        static BuildingSettings current;
        public static BuildingSettings Current
        {
            get
            {
                if (current == null) current = Resources.Load<BuildingSettings>("BuildingSettings");
                if (current == null) current = CreateInstance<BuildingSettings>();
                return current;
            }
        }
    }
}
