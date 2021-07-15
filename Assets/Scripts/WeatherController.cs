using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//此腳本控制client端，更改玩家所看見的天氣

public class WeatherController : MonoBehaviour
{
    public static WeatherController singleton;

    public ParticleSystem RainEffect;
    private float MaxRainEmitRate = 400f;
    private float minRainEmitRate = 15f;

    void Awake() 
    {
        singleton = this;    
    }

    public void ToggleRaining(float rainStage)
    {
        bool isRaining = rainStage > 0 ? true : false;
        if(isRaining && !RainEffect.isPlaying)
        {
            RainEffect.Play();
        }
        else if(!isRaining && RainEffect.isPlaying)
        {
            RainEffect.Stop();
        }
        float emissionRate = minRainEmitRate + (MaxRainEmitRate - minRainEmitRate) * rainStage;
        
        ParticleSystem.EmissionModule emissionModule = RainEffect.emission;
        emissionModule.rateOverTime = emissionRate;

    }
}
