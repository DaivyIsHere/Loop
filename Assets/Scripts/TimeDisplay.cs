using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeDisplay : MonoBehaviour
{
    [Header("TextMesh")]
    public TextMesh timeDisplay;
    public TextMesh dateDisplay;

    void Update()
    {
        dateDisplay.text = DateTime.Now.ToString("MM/dd/yyyy");
        timeDisplay.text = DateTime.Now.ToString("HH:mm:ss");
    }
}
