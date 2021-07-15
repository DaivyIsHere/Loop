using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroyAfterTime : MonoBehaviour
{
    public GameObject target;
    public float afterTime;

    void Start()
    {
        if(target)
        {
            Destroy(target,afterTime);
        }
    }
}
