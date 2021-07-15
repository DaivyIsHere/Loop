using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///用來自動刪除overlay
public class OverlayAutoDestroy : MonoBehaviour
{
    public GameObject DestroyTarget;//parent or self
    
    public void SelfDestroy()
    {
        Destroy(DestroyTarget);
    }
}
