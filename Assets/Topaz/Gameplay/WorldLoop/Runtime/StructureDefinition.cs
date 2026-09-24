using UnityEngine;

namespace Topaz.LoopStudy
{
    [CreateAssetMenu(menuName = "Topaz/Loop/Structure")]
    public sealed class StructureDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "structure.storage_chest";
        [SerializeField, Min(1)] int slotCapacity = 12;

        public string StableId => stableId;
        public int SlotCapacity => slotCapacity;
    }
}
