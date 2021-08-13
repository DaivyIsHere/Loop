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

    //protected override bool CanInteract()
    //{
    //    if (Player.localPlayer == null)
    //        return false;

    //    Player player = Player.localPlayer;

    //    if (player.playerWater.waterAmount >= player.playerWater.MaxWaterAmount)
    //        return false;
    //    else
    //        return true;
    //}

    //[Client]
    //protected override void DoAction()
    //{
    //    //print("do action");
    //    Player.localPlayer.playerWater.CmdDrinkWater(id);
    //}

    protected override bool CanInteract(Player player)
    {
        if (player == null)
            return false;

        if (player.playerWater.waterAmount >= player.playerWater.MaxWaterAmount)
            return false;

        return true;
    }

    [Server]
    protected override void DoAction(Player player)
    {
        player.playerWater.DrinkWater(id);
    }
}
