using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

public class Fountain : InteractableArea
{
    public int id;
    public string builder;//player account, but display ign instead
    public DateTime builtTime;

    public int totalusedTimes = 0;
    public int totalDrankAmount = 0;

    protected override void Update()
    {
        if(isClient && base.InRange && (Input.GetKeyDown(KeyCode.E)))
            DoAction();
        base.Update();
    }

    protected override bool CanInteract()
    {
        if(Player.localPlayer == null)
            return false;
        
        Player player = Player.localPlayer;

        if(player.playerWater.waterAmount >= player.playerWater.MaxWaterAmount)
            return false;
        else
            return true;
    }

    [Client]
    protected override void DoAction()
    {
        //print("do action");
        Player.localPlayer.playerWater.CmdDrinkWater(id);
    }
}
