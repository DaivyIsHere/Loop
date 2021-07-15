using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthWater : MonoBehaviour
{
    public GameObject panel;
    public Slider healthSlider;
    public Text healthStatus;
    public Slider waterSlider;
    public Text waterStatus;


    private float TargetDisplayTime = 0.2f;//顯示到目標數字時間
    private float currentWater;
    private float currentHealth;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)//如果LocalPlayer存在
        {
            if(!panel.activeSelf)//如果未開啟Panel，則開啟並初始化
            {
                currentHealth = player.health.current;
                currentWater = player.playerWater.waterAmount;
                healthStatus.text = currentHealth + " / " + Player.localPlayer.health.max;
                healthSlider.value = Player.localPlayer.health.Percent();
                waterStatus.text = Mathf.RoundToInt(currentWater).ToString();
                waterSlider.value = Player.localPlayer.playerWater.WaterPercent();
                panel.SetActive(true);
            }
            else
            {
                UpdateHealth();
                UpdateWater();
            }
        }
        else
            panel.SetActive(false);
    }

    void UpdateHealth()
    {
        if(healthSlider.value != Player.localPlayer.health.Percent())
        {
            float diff = Player.localPlayer.health.Percent() - healthSlider.value;
            healthSlider.value += diff / (TargetDisplayTime / Time.deltaTime);
        }

        if(currentHealth != Player.localPlayer.health.current)
        {
            float diff = Player.localPlayer.health.current - currentHealth;
            currentHealth += diff / (TargetDisplayTime / Time.deltaTime);
            healthStatus.text = Player.localPlayer.health.current + " / " + Player.localPlayer.health.max;
            //healthStatus.text = Mathf.RoundToInt(currentHealth) + " / " + Player.localPlayer.health.max;
        }

    }

    void UpdateWater()
    {
        if(waterSlider.value != Player.localPlayer.playerWater.WaterPercent())
        {
            float diff = Player.localPlayer.playerWater.WaterPercent() - waterSlider.value;
            waterSlider.value += diff / (TargetDisplayTime / Time.deltaTime);
        }

        if(waterStatus.text.ToInt() != Player.localPlayer.playerWater.waterAmount)
        {
            float diff = Player.localPlayer.playerWater.waterAmount - currentWater;
            currentWater += diff / (TargetDisplayTime / Time.deltaTime);
            waterStatus.text = Mathf.RoundToInt(currentWater).ToString();
        }
    }
}
