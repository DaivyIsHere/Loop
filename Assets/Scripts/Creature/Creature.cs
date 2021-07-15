// The Creature struct only contains the dynamic creature properties, so that the static
// properties can be read from the scriptable object.
//
// Creature have to be structs in order to work with SyncLists.
//
// Use .Equals to compare two creatures. Comparing the name is NOT enough for cases
// where dynamic stats differ.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public partial struct Creature
{
    // hashcode used to reference the real ScriptableCreature (can't link to data
    // directly because synclist only supports simple types). and syncing a
    // string's hashcode instead of the string takes WAY less bandwidth.
    public int hash;

    // dynamic stats
    public int ceatureID;


    // constructors
    public Creature(ScriptableCreature data , int ceatureID)
    {
        hash = data.name.GetStableHashCode();
        this.ceatureID = ceatureID;
    }

    // wrappers for easier access
    public ScriptableCreature data
    {
        get
        {
            // show a useful error message if the key can't be found
            // note: ScriptableItem.OnValidate 'is in resource folder' check
            //       causes Unity SendMessage warnings and false positives.
            //       this solution is a lot better.
            if (!ScriptableCreature.All.ContainsKey(hash))
                throw new KeyNotFoundException("There is no ScriptableCreature with hash=" + hash + ". Make sure that all ScriptableCreatures are in the Resources folder so they are loaded properly.");
            return ScriptableCreature.All[hash];
        }
    }

    public string name => data.name;
    public Sprite image => data.image;
}
