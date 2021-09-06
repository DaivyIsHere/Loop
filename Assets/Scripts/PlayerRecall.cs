using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerRecall : NetworkBehaviour
{
    [SerializeField] private Player player;

    // Update is called once per frame
    protected virtual void Update()
    {
        if (isClient)
        {
            if (Input.GetKeyDown(KeyCode.R) && !UIUtils.AnyInputActive())
            {
                CmdTeleport(player);
            }
        }

    }

    [Command]
    public void CmdTeleport(Player player)
    {
        //GameObject TeleportDest = GameObject.FindGameObjectWithTag("Teleport");
        Vector3 TeleportDest = new Vector3(0.0f, 4.0f, 0.0f);
        player.movement.Warp(TeleportDest);
    }
}
