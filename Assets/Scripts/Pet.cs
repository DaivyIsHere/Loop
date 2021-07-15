using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkNavMeshAgent2D))]
public class Pet : NetworkBehaviour
{
    [Header("Components")]
    public Movement movement;

    [Header("Icons")]
    public Sprite portraitIcon; // for pet status UI

    [Header("Movement")]
    public float returnDistance = 5; // return to player if dist > ...
    public float ownerDistance = 2; // keep a distance between owner and pet
    public float teleportDistance = 15;// pet should teleport if the owner gets too far away for whatever reason

    [SyncVar] NetworkIdentity _owner;
    public Player owner
    {
        get { return _owner != null ? _owner.GetComponent<Player>() : null; }
        set { _owner = value != null ? value.netIdentity : null; }
    }

    public Player ownerTest;
    public Creature creature;

    // pet's destination should always be right next to player, not inside him
    // -> we use a helper property so we don't have to recalculate it each time
    // -> we offset the position by exactly 1 x bounds to the left because dogs
    //    are usually trained to walk on the left of the owner. looks natural.
    public Vector2 petDefaultPosition
    {
        get
        {
            Bounds bounds = owner.collider.bounds;
            return owner.transform.position - owner.transform.right * bounds.size.x;
        }
    }

    bool EventOwnerDisappeared()
    {
        return owner == null;
    }

    bool EventNeedReturnToOwner()
    {
        return DistanceToOwner() > returnDistance;
    }

    bool EventNeedTeleportToOwner()
    {
        return DistanceToOwner() > teleportDistance;
    }

    void Update()
    {
        if (isClient)
        {
            UpdateClient();
        }

        if (isServer)
        {
            UpdateServer();
        }
    }

    void UpdateClient()
    {
        if (movement.GetVelocity().x != 0 && Mathf.Abs(movement.GetVelocity().x) > 0.1f)
        {
            if (movement.GetVelocity().x > 0)
                GetComponentInChildren<SpriteRenderer>().flipX = false;
            else
                GetComponentInChildren<SpriteRenderer>().flipX = true;
        }
    }

    void UpdateServer()
    {
        //test
        if (ownerTest != null)
            owner = ownerTest;

        if (EventNeedTeleportToOwner())
        {
            movement.Warp(petDefaultPosition);
        }
        else if (EventNeedReturnToOwner())
        {
            movement.Navigate(owner.transform.position, ownerDistance);
        }
    }

    public float DistanceToOwner()
    {
        return owner != null ? Vector2.Distance(owner.transform.position, transform.position) : 0;
    }

}
