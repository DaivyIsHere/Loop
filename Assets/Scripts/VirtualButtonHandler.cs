using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

///this scirpt handle every virtual button input, use VirtualButtonHandler.singleton to check
public class VirtualButtonHandler : MonoBehaviour
{
    public static VirtualButtonHandler singleton;

    public Button InteractBtn;
    public int InteractBtn_ListeningCount = 0;

    void Awake()
    {
        if(singleton == null)
            singleton = this;
    }
        
    void Update() 
    {
        if(!Player.localPlayer)
        {
            InteractBtn.gameObject.SetActive(false);
            return;
        }
        else
        {
            InteractBtn.gameObject.SetActive(true);
        }

        InteractBtn.interactable = InteractBtn_ListeningCount > 0;    
    }

    ///Reset按鈕，未來在轉換場景時，如果玩家站在InteractableArea範圍內可能會有漏掉沒清掉listener，不確定(備用)
    void ResetInteractBtn()
    {
        InteractBtn.onClick.RemoveAllListeners();
        InteractBtn_ListeningCount = 0;
    }

}
