using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debugger : MonoBehaviour
{
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 60;
        style.normal.textColor = Color.black;
        string content = "";
        if (Player.localPlayer)
        {
            List<string> keyList = new List<string>(Player.onlinePlayers.Keys);
            foreach (var key in keyList)
            {
                content += (key+"\n");
            }
        }
        GUI.Label(new Rect(10, 10, 200, 200), "Current Player \n" + content, style);
    }
}
