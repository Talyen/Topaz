using UnityEngine;
namespace Topaz.Generation
{
    [CreateAssetMenu(menuName = "Topaz/Woodland Preset")]
    public sealed class WoodlandPreset : ScriptableObject
    {
        public WoodlandSettings settings = new WoodlandSettings();
        public TerrainLayer grass;
        public TerrainLayer path;
        public Material terrainMaterial;
        public GameObject treeVisual;
        public GameObject rockVisual;
        public GameObject treeVariant;
        public GameObject detail;
    }
}
