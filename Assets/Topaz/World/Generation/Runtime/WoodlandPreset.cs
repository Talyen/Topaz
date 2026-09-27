using UnityEngine;
namespace Topaz.Generation
{
    [CreateAssetMenu(menuName = "Topaz/Woodland Preset")]
    public sealed class WoodlandPreset : ScriptableObject
    {
        public WoodlandSettings settings = new WoodlandSettings();
        public TerrainLayer grass;
        public TerrainLayer path;
        public TerrainLayer rockLayer;
        public TerrainLayer forestLayer;
        public Material distantMaterial;
        public GameObject[] trees = System.Array.Empty<GameObject>();
        public GameObject[] rocks = System.Array.Empty<GameObject>();
        public GameObject[] undergrowth = System.Array.Empty<GameObject>();
        public GameObject discoveryCache;
        public GameObject[] discoveries = System.Array.Empty<GameObject>();
        public Topaz.Combat.EnemyCombatant[] enemies = System.Array.Empty<Topaz.Combat.EnemyCombatant>();
        public Material terrainMaterial;
        public GameObject treeVisual;
        public GameObject rockVisual;
        public GameObject treeVariant;
        public GameObject detail;
    }
}
