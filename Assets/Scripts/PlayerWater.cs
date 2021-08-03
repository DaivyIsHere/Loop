using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerWater : NetworkBehaviour
{
    [Header("Components")] // to be assigned in inspector
    public Player player;

    [Header("Water")]
    //這裡計算裝備加成
    public bool waterLose = true; // can be disabled in area etc.
    public int waterLoseAmount = 5;
    public int waterLoseInterval = 60;//正確為60秒

    [SerializeField] public int _MaxWaterAmount = 100;
    public int MaxWaterAmount
    {
        get { return _MaxWaterAmount; }
    }
    [SyncVar] public int _waterAmount;//真實數字
    public int waterAmount
    {
        get { return Mathf.Min(_waterAmount, MaxWaterAmount); }
        set { _waterAmount = Mathf.Clamp(value, 0, MaxWaterAmount); }
    }
    public float WaterPercent()
    {
        return (waterAmount != 0 && MaxWaterAmount != 0) ? (float)waterAmount / (float)MaxWaterAmount : 0;
    }

    [Command]
    public void CmdDrinkWater(int fountainID)
    {
        int amount = MaxWaterAmount - waterAmount;
        if (amount > 0)
            ConsumeWater(amount, fountainID);
    }

    public void DrinkWater(int fountainID)
    {
        int amount = MaxWaterAmount - waterAmount;
        if (amount > 0)
            ConsumeWater(amount, fountainID);
    }

    [Server]//call Database to save water changes
    public void ConsumeWater(int amount, int fountainID)
    {
        EnvironmentManager.singleton.PlayerDrinkWater(player, amount, fountainID);
        RpcDisplayWaterAmount(amount);
    }

    [Server]//Repeat losing water
    public void LoseWater()
    {
        if (enabled && waterLose)
        {
            EnvironmentManager.singleton.PlayerLoseWater(player);
        }
    }

    [ClientRpc]
    public void RpcDisplayWaterAmount(int amount)
    {
        string text;
        if (amount >= 0)
            text = "+ " + amount + " water";
        else
            text = "- " + Mathf.Abs(amount) + " water";

        GameObject overlay = Instantiate(player.amountOverlay, transform.position + player.overlayOffset, Quaternion.identity, player.overlayPosition.transform);
        overlay.GetComponentInChildren<TextMesh>().color = player.waterTextColor;
        overlay.GetComponentInChildren<TextMesh>().text = text;
    }
}
