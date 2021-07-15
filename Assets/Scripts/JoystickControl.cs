using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0162

///this script holds joysticks references for player to use
public class JoystickControl : MonoBehaviour
{
    public static JoystickControl singleton;

    public DynamicJoystick MoveJoystick;
    public DynamicJoystick ShootJoystick;

    void Awake()
    {
        if (singleton == null)
            singleton = this;
    }

    void Update()
    {
#if UNITY_STANDALONE
        MoveJoystick.gameObject.SetActive(false);
        ShootJoystick.gameObject.SetActive(false);
        return;
#endif

        if (Player.localPlayer)
        {
            MoveJoystick.gameObject.SetActive(true);
            ShootJoystick.gameObject.SetActive(true);
        }
        else
        {
            MoveJoystick.gameObject.SetActive(false);
            ShootJoystick.gameObject.SetActive(false);
        }
    }
}
