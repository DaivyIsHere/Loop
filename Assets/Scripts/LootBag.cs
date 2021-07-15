using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class LootBag : Inventory
{
    public float lootBagRadius = 0.5f;
    public string soulBoundPlayer = "";//name ///所有人都可以Loot的話string以""表示
    public double destroyTime = 60f;//什麼時間會摧毀，以NetworkTime.time + existTime設
    public int defaultSize = 10;

    void Start()
    {
        GetComponent<CircleCollider2D>().radius = lootBagRadius;
    }

    void Update() 
    {
        if(base.isServer)
        {
            if(!HasLoot())
                NetworkServer.Destroy(this.gameObject);

            if(NetworkTime.time > destroyTime)
                NetworkServer.Destroy(this.gameObject);
        }
    }

    //use this method instead of inventory.Add
    public void AddLoot(Item item, int amount)
    {
        InitializeSize();
        Add(item, amount);
    }

    public void InitializeSize()
    {
        for (int i = 0; i < defaultSize; i++)
        {
            slots.Add(new ItemSlot());
        }
    }

    public bool HasLoot()
    {
        return SlotsOccupied() > 0f;
    }

    public bool IsSoulBound()
    {
        return soulBoundPlayer != "";
    }
}
