//It only stores an creature's static data.
//check ScriptableItem.cs for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName="Loop Creature/General", order=999)]
public partial class ScriptableCreature : ScriptableObject
{
    [Header("Base Stats")]
    public Sprite image;
    

    // caching /////////////////////////////////////////////////////////////////
    // we can only use Resources.Load in the main thread. we can't use it when
    // declaring static variables. so we have to use it as soon as 'All' is
    // accessed for the first time from the main thread.
    // -> we save the hash so the dynamic item part doesn't have to contain and
    //    sync the whole name over the network
    static Dictionary<int, ScriptableCreature> cache;
    public static Dictionary<int, ScriptableCreature> All
    {
        get
        {
            // not loaded yet?
            if (cache == null)
            {
                // get all ScriptableCreature in resources
                ScriptableCreature[] creatures = Resources.LoadAll<ScriptableCreature>("");

                // check for duplicates, then add to cache
                List<string> duplicates = creatures.ToList().FindDuplicates(creature => creature.name);
                if (duplicates.Count == 0)
                {
                    cache = creatures.ToDictionary(creature => creature.name.GetStableHashCode(), creature => creature);
                }
                else
                {
                    foreach (string duplicate in duplicates)
                        Debug.LogError("Resources folder contains multiple ScriptableCreature with the name " + duplicate);
                }
            }
            return cache;
        }
    }
}
