using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

//this script only run on server
public class EnvironmentManager : NetworkBehaviour
{
    public static EnvironmentManager singleton;//be sure only run on server

    public int groundWater;
    public int groundWater_Pure;//指groundwater內，有含多少Pure，所以數量必不大於groundwater
    public int airWater_Pure;

    public GameObject fountainPref;
    public GameObject RainAreaPref;
    public List<Fountain> fountainList;
    public List<RainArea> RainAreaList;

    [Header("Database")]
    public float saveInterval = 60f; // in seconds

    private float checkRainFinishTimeInterval = 60f;//每幾秒偵測一次是否下完雨，若下完雨則Destroy

    public void SpawnFountain(GameObject fountain)
    {
        NetworkServer.Spawn(fountain);
        fountainList.Add(fountain.GetComponent<Fountain>());
    }

    public void SpawnRainArea(GameObject RainArea)
    {
        NetworkServer.Spawn(RainArea);
        RainAreaList.Add(RainArea.GetComponent<RainArea>());
        checkRainFinishTime();
    }

    public override void OnStartServer()
    {
        if(singleton == null)
            singleton = this;
        //Envir
        Database.singleton.LoadEnvironmentData();
        InvokeRepeating(nameof(SaveEnvironmentData), saveInterval, saveInterval);
        InvokeRepeating(nameof(checkRainFinishTime), 0, checkRainFinishTimeInterval);
    }

    /*
    public override void OnStopServer()
    {
        CancelInvoke(nameof(SaveEnvironmentData));
    }*/

    public Fountain GetFountainByID(int id)
    {
        foreach (var f in fountainList)
        {
            if (f.id == id)
                return f;
        }

        return null;
    }

    public void SaveEnvironmentData()
    {
        Database.singleton.SaveEnvironmentData();
        Debug.Log("EnvironmentData Saved");
    }

    public void UseGroundWater(int amount, int fountainID)
    {
        GetFountainByID(fountainID).totalusedTimes += 1;
        GetFountainByID(fountainID).totalDrankAmount += amount;
        groundWater -= amount;
        //Database.singleton.SaveFountain();
        //Database.singleton.SaveWaterData();
    }

    public void PlayerDrinkWater(Player player, int amount, int fountainID)
    {
        print(player.name + " Drink " + amount + " water");
        player.playerWater.waterAmount += amount;
        //Database.singleton.CharacterSave(player, true);
        UseGroundWater(amount, fountainID);
    }

    public void PlayerLoseWater(Player player)
    {
        if (IsPlayerInRainArea(player))
        {
            player.playerWater.waterAmount = player.playerWater.MaxWaterAmount;
            //Database.singleton.CharacterSave(player, true);
            return;
        }
        int loseAmount = player.playerWater.waterLoseAmount;
        loseAmount = Mathf.Clamp(loseAmount, 0, player.playerWater.waterAmount);
        if(loseAmount == 0)
            return;
        player.playerWater.waterAmount -= loseAmount;
        //Database.singleton.CharacterSave(player, true);
        player.playerWater.RpcDisplayWaterAmount(-loseAmount);
    }

    public bool IsPlayerInRainArea(Player player)
    {
        foreach (RainArea r in RainAreaList)
        {
            if (r.IsPosInBound(player.transform.position))
            {
                return true;
            }
        }
        return false;//如果都不在雨範圍內
    }

    public void checkRainFinishTime()
    {
        for (int i = 0; i < RainAreaList.Count; i++)
        {
            //print(RainAreaList[i].RainFinishedTime);
            //print(DateTime.UtcNow);
            if (RainAreaList[i].RainFinishedTime < DateTime.UtcNow)
            {
                print("Rain Finished!");
                NetworkServer.Destroy(RainAreaList[i].gameObject);
                RainAreaList.RemoveAt(i);
                Database.singleton.SaveRainArea();
            }
        }
    }

}
