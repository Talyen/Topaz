using UnityEngine;

namespace Topaz.LoopStudy
{
    [CreateAssetMenu(menuName = "Topaz/Loop/Structure")]
    public sealed class StructureDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "structure.storage_chest";
        [SerializeField, Min(1)] int woodCapacity = 20;

        public string StableId => stableId;
        public int WoodCapacity => woodCapacity;
    }
}
