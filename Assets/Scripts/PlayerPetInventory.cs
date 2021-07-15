using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerPetInventory : NetworkBehaviour
{
    public SyncList<CreatureSlot> slots = new SyncList<CreatureSlot>();

    // helper function to count the free slots
    public int SlotsFree()
    {
        // count manually. Linq is HEAVY(!) on GC and performance
        int free = 0;
        foreach (CreatureSlot slot in slots)
            if (slot.isEmpty)
                ++free;
        return free;
    }

    public int SlotsOccupied()
    {
        // count manually. Linq is HEAVY(!) on GC and performance
        int occupied = 0;
        foreach (CreatureSlot slot in slots)
            if (!slot.isEmpty)
                ++occupied;
        return occupied;
    }
}
