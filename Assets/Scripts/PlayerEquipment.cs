﻿using System;
using UnityEngine;
using Mirror;

[Serializable]
public partial struct EquipmentInfo
{
    public string requiredCategory;
    //public SubAnimation location;
    public ScriptableItemAndAmount defaultItem;
}

[RequireComponent(typeof(PlayerInventory))]
public class PlayerEquipment : Equipment
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;

    [Header("Equipment Info")]
    public EquipmentInfo[] slotInfo = {
        new EquipmentInfo{requiredCategory="Weapon", defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Ability", defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Armor", defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Ring", defaultItem=new ScriptableItemAndAmount()},
        //new EquipmentInfo{requiredCategory="Head", defaultItem=new ScriptableItemAndAmount()},
        //new EquipmentInfo{requiredCategory="Legs", defaultItem=new ScriptableItemAndAmount()},
        //new EquipmentInfo{requiredCategory="Shield", defaultItem=new ScriptableItemAndAmount()},
        //new EquipmentInfo{requiredCategory="Shoulders", defaultItem=new ScriptableItemAndAmount()},
        //new EquipmentInfo{requiredCategory="Hands", defaultItem=new ScriptableItemAndAmount()},
        //new EquipmentInfo{requiredCategory="Feet", defaultItem=new ScriptableItemAndAmount()}
    };

    private void Update() {
        //print(this.name +" "+ GetWeapon().name);
    }

    public override void OnStartClient()
    {
        // setup synclist callbacks on client. no need to update and show and
        // animate equipment on server
        slots.Callback += OnEquipmentChanged;

        // refresh all locations once (on synclist changed won't be called
        // for initial lists)
        // -> needs to happen before ProximityChecker's initial SetVis call,
        //    otherwise we get a hidden character with visible equipment
        //    (hence OnStartClient and not Start)
        for (int i = 0; i < slots.Count; ++i)
            RefreshLocation(i);
    }

    void OnEquipmentChanged(SyncList<ItemSlot>.Operation op, int index, ItemSlot oldSlot, ItemSlot newSlot)
    {
        // update the equipment
        RefreshLocation(index);
    }

    public void RefreshLocation(int index)
    {
        ///目前不需要animation所以註解掉
        //ItemSlot slot = slots[index];
        //EquipmentInfo info = slotInfo[index];

        // valid cateogry and valid location? otherwise don't bother
        //if (info.requiredCategory != "" && info.location != null)
            //info.location.spritesToAnimate = slot.amount > 0 ? ((EquipmentItem)slot.item.data).sprites : null;
    }

    // swap inventory & equipment slots to equip/unequip. used in multiple places
    [Server]
    public void SwapInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and in the equipment
        if (inventory.InventoryOperationsAllowed() &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            // item slot has to be empty (unequip) or equipabable
            ItemSlot slot = inventory.slots[inventoryIndex];
            if (slot.amount == 0 ||
                slot.item.data is EquipmentItem itemData &&
                itemData.CanEquip(player, inventoryIndex, equipmentIndex))
            {
                // swap them
                ItemSlot temp = slots[equipmentIndex];
                slots[equipmentIndex] = slot;
                inventory.slots[inventoryIndex] = temp;
            }
        }
    }

    [Command]
    public void CmdSwapInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        SwapInventoryEquip(inventoryIndex, equipmentIndex);
    }


    // drag & drop /////////////////////////////////////////////////////////////
    void OnDragAndDrop_InventorySlot_EquipmentSlot(int[] slotIndices)
    {
        CmdSwapInventoryEquip(slotIndices[0], slotIndices[1]);
    }

    void OnDragAndDrop_EquipmentSlot_InventorySlot(int[] slotIndices)
    {
        CmdSwapInventoryEquip(slotIndices[1], slotIndices[0]); // reversed
    }

    //helper function to check if any weapon equiped
    public bool HasWeaponEquiped()
    {
        return slots[0].amount > 0;
    }

    public WeaponItem GetWeapon()
    {
        return ((WeaponItem)slots[0].item.data);
    }

    // validation
    void OnValidate()
    {
        // it's easy to set a default item and forget to set amount from 0 to 1
        // -> let's do this automatically.
        for (int i = 0; i < slotInfo.Length; ++i)
            if (slotInfo[i].defaultItem.item != null && slotInfo[i].defaultItem.amount == 0)
                slotInfo[i].defaultItem.amount = 1;
    }
}

