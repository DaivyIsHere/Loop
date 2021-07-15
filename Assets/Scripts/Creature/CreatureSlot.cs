using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public partial struct CreatureSlot
{
    public Creature creature;
    public bool isEmpty;

    // constructors
    public CreatureSlot(Creature creature, bool isEmpty = false)
    {
        this.creature = creature;
        this.isEmpty = isEmpty;
    }
}
