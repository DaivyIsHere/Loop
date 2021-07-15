using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class InteractableArea : NetworkBehaviour
{
    public Animator toolTipAnimation;
    public bool InRange;
    public bool IsShowing = false;

    protected virtual void Update() 
    {
        ShowOrHideToolTip(); 
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //print(other.gameObject + " got in the area.");
        if (Player.localPlayer != null && other.gameObject == Player.localPlayer.gameObject)
        {
            InRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (Player.localPlayer != null && other.gameObject == Player.localPlayer.gameObject)
        {
            InRange = false;
        }
    }

    public void ShowOrHideToolTip()
    {
        if(InRange && CanInteract() && !IsShowing)
        {
            toolTipAnimation.SetTrigger("show");
            IsShowing = true;
            VirtualButtonHandler.singleton.InteractBtn.onClick.AddListener(DoAction);
            VirtualButtonHandler.singleton.InteractBtn_ListeningCount += 1;
        }
        else if(( !InRange || !CanInteract() ) && IsShowing)
        {
            toolTipAnimation.SetTrigger("hide");
            IsShowing = false;
            VirtualButtonHandler.singleton.InteractBtn.onClick.RemoveListener(DoAction);
            VirtualButtonHandler.singleton.InteractBtn_ListeningCount -= 1;
        }
    }

    protected virtual bool CanInteract()
    {
        return true;
    }

    protected virtual void DoAction()
    {

    }
}
