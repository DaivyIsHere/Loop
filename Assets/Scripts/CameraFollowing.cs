using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowing : MonoBehaviour
{
    public static CameraFollowing instance;
    public GameObject mousePlane;
    public LayerMask mousePlaneLayer;

    void Awake()
    {
        CreateInstance();
    }

    void CreateInstance()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    
    public Vector3 OriginalPos;
    public Vector3 Offset = Vector3.zero;
    public Vector3 CamAngle;
    public GameObject followObject;
    public Vector3 targetPosition;
    private float moveSpeed = 5f;

    void Start()
    {
        OriginalPos = this.transform.position;
        //transform.eulerAngles = new Vector3(-40f,0,0);
    }

    void LateUpdate()
    {
        if(followObject==null)
            return;
        
        targetPosition = new Vector3(followObject.transform.position.x, followObject.transform.position.y, OriginalPos.z);
        targetPosition += Offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.eulerAngles = CamAngle;//Offset -20度 配(0, -3f, 2.5f) -30度 配(0, -4.5f, 3f)
        mousePlane.transform.position = new Vector3(transform.position.x, transform.position.y , 0f);
    }
}
