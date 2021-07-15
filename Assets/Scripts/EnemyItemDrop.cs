using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnemyItemDrop : NetworkBehaviour
{
    [Header("Components")]
    public Enemy enemy;
    public GameObject lootBagPrefab;
    public ItemDropChance[] dropChances;
    public float existTime = 60f;//存在多久
    public bool soulBoundDrop = false;//決定是否為soulbound
    //要造成hp多少百分比的傷害才會有掉落(但過門檻還是要看物品掉過機率)，最高門檻為血量的一半，盡量不要太高
    ///未來有可能轉化為定值而不是比例，因為未來會有hp scale，而玩家越多每個人打的%數就越少
    [Range(0,0.5f)] public float dropThreshold = 0f;
    //public int minSoulBoundDrop = 3;//如果玩家都沒有達到門檻，則最少會掉落多少個，依照造成傷害最高的玩家來排序

    [Server]
    public void DropLootBag(string boundPlayer)
    {
        if(!HasDrop())
            return;

        List<ItemSlot> newSlots = new List<ItemSlot>();
        // generate items (note: can't use Linq because of SyncList)
        foreach (ItemDropChance itemChance in dropChances)
            if (Random.value <= itemChance.probability)
                newSlots.Add(new ItemSlot(new Item(itemChance.item), Random.Range(itemChance.minAmount, itemChance.maxAmount + 1)));
        
        if(newSlots.Count > 0)
        {
            GameObject lootbag = Instantiate(lootBagPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(lootbag);

            lootbag.GetComponent<LootBag>().destroyTime = NetworkTime.time + existTime;

            if(soulBoundDrop)//處理soulbound
                lootbag.GetComponent<LootBag>().soulBoundPlayer = boundPlayer;
            else
                lootbag.GetComponent<LootBag>().soulBoundPlayer = "";
                
            foreach (var i in newSlots)
            {
                lootbag.GetComponent<LootBag>().AddLoot(i.item, i.amount);
            }
        }
    }

    public bool HasDrop()//是否會掉落物品
    {
        return dropChances.Length > 0;
    }
}
