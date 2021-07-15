// we move as much of the movement code as possible into a separate component,
// so that we can switch it out with character controller movement (etc.) easily
using System;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkNavMeshAgentRubberbanding2D))]
[DisallowMultipleComponent]
public class PlayerNavMeshMovement2D : NavMeshMovement2D
{
    [Header("Components")]
    public Player player;
    public NetworkNavMeshAgentRubberbanding2D rubberbanding;

    [Header("Joystick Controller")]
    private DynamicJoystick MoveJoystick;

    void Start()
    {
        if (isLocalPlayer)
        {
            MoveJoystick = JoystickControl.singleton.MoveJoystick;
        }
    }

    public override void Reset()
    {
        // rubberbanding needs a custom reset, along with the base navmesh reset
        if (isServer)
            rubberbanding.ResetMovement();
        agent.ResetMovement();
    }

    // for 4 years since uMMORPG release we tried to detect warps in
    // NetworkNavMeshAgent/Rubberbanding. it never worked 100% of the time:
    // -> checking if dist(pos, lastpos) > speed worked well for far teleports,
    //    but failed for near teleports with dist < speed meters.
    // -> checking if speed since last update is > speed is the perfect idea,
    //    but it turns out that NavMeshAgent sometimes moves faster than
    //    agent.speed, e.g. when moving up or down a corner/stone. in fact, it
    //    sometimes moves up to 5x faster than speed, which makes warp detection
    //    hard.
    // => the ONLY 100% RELIABLE solution is to have our own Warp function that
    //    force warps the client over the network.
    // => this is extremely important for cases where players get warped behind
    //    a small door or wall. this just has to work.
    public override void Warp(Vector2 destination)
    {
        // rubberbanding needs to know about warp. this is the only 100%
        // reliable way to detect it.
        if (isServer)
            rubberbanding.RpcWarp(destination);
        agent.Warp(destination);
    }

    void Update()
    {
        // only for local player
        if (!isLocalPlayer) 
            return;

        if(IsMovementAllowed())
            MoveWASD();

    }

    // movement ////////////////////////////////////////////////////////////////
    // check if movement is currently allowed
    // -> not in Movement.cs because we would have to add it to each player
    //    movement system. (can't use an abstract PlayerMovement.cs because
    //    PlayerNavMeshMovement needs to inherit from NavMeshMovement already)
    public bool IsMovementAllowed()
    {
        // in a state where movement is allowed?
        // and if local player: not typing in an input?
        // (fix: only check for local player. checking in all cases means that
        //       no player could move if host types anything in an input)
        bool isLocalPlayerTyping = isLocalPlayer && UIUtils.AnyInputActive();
        return player.health.current > 0 && !isLocalPlayerTyping;
    }

    [Client]
    void MoveWASD()
    {
        // don't move if currently typing in an input
        // we check this after checking h and v to save computations
        if (!UIUtils.AnyInputActive())
        {
            // get horizontal and vertical input
            // note: no != 0 check because it's 0 when we stop moving rapidly
            float horizontal = 0f;
            float vertical = 0f;

#if UNITY_STANDALONE
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
#endif
#if UNITY_ANDROID
            horizontal = MoveJoystick.Horizontal;
            vertical = MoveJoystick.Vertical;
#endif
#if UNITY_IOS
            horizontal = MoveJoystick.Horizontal;
            vertical = MoveJoystick.Vertical;
#endif


            if (horizontal != 0 || vertical != 0)
            {
                // create direction, normalize in case of diagonal movement
                Vector2 direction = new Vector2(horizontal, vertical);
                direction = direction.normalized;
                //if (direction.magnitude > 1) direction = direction.normalized;

                // draw direction for debugging
                Debug.DrawLine(transform.position, transform.position + (Vector3)direction, Color.green, 0, false);

                // note: SetSpeed() already sets agent.speed to player.speed
                agent.velocity = direction * agent.speed;
            }
        }
    }

    // validation //////////////////////////////////////////////////////////////
    void OnValidate()
    {
        // make sure that the NetworkNavMeshAgentRubberbanding component is
        // ABOVE this component, so that it gets updated before this one.
        // -> otherwise it overwrites player's WASD velocity for local player
        //    hosts
        // -> there might be away around it, but a warning is good for now
        Component[] components = GetComponents<Component>();
        if (Array.IndexOf(components, GetComponent<NetworkNavMeshAgentRubberbanding2D>()) >
            Array.IndexOf(components, this))
            Debug.LogWarning(name + "'s NetworkNavMeshAgentRubberbanding2D component is below the PlayerNavMeshMovement2D component. Please drag it above the Player component in the Inspector, otherwise there might be WASD movement issues due to the Update order.");
    }
}

