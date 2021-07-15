using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(EnemyItemDrop))]
public class Enemy : Entity
{
    [Header("Components")]
    public EnemyItemDrop enemyItemDrop;
    public EnemyShoot enemyShoot;

    [Header("Movement")]
    public Vector3 movementVector = Vector3.zero;
    [Range(0, 1)] public float moveProbability = 0.1f; // chance per second
    private Vector2 destination;
    private float roamDistance = 2f;
    private float followDistance = 6.5f;
    //public float turnSpd = 10f;//轉向速度

    //public Vector3 destination = Vector3.zero;
    //public bool followingDestination = false;
    //[Range(0, 1)] public float moveProbability = 0.1f; // chance per second
    // monsters should follow their targets even if they run out of the movement
    // radius. the follow dist should always be bigger than the biggest archer's
    // attack range, so that archers will always pull aggro, even when attacking
    // from far away.
    [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f; // move as close as 0.8 * attackRange to a target

    [Header("Overlay")]
    public GameObject overlayPosition;
    public Vector3 overlayOffset = new Vector3(0, 1, 0);
    public GameObject amountOverlay;
    public Color damageTextColor = new Color32(205, 60, 53, 255);

    [Header("Loot")]
    //紀錄每個玩家所造成的傷害，用以檢測玩家是否達到一定傷害才能獲得掉落寶物
    public Dictionary<string, double> playerDamageDone = new Dictionary<string, double>();

    [Header("Respawn")]
    //public float deathTime = 30f; // enough for animation & looting
    //double deathTimeEnd; // double for long term precision
    //public bool respawn = true;
    //public float respawnTime = 10f;
    //double respawnTimeEnd; // double for long term precision

    // save the start position for random movement distance and respawning
    Vector2 startPosition;

    protected override void Start()
    {
        base.Start();
        // remember start position in case we need to respawn later
        startPosition = transform.position;
        destination = RandomRoamPosition();
    }

    protected override void UpdateOverlays()
    {
        //overlayPosition.transform.position = transform.position + new Vector3(0, 0.75f, 0);
        //overlayPosition.transform.eulerAngles = Vector3.zero;
    }

    void LateUpdate()
    {
        // pass parameters to animation state machine
        // => passing the states directly is the most reliable way to avoid all
        //    kinds of glitches like movement sliding, attack twitching, etc.
        // => make sure to import all looping animations like idle/run/attack
        //    with 'loop time' enabled, otherwise the client might only play it
        //    once
        // => only play moving animation while the agent is actually moving. the
        //    MOVING state might be delayed to due latency or we might be in
        //    MOVING while a path is still pending, etc.
        // => skill names are assumed to be boolean parameters in animator
        //    so we don't need to worry about an animation number etc.
        if (isClient) // no need for animations on the server
        {
            health.healthBar.transform.parent.gameObject.SetActive(true);//server端關閉血量顯示
            health.healthBar.fillAmount = health.Percent();
            ///Animation
            /*
            animator.SetBool("MOVING", state == "MOVING" && movement != Vector3.zero);
            animator.SetBool("CASTING", state == "CASTING");
            //foreach (Skill skill in skills)
                //animator.SetBool(skill.name, skill.CastTimeRemaining() > 0);
            animator.SetBool("STUNNED", state == "STUNNED");
            animator.SetBool("DEAD", state == "DEAD");
            animator.SetFloat("LookX", lookDirection.x);
            animator.SetFloat("LookY", lookDirection.y);
            */
        }
    }

    // OnDrawGizmos only happens while the Script is not collapsed
    void OnDrawGizmos()
    {
        // draw the movement area (around 'start' if game running,
        // or around current position if still editing)
        //Vector2 startHelp = Application.isPlaying ? startPosition : (Vector2)transform.position;
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(startHelp, moveDistance);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawWireSphere(transform.position, projectilePrefab.GetComponent<Projectile>().range);

        // draw the follow dist
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, followDistance);
    }

    ///Behaviours For State
    void Move()
    {
        Vector3 direction = destination - (Vector2)this.transform.position;
        movementVector = Vector3.Normalize(direction);
        GetComponent<Rigidbody2D>().velocity = movementVector * speed;

        float angle = Mathf.Atan2(movementVector.y, movementVector.x) * Mathf.Rad2Deg;
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
        //transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * turnSpd);
    }
    void Attack()
    {
        enemyShoot.Shoot();
    }

    Vector2 RandomRoamPosition()
    {
        Vector2 circle2D = Random.insideUnitCircle * roamDistance;
        return (Vector2)transform.position + circle2D;
    }


    /// finite state machine events /////////////////////////////////////////////
    bool EventDied()
    {
        return health.current == 0;
    }

    bool EventTargetDisappeared()
    {
        return target == null;
    }

    bool EventTargetDied()
    {
        return target != null && target.health.current == 0;
    }

    bool EventTargetTooFarToFollow()
    {
        return target != null &&
               Vector2.Distance(transform.position, target.collider.ClosestPointOnBounds(transform.position)) > followDistance;
    }

    bool EventTargetTooFarToAttack()
    {
        return target != null &&
            Vector2.Distance(transform.position, target.collider.ClosestPointOnBounds(transform.position)) > enemyShoot.projectileAttribute.moveRange * 0.8f;
        //走到0.8倍的攻擊範圍內，確保子彈能命中
    }

    /*
    bool EventReachRoamPosition()
    {
        return Vector2.Distance(transform.position, destination) < 0.1f;
    }*/

    bool EventAggro()
    {
        return target != null && target.health.current > 0;
    }

    bool EventMoveEnd()
    {
        return state == "MOVING" && !movement.IsMoving();
    }

    bool EventMoveRandomly()
    {
        return Random.value <= moveProbability * Time.deltaTime;
    }

    bool EventStunned()
    {
        return NetworkTime.time <= stunTimeEnd;
    }

    // finite state machine - server ///////////////////////////////////////////
    [Server]///IDLE state after respawn for a while.
    string UpdateServer_IDLE()
    {
        if (EventDied())
        {
            // we died.
            return "DEAD";
        }
        if (EventTargetDied())
        {
            // we had a target before, but it died now. clear it.
            target = null;
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and go back to start. don't stay here.
            target = null;
            movement.Navigate(startPosition, 0);
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            // we had a target before, but it's out of attack range now.
            // follow it. (use collider point(s) to also work with big entities)
            movement.Navigate(target.collider.ClosestPointOnBounds(transform.position),
                              enemyShoot.projectileAttribute.moveRange * attackToMoveRangeRatio);
            return "MOVING";
        }
        if (EventAggro())
        {
            return "ATTACK";
        }
        if (EventMoveRandomly())
        {
            // walk to a random position in movement radius (from 'start')
            // note: circle y is 0 because we add it to start.y
            Vector2 circle2D = Random.insideUnitCircle * roamDistance;
            movement.Navigate(startPosition + circle2D, 0);
            return "MOVING";
        }
        if (EventMoveEnd()) { } // don't care
        if (EventTargetDisappeared()) { } // don't care

        return "IDLE"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_MOVING()
    {
        //Move();
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventDied())
        {
            // we died.
            return "DEAD";
        }
        if (EventMoveEnd())
        {
            // we reached our destination.
            return "IDLE";
        }
        if (EventTargetDied())
        {
            target = null;
            movement.Reset();
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and start roaming
            target = null;
            movement.Navigate(startPosition, 0);
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())//has target but too far
        {
            // we had a target before, but it's out of attack range now.
            // chase it. ///(use collider point(s) to also work with big entities)
            movement.Navigate(target.collider.ClosestPointOnBounds(transform.position),
                              enemyShoot.projectileAttribute.moveRange * attackToMoveRangeRatio);
            return "MOVING";
        }
        if (EventAggro())
        {
            return "ATTACK";
        }
        if (EventTargetDisappeared()) { }
        return "MOVING";
    }

    [Server]
    string UpdateServer_DEAD()
    {
        OnDeath();

        if (EventDied()) { }
        if (EventTargetDisappeared()) { }
        if (EventTargetDied()) { }
        if (EventTargetTooFarToFollow()) { }
        if (EventTargetTooFarToAttack()) { }
        if (EventAggro()) { }
        return "DEAD";
    }

    [Server]
    string UpdateServer_ATTACK()
    {
        if (EventDied())
        {
            // we died.
            return "DEAD";
        }
        if (EventTargetDisappeared())
        {
            movement.Reset();
            return "IDLE";
        }
        if (EventTargetDied())
        {
            target = null;
            movement.Reset();
            return "IDLE";
        }
        if (EventTargetTooFarToAttack())//has target but too far
        {
            // we had a target before, but it's out of attack range now.
            // chase it. ///(use collider point(s) to also work with big entities)
            movement.Navigate(target.collider.ClosestPointOnBounds(transform.position),
                              enemyShoot.projectileAttribute.moveRange * attackToMoveRangeRatio);
            return "MOVING";
        }
        if (EventAggro())
        {
            movement.Reset();
            Attack();
            return "ATTACK";
        }
        if (EventTargetTooFarToFollow()) { }
        return "ATTACK";
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE") return UpdateServer_IDLE();
        if (state == "MOVING") return UpdateServer_MOVING();
        if (state == "DEAD") return UpdateServer_DEAD();
        if (state == "ATTACK") return UpdateServer_ATTACK();
        Debug.LogError("invalid state:" + state);
        return "IDLE";
    }

    // finite state machine - client ///////////////////////////////////////////
    [Client]
    protected override void UpdateClient() { }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        if (other.tag == "Projectile")
        {
            if (!other.GetComponent<Projectile>())
                return;
            if (!other.GetComponent<Projectile>().IsFromPlayer)
                return;

            Destroy(other.gameObject);
            ///SHOW DAMAGE POPUP
            int damage = other.GetComponent<Projectile>().damage;

            ///Popup
            combat.ShowDamagePopup(damage);
            /*
            string text;
            if (damage > 0)
                text = "-" + damage;
            else
                text = "";
            GameObject overlay = Instantiate(amountOverlay, transform.position + overlayOffset, Quaternion.identity, overlayPosition.transform);
            overlay.GetComponentInChildren<TextMesh>().color = damageTextColor;
            overlay.GetComponentInChildren<TextMesh>().text = text;
            */

            if (isServer)//only decrease hp if server
            {
                health.current -= damage;//Will be clamped By Health script
                SetPlayerDamage(other.GetComponent<Projectile>().owner, damage);
            }
        }
    }

    [Server]
    public void SetPlayerDamage(string playerName, double damage)
    {
        if (playerDamageDone.TryGetValue(playerName, out double dealtdamage))
        {
            playerDamageDone[playerName] = dealtdamage + damage;
        }
        else
        {
            playerDamageDone[playerName] = damage;
        }
    }

    // aggro ///////////////////////////////////////////////////////////////////
    // this function is called by people who attack us and by AggroArea
    [ServerCallback]
    public override void OnAggro(Entity entity)
    {
        // are we alive, and is the entity alive and of correct type?
        if (entity != null && CanAttack(entity))
        {
            // no target yet(==self), or closer than current target?
            // => has to be at least 20% closer to be worth it, otherwise we
            //    may end up nervously switching between two targets
            // => we do NOT use Utils.ClosestDistance, because then we often
            //    also end up nervously switching between two animated targets,
            //    since their collides moves with the animation.
            //    => we don't even need closestdistance here because they are in
            //       the aggro area anyway. transform.position is perfectly fine
            if (target == null)
            {
                target = entity;
            }
            else if (entity != target) // no need to check dist for same target
            {
                float oldDistance = Vector2.Distance(transform.position, target.transform.position);
                float newDistance = Vector2.Distance(transform.position, entity.transform.position);
                if (newDistance < oldDistance * 0.8) target = entity;
            }

        }
    }

    // death ///////////////////////////////////////////////////////////////////
    protected override void OnDeath()
    {
        // take care of entity stuff
        base.OnDeath();

        GetComponent<Rigidbody2D>().velocity = Vector2.zero;

        if (enemyItemDrop.soulBoundDrop)//如果是soulbound則處理每個玩家的掉落
        {
            foreach (var pdd in playerDamageDone)
            {
                if (pdd.Value > enemyItemDrop.dropThreshold * health.max)
                {
                    //是否會soulbound由EnemyItemDrop的bool soulBoundDrop控制，此處都回傳最後攻擊玩家(或是造成傷害最高)
                    enemyItemDrop.DropLootBag(pdd.Key);
                }
            }
        }
        else//如果不是soulbound則固定掉落一件
        {
            enemyItemDrop.DropLootBag("");
        }

        NetworkServer.Destroy(this.gameObject);

        // set death and respawn end times. we set both of them now to make sure
        // that everything works fine even if a monster isn't updated for a
        // while. so as soon as it's updated again, the death/respawn will
        // happen immediately if current time > end time.
        //deathTimeEnd = NetworkTime.time + deathTime;
        //respawnTimeEnd = deathTimeEnd + respawnTime; // after death time ended
    }

    // skills //////////////////////////////////////////////////////////////////
    // we use 'is' instead of 'GetType' so that it works for inherited types too
    public override bool CanAttack(Entity entity)
    {
        return base.CanAttack(entity) && entity is Player; /*&&
               (entity is Player; ||
                entity is Pet ||
                entity is Mount);*/

    }
}
