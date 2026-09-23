using System;
using System.Collections.Generic;

namespace Topaz.LoopStudy
{
    /// <summary>Fixed slots and item stack rules over Unity-serializable save records.</summary>
    public sealed class InventorySlots
    {
        readonly List<ItemStackRecord> _slots;
        readonly int _capacity;

        public InventorySlots(List<ItemStackRecord> slots, int capacity)
        {
            if (slots == null || capacity < 1 || slots.Count > capacity)
                throw new ArgumentException("Inventory slots are missing or exceed their capacity.");
            _slots = slots;
            _capacity = capacity;
            foreach (ItemStackRecord slot in slots)
                if (slot == null || slot.count < 0 ||
                    (slot.count > 0 && string.IsNullOrEmpty(slot.itemId)))
                    throw new ArgumentException("Inventory contains an invalid slot.");
            while (_slots.Count < _capacity) _slots.Add(new ItemStackRecord());
        }

        public int Capacity => _capacity;
        public IReadOnlyList<ItemStackRecord> Slots => _slots;

        public int Count(string itemId)
        {
            int count = 0;
            foreach (ItemStackRecord slot in _slots)
                if (slot.itemId == itemId) count += slot.count;
            return count;
        }

        public int SpaceFor(ItemDefinition item)
        {
            int space = 0;
            foreach (ItemStackRecord slot in _slots)
            {
                if (slot.count == 0) space += item.MaxStack;
                else if (slot.itemId == item.StableId) space += Math.Max(0, item.MaxStack - slot.count);
            }
            return space;
        }

        public int Add(ItemDefinition item, int offered)
        {
            if (item == null || offered <= 0) return 0;
            int remaining = offered;
            foreach (ItemStackRecord slot in _slots)
            {
                if (remaining == 0) break;
                if (slot.itemId != item.StableId || slot.count == 0) continue;
                int accepted = Math.Min(remaining, Math.Max(0, item.MaxStack - slot.count));
                slot.count += accepted;
                remaining -= accepted;
            }
            foreach (ItemStackRecord slot in _slots)
            {
                if (remaining == 0) break;
                if (slot.count != 0) continue;
                int accepted = Math.Min(remaining, item.MaxStack);
                slot.itemId = item.StableId;
                slot.count = accepted;
                remaining -= accepted;
            }
            return offered - remaining;
        }

        public int Remove(string itemId, int requested)
        {
            if (string.IsNullOrEmpty(itemId) || requested <= 0) return 0;
            int remaining = requested;
            for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStackRecord slot = _slots[i];
                if (slot.itemId != itemId || slot.count == 0) continue;
                int taken = Math.Min(remaining, slot.count);
                slot.count -= taken;
                remaining -= taken;
                if (slot.count == 0) slot.itemId = null;
            }
            return requested - remaining;
        }

        public int TransferAllTo(InventorySlots destination, Func<string, ItemDefinition> resolve)
        {
            int moved = 0;
            foreach (ItemStackRecord slot in _slots)
            {
                if (slot.count == 0) continue;
                ItemDefinition item = resolve(slot.itemId);
                if (item == null) continue; // Preserve unknown IDs for future content or migration.
                int accepted = destination.Add(item, slot.count);
                slot.count -= accepted;
                moved += accepted;
                if (slot.count == 0) slot.itemId = null;
            }
            return moved;
        }
    }
}
