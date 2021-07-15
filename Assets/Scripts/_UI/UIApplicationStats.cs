using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UIApplicationStats : MonoBehaviour
{
    public float updateStatsRate = 0.25f;
    public int frameRate;
    public double ServerPings;

    void Start()
    {
        InvokeRepeating(nameof(UpdateStats), 0, updateStatsRate);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(0,0,0,0.5f);
        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperLeft;
        GUI.Label(new Rect( Screen.width - 350, 20, 150, 100), "FPS:"+ frameRate.ToString(), style);
        GUI.Label(new Rect( Screen.width - 200, 20, 150, 100), "Pings:"+ ServerPings.ToString()+"ms", style);
    }

    void UpdateStats()
    {
        frameRate = (int)(1.0f / Time.smoothDeltaTime);
        ServerPings = (int)(NetworkTime.rtt*1000);
    }

}
