using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>One reusable chest object whose contents live in the save record.</summary>
    public sealed class StorageChest : MonoBehaviour
    {
        [SerializeField] StructureDefinition definition;

        StructureStateRecord _state;
        InventorySlots _inventory;

        public bool IsPlaced => _state != null;
        public int SlotCapacity => definition != null ? definition.SlotCapacity : 0;
        public InventorySlots Inventory => _inventory;
        public StructureStateRecord State => _state;

        public void Bind(StructureStateRecord state)
        {
            _state = state;
            _inventory = state == null ? null : new InventorySlots(state.slots, SlotCapacity);
            if (state == null)
            {
                gameObject.SetActive(false);
                return;
            }
            transform.position = new Vector3(state.x, 0f, state.z);
            gameObject.SetActive(true);
        }

    }
}
