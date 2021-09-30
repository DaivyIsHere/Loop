// The Entity class is rather simple. It contains a few basic entity properties
// like health, mana and level that all inheriting classes like Players and
// Monsters can use.
//
// Entities also have a _target_ Entity that can't be synchronized with a
// SyncVar. Instead we created a EntityTargetSync component that takes care of
// that for us.
//
// Entities use a deterministic finite state machine to handle IDLE/MOVING/DEAD/
// CASTING etc. states and events. Using a deterministic FSM means that we react
// to every single event that can happen in every state (as opposed to just
// taking care of the ones that we care about right now). This means a bit more
// code, but it also means that we avoid all kinds of weird situations like 'the
// monster doesn't react to a dead target when casting' etc.
// The next state is always set with the return value of the UpdateServer
// function. It can never be set outside of it, to make sure that all events are
// truly handled in the state machine and not outside of it. Otherwise we may be
// tempted to set a state in CmdBeingTrading etc., but would likely forget of
// special things to do depending on the current state.
//
// Entities also need a kinematic Rigidbody so that OnTrigger functions can be
// called. Note that there is currently a Unity bug that slows down the agent
// when having lots of FPS(300+) if the Rigidbody's Interpolate option is
// enabled. So for now it's important to disable Interpolation - which is a good
// idea in general to increase performance.
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Mirror;

// note: no animator required, towers, dummies etc. may not have one
[RequireComponent(typeof(Level))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Combat))]
[RequireComponent(typeof(Rigidbody2D))] // kinematic, only needed for OnTrigger
[RequireComponent(typeof(NetworkProximityGridChecker))]
//[RequireComponent(typeof(NavMeshAgent2D))]
[RequireComponent(typeof(AudioSource))]

public abstract partial class Entity : NetworkBehaviour
{
    [Header("Components")]
    public Level level;
    public Health health;
    public Combat combat;
    public Movement movement;
    //public NavMeshAgent2D agent;
    public NetworkProximityGridChecker proxchecker;
    public Animator animator;
#pragma warning disable CS0109 // member does not hide accessible member
    public new Collider2D collider;
#pragma warning restore CS0109 // member does not hide accessible member
    public AudioSource audioSource;

    // finite state machine
    // -> state only writable by entity class to avoid all kinds of confusion
    [Header("State")]
    [SyncVar, SerializeField] string _state = "IDLE";
    public string state => _state;

    // it's useful to know an entity's last combat time (did/was attacked)
    // e.g. to prevent logging out for x seconds after combat
    [SyncVar] public double lastCombatTime;

    // 'Entity' can't be SyncVar and NetworkIdentity causes errors when null,
    // so we use [SyncVar] GameObject and wrap it for simplicity
    [Header("Target")]
    [SyncVar] NetworkIdentity _target;
    public Entity target
    {
        get { return _target != null ? _target.GetComponent<Entity>() : null; }
        set { _target = value != null ? value.netIdentity : null; }
    }

    /*
    [Header("Health")]
    [SerializeField] public int _healthMax = 100;
    public int healthMax
    {
        get { return _healthMax ;}
    }
    [SyncVar]public int _health;//真實數字
    public int health
    {
        get { return Mathf.Min(_health, healthMax);}
        set { _health = Mathf.Clamp(value, 0, healthMax); }
    }
    public bool healthRecovery = true; // can be disabled in combat etc.
    [SerializeField] public int _healthRecoveryRate = 1;
    public virtual int healthRecoveryRate
    {
        get { return _healthRecoveryRate; }
    }
    public Image healthBar;
    

    [Header("Damage")]
    [SerializeField] protected int _damage;
    public virtual int damage
    {
        get
        {
            return _damage;
        }
    }

    [Header("Defense")]
    [SerializeField] protected int _defense;
    public virtual int defense
    {
        get
        {
            return _defense;
        }
    }
    */

    [Header("Speed")]
    [SerializeField] protected LinearFloat _speed = new LinearFloat { baseValue = 3 };
    public virtual float speed
    {
        get
        {
            return _speed.Get(level.current);
        }
    }

    // 3D text mesh for name above the entity's head
    //[Header("Text Meshes")]
    //public TextMesh stunnedOverlay;

    // every entity can be stunned by setting stunEndTime
    protected double stunTimeEnd;

    // safe zone flag
    // -> needs to be in Entity because both player and pet need it
    [HideInInspector] public bool inSafeZone;

    // look direction for animations and targetless skills
    // (NavMeshAgent itself just moves without actually looking anywhere)
    // => should always be normalized so that the animator doesn't do blending
    public Vector2 lookDirection = Vector2.down; // down by default

    // networkbehaviour ////////////////////////////////////////////////////////
    protected virtual void Awake(){}

    public override void OnStartServer()
    {
        // dead if spawned without health
        if (!health.spawnFull && health.current == 0) _state = "DEAD";
    }

    protected virtual void Start()
    {
        // disable animator on server. this is a huge performance boost and
        // definitely worth one line of code (1000 monsters: 22 fps => 32 fps)
        // (!isClient because we don't want to do it in host mode either)
        // (OnStartServer doesn't know isClient yet, Start is the only option)
        if (!isClient) animator.enabled = false;
    }

    // server function to check which entities need to be updated.
    // monsters, npcs etc. don't have to be updated if no player is around
    // checking observers is enough, because lonely players have at least
    // themselves as observers, so players will always be updated
    // and dead monsters will respawn immediately in the first update call
    // even if we didn't update them in a long time (because of the 'end'
    // times)
    // -> update only if:
    //    - has observers
    //    - if the entity is hidden, otherwise it would never be updated again
    //      because it would never get new observers
    // -> can be overwritten if necessary (e.g. pets might be too far from
    //    observers but should still be updated to run to owner)
    // => only call this on server. client should always update!
    public virtual bool IsWorthUpdating()
    {
        return netIdentity.observers.Count > 0 ||
               IsHidden();
    }

    // entity logic will be implemented with a finite state machine
    // -> we should react to every state and to every event for correctness
    // -> we keep it functional for simplicity
    // note: can still use LateUpdate for Updates that should happen in any case
    void Update()
    {
        // always update all the objects that the client sees
        if (isClient)
        {
            UpdateClient();
        }

        // on server, only update if worth updating
        // (see IsWorthUpdating comments)
        // -> we also clear the target if it's hidden, so that players don't
        //    keep hidden (respawning) monsters as target, hence don't show them
        //    as target again when they are shown again
        if (isServer && IsWorthUpdating())
        {
            //CleanupBuffs();
            ///OnUpdateServer();///為了 Enemy.cs
            if (target != null && target.IsHidden()) target = null;
            _state = UpdateServer();
        }

        // update look direction on server and client (saves a SyncVar)
        // -> always look at move or target direction (if any), otherwise
        //    use the last one when IDLE
        // -> always orthonormal like (0,1) etc. and never (0, 0.5) so the blend
        //    tree doesn't actually blend between sprite animations
        // -> with default value so that default is played instead of nothing for
        //    Vector2.zero cases
        ////lookDirection
        /*
        if (agent.velocity != Vector2.zero)
            lookDirection = Utils.OrthonormalVector2(agent.velocity, lookDirection);
        else if (target != null)
            lookDirection = Utils.OrthonormalVector2(target.transform.position - transform.position, lookDirection);
        */

        // update overlays in any case, except on server-only mode
        // (also update for character selection previews etc. then)
        if (!isServerOnly) UpdateOverlays();
    }

    // update for server. should return the new state.
    protected abstract string UpdateServer();

    ///當serverUpdate時
    protected virtual void OnUpdateServer() { }

    // update for client.
    protected abstract void UpdateClient();

    // can be overwritten for more overlays
    protected virtual void UpdateOverlays()
    {
        //if (stunnedOverlay != null)
        //    stunnedOverlay.gameObject.SetActive(state == "STUNNED");
    }

    // visibility //////////////////////////////////////////////////////////////
    // hide a entity
    // note: using SetActive won't work because its not synced and it would
    //       cause inactive objects to not receive any info anymore
    // note: this won't be visible on the server as it always sees everything.
    [Server]
    public void Hide()
    {
        proxchecker.forceHidden = true;
    }

    [Server]
    public void Show()
    {
        proxchecker.forceHidden = false;
    }


    // is the entity currently hidden?
    // note: usually the server is the only one who uses forceHidden, the
    //       client usually doesn't know about it and simply doesn't see the
    //       GameObject.
    public bool IsHidden() => proxchecker.forceHidden;

    public float VisRange() => NetworkProximityGridChecker.visRange;

    // revive //////////////////////////////////////////////////////////////////
    [Server]
    public void Revive(float healthPercentage = 1)
    {
        health.current = Mathf.RoundToInt(health.max * healthPercentage);
    }


    // aggro ///////////////////////////////////////////////////////////////////
    // this function is called by the AggroArea (if any) on clients and server
    public virtual void OnAggro(Entity entity) { }

    // attack //////////////////////////////////////////////////////////////////
    // we need a function to check if an entity can attack another.
    // => overwrite to add more cases like 'monsters can only attack players'
    //    or 'player can attack pets but not own pet' etc.
    // => raycast NavMesh to prevent attacks through walls, while allowing
    //    attacks through steep hills etc. (unlike Physics.Raycast). this is
    //    very important to prevent exploits where someone might try to attack a
    //    boss monster through a dungeon wall, etc.
    public virtual bool CanAttack(Entity entity)
    {
        return health.current > 0 &&
               entity.health.current > 0 &&
               entity != this &&
               !inSafeZone && !entity.inSafeZone &&
               !NavMesh2D.Raycast(transform.position, entity.transform.position, out NavMeshHit2D hit, NavMesh2D.AllAreas); ;
    }

    // death ///////////////////////////////////////////////////////////////////
    // universal OnDeath function that takes care of all the Entity stuff.
    // should be called by inheriting classes' finite state machine on death.
    [Server]
    public virtual void OnDeath()
    {
        // clear target
        target = null;
    }

    // ontrigger ///////////////////////////////////////////////////////////////
    // protected so that inheriting classes can use OnTrigger too, while also
    // calling those here via base.OnTriggerEnter/Exit
    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        // check if trigger first to avoid GetComponent tests for environment
        if (col.isTrigger && col.GetComponent<SafeZone>())
            inSafeZone = true;
    }

    protected virtual void OnTriggerExit2D(Collider2D col)
    {
        // check if trigger first to avoid GetComponent tests for environment
        if (col.isTrigger && col.GetComponent<SafeZone>())
            inSafeZone = false;
    }

}



