using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(CircleCollider2D))]
public class RainArea : NetworkBehaviour
{
    public int id;
    public DateTime RainFinishedTime;
    private float maxRainRadius = 10f;//代表如果與圓心距離__單位以內，雨量都是最大

    public bool IsPosInBound(Vector2 pos)
    {
        float radius = GetComponent<CircleCollider2D>().radius;
        if(Vector2.Distance(pos, (Vector2)this.transform.position) < radius)
            return true;
        else
            return false;
    }

    public float GetRainStage(Vector2 pos)
    {
        float radius = GetComponent<CircleCollider2D>().radius;
        float dis = Vector2.Distance(pos, (Vector2)this.transform.position);
        //print("dis = " +dis);
        if(dis <= maxRainRadius)
            return 1;
        else
            return Mathf.Clamp(1- ((dis - maxRainRadius) / (radius - maxRainRadius)), 0, 1);
    }

    void OnDrawGizmos() 
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, GetComponent<CircleCollider2D>().radius);
        Gizmos.DrawWireSphere(transform.position, maxRainRadius);
    }
}
