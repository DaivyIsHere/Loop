using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

///此腳本與NetworkProximityChecker類似，但未實做距離檢測
///未來必須與NetworkProximityChecker做連接

[RequireComponent(typeof(LootBag))]
public class LootBagVisibility : NetworkVisibility
{
    public LootBag lootBag;
    [Tooltip("How often (in seconds) that this object should update the list of observers that can see it.")]
    public float visUpdateInterval = 1f;

    public override void OnStartServer()
    {
        InvokeRepeating(nameof(RebuildObservers), 0, visUpdateInterval);
    }
    public override void OnStopServer()
    {
        CancelInvoke(nameof(RebuildObservers));
    }

    void RebuildObservers()
    {
        netIdentity.RebuildObservers(false);
    }

    /// <summary>
    /// Callback used by the visibility system to determine if an observer (player) can see this object.
    /// <para>If this function returns true, the network connection will be added as an observer.</para>
    /// </summary>
    /// <param name="conn">Network connection of a player.</param>
    /// <returns>True if the player can see this object.</returns>
    public override bool OnCheckObserver(NetworkConnection conn)
    {
        if (!lootBag.IsSoulBound())
            return true;

        return conn.identity.name == lootBag.soulBoundPlayer;
    }

    /// <summary>
    /// Callback used by the visibility system to (re)construct the set of observers that can see this object.
    /// <para>Implementations of this callback should add network connections of players that can see this object to the observers set.</para>
    /// </summary>
    /// <param name="observers">The new set of observers for this object.</param>
    /// <param name="initialize">True if the set of observers is being built for the first time.</param>
    public override void OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                if (!lootBag.IsSoulBound())
                {
                    observers.Add(conn);//如果不是soulbound每個人都看的到
                }
                else
                {
                    if (conn.identity.name == lootBag.soulBoundPlayer)//檢查名字
                    {
                        observers.Add(conn);
                    }
                }

            }
        }
    }
}
