// Inventory & Equip both use slots and some common functions. might as well
// abstract them to save code.
using Mirror;

public abstract class ItemContainer : NetworkBehaviour
{
    // the slots
    public SyncList<ItemSlot> slots = new SyncList<ItemSlot>();

    // helper function to find an item in the slots
    public int GetItemIndexByName(string itemName)
    {
        // (avoid FindIndex to minimize allocations)
        for (int i = 0; i < slots.Count; ++i)
        {
            ItemSlot slot = slots[i];
            if (slot.amount > 0 && slot.item.name == itemName)
                return i;
        }
        return -1;
    }
}
