using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizePositionSetup : MonoBehaviour
{    
    private void Start()
    {
        transform.position += new Vector3(
            Random.Range(-4f, 4f),
            Random.Range(-4f, 4f),
            0f
            );  
    }
  
}
