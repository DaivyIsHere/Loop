using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class PlayerLooting : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;

    public LootBag currentloot;
    public float LootRange = 1f;
    
    void Update() 
    {
        if(CanLoot() && base.hasAuthority)
        {
            UILoot.singleton.Show();
        }
    }

    // loot ////////////////////////////////////////////////////////////////////
    void OnTriggerEnter2D(Collider2D other)
    {
        LootBag loot = other.GetComponent<LootBag>();
        if(!loot)
            return;
        if(loot.soulBoundPlayer != player.name && loot.IsSoulBound())
            return;

        if(player.isLocalPlayer || base.isServer)
        {
            currentloot = loot;
        }
    }

    ///有點耗效能，未來觀望是否不要每幀都檢測
    void OnTriggerStay2D(Collider2D other) 
    {
        LootBag loot = other.GetComponent<LootBag>();
        if(!loot)
            return;
        if(loot.soulBoundPlayer != player.name && loot.IsSoulBound())
            return;

        if(player.isLocalPlayer || base.isServer)
        {
            currentloot = loot;
        }
    }

    void OnTriggerExit2D(Collider2D other) 
    {
        LootBag loot = other.GetComponent<LootBag>();
        if(!loot)
            return;
        if(loot.soulBoundPlayer != player.name && loot.IsSoulBound())
            return;
        
        if(player.isLocalPlayer || base.isServer)
        {
            if(currentloot == loot)
                currentloot = null;
        }
    }

    public bool CanLoot()
    {
        return (currentloot != null) && 
        Utils.ClosestDistance(
            player.GetComponent<Collider2D>(), 
            currentloot.GetComponent<Collider2D>()) <= LootRange &&
        (currentloot.soulBoundPlayer == player.name || !currentloot.IsSoulBound());
    }

    [Command]
    public void CmdTakeItem(int index)
    {
        // validate: dead monster and close enough and valid loot index?
        // use collider point(s) to also work with big entities
        ///未來在這檢查Player State Machine
        /*
        if ((player.state == "IDLE" || player.state == "MOVING" || player.state == "CASTING") &&
            player.target != null &&
            player.target is Monster monster &&
            player.target.health.current == 0 &&
            Utils.ClosestDistance(player, player.target) <= player.interactionRange)
        */
        if(CanLoot())
        {
            if (0 <= index && index < currentloot.slots.Count &&
                currentloot.slots[index].amount > 0)
            {
                ItemSlot slot = currentloot.slots[index];

                // try to add it to the inventory, clear monster slot if it worked
                if (inventory.Add(slot.item, slot.amount))
                {
                    slot.amount = 0;
                    currentloot.slots[index] = slot;
                }
            }
        }
    }
}

