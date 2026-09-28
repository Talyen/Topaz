using UnityEngine;
namespace Topaz.Generation
{
    [System.Serializable]
    public sealed class GroundCoverPrototype
    {
        public GameObject prefab;
        public Vector2 width = new Vector2(.65f,1.1f);
        public Vector2 height = new Vector2(.25f,.45f);
        public void Validate()
        {
            if(prefab==null || prefab.GetComponent<MeshFilter>()==null || prefab.GetComponent<MeshRenderer>()==null ||
                !float.IsFinite(width.x)||!float.IsFinite(width.y)||!float.IsFinite(height.x)||!float.IsFinite(height.y)||
                width.x<=0||height.x<=0||width.y<width.x||height.y<height.x)
                throw new System.InvalidOperationException("Invalid ground-cover mesh or scale range.");
        }
    }
    [CreateAssetMenu(menuName = "Topaz/Woodland Preset")]
    public sealed class WoodlandPreset : ScriptableObject
    {
        public UnityEngine.Rendering.GraphicsStateCollection[] warmupCollections = System.Array.Empty<UnityEngine.Rendering.GraphicsStateCollection>();
        public WoodlandSettings settings = new WoodlandSettings();
        public TerrainLayer grass;
        public TerrainLayer path;
        public TerrainLayer rockLayer;
        public TerrainLayer forestLayer;
        public Material distantMaterial;
        public Color[] distantGroundColors = {new Color(.20f,.31f,.20f),new Color(.29f,.265f,.22f),new Color(.3f,.335f,.31f),new Color(.17f,.25f,.18f)};
        public GameObject[] trees = System.Array.Empty<GameObject>();
        public GameObject[] backgroundMountains = System.Array.Empty<GameObject>();
        public GameObject[] snowRocks = System.Array.Empty<GameObject>();
        public GameObject[] distantTrees = System.Array.Empty<GameObject>();
        public GameObject[] cliffs = System.Array.Empty<GameObject>();
        public GameObject[] rocks = System.Array.Empty<GameObject>();
        public GameObject[] undergrowth = System.Array.Empty<GameObject>();
        public GameObject bridge;
        public GameObject discoveryCache;
        public GameObject[] discoveries = System.Array.Empty<GameObject>();
        public GameObject[] destinations = System.Array.Empty<GameObject>();
        public GameObject titansGraveDistant;
        public GameObject splitPeakDistant;
        public Topaz.Combat.EnemyCombatant[] enemies = System.Array.Empty<Topaz.Combat.EnemyCombatant>();
        public Topaz.Combat.EnemyCombatant[] goblins = System.Array.Empty<Topaz.Combat.EnemyCombatant>();
        public Topaz.Combat.EnemyCombatant[] raiders = System.Array.Empty<Topaz.Combat.EnemyCombatant>();
        public Topaz.Combat.EnemyCombatant troll;
        public Material terrainMaterial;
        public GameObject treeVisual;
        public GameObject rockVisual;
        public GameObject treeVariant;
        public GameObject detail;
        public GroundCoverPrototype[] groundCover = System.Array.Empty<GroundCoverPrototype>();
        public WoodlandAssetRole[] roles = System.Array.Empty<WoodlandAssetRole>();
        public WoodlandAssetRole Binding(GameObject prefab)
        {
            foreach(var role in roles)if(role.prefab==prefab)return role;
            return null;
        }
        public void ValidateContent()
        {
            if(grass==null || path==null || rockLayer==null || forestLayer==null || discoveryCache==null || discoveries.Length<3)
                throw new System.InvalidOperationException("Outdoor v3 requires four terrain layers, a cache and landmark/camp/resource-site compositions.");
            if(distantGroundColors==null || distantGroundColors.Length!=4)throw new System.InvalidOperationException("Four distant terrain palette averages are required.");
            if(groundCover.Length!=0 && groundCover.Length!=4)throw new System.InvalidOperationException("Alpine ground cover requires low, upright and shade grass plus flowers.");
            if(destinations.Length>0 && destinations.Length!=(int)DestinationKind.SplitPeak+1)
                throw new System.InvalidOperationException("Destination bindings must match the generation catalog.");
            foreach(var entry in groundCover)entry.Validate();
            foreach(var role in roles)role.Validate();
            int missing=0;
            foreach(var array in new[]{trees,rocks,undergrowth})foreach(var prefab in array)if(prefab==null)missing++;
            if(missing>0)Debug.LogWarning($"[Topaz] Skipping {missing} unbound optional woodland decorations.",this);
            foreach(var required in new[]{WoodlandRole.Landmark,WoodlandRole.Camp,WoodlandRole.ResourceSite,WoodlandRole.ShallowWater})
                if(!System.Array.Exists(roles,r=>r.role==required))throw new System.InvalidOperationException("Missing semantic role "+required);
        }
    }
}
