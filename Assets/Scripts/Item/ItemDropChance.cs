// Defines the drop chance of an item for monster loot generation.
using System;
using UnityEngine;

[Serializable]
public class ItemDropChance
{
    public ScriptableItem item;
    [Range(0,1)] public float probability;
    public int maxAmount = 1;
    [HideInInspector]
    public int minAmount = 1;
}
