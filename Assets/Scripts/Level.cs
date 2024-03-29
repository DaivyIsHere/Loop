﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class Level : NetworkBehaviour
{
    [SyncVar] public int current = 1;
    public int max = 1;

    void OnValidate()
    {
        current = Mathf.Clamp(current, 1, max);
    }
}
