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
        if (isClient)
        {
            ShowOrHideToolTip();

            if (InRange && Input.GetKeyDown(KeyCode.E))
                CmdTryInteract(Player.localPlayer);
        }
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
        if (InRange && CanInteract(Player.localPlayer) && !IsShowing)
        {
            toolTipAnimation.SetTrigger("show");
            IsShowing = true;
            //VirtualButtonHandler.singleton.InteractBtn.onClick.AddListener(DoAction);
            VirtualButtonHandler.singleton.InteractBtn.onClick.AddListener(TryInteract);
            VirtualButtonHandler.singleton.InteractBtn_ListeningCount += 1;
        }
        else if ((!InRange || !CanInteract(Player.localPlayer)) && IsShowing)
        {
            toolTipAnimation.SetTrigger("hide");
            IsShowing = false;
            //VirtualButtonHandler.singleton.InteractBtn.onClick.RemoveListener(DoAction);
            VirtualButtonHandler.singleton.InteractBtn.onClick.RemoveListener(TryInteract);
            VirtualButtonHandler.singleton.InteractBtn_ListeningCount -= 1;
        }
    }

    //protected virtual bool CanInteract()
    //{
    //    return true;
    //}

    //protected virtual void DoAction()
    //{

    //}

    protected virtual bool CanInteract(Player player)
    {
        return true;
    }

    protected virtual void DoAction(Player player)
    {

    }

    [Command(ignoreAuthority = true)]
    private void CmdTryInteract(Player player)
    {
        //Can set a field for the interactable distance, for now I just hard code it as 1
        if (Utils.ClosestDistance(player.GetComponent<Collider2D>(), GetComponent<Collider2D>()) > 1)
            return;

        if (CanInteract(player))
            DoAction(player);
    }

    private void TryInteract() => CmdTryInteract(Player.localPlayer);
}
