using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Enemy _enemy;

    [Space]
    [SerializeField] private List<EnemyTransition> _transitions;
    [SerializeField] private List<EnemyAnyTransition> _anyTransitions;

    [HideInInspector]
    public StateMachine StateMachine;

    #region States
    private EnemyIdle _enemyIdle;
    private EnemyWandering _enemyWandering;
    private EnemyChasing _enemyChasing;
    private EnemyAttacking _enemyAttacking;
    private EnemyFleeing _enemyFleeing;
    private EnemyDead _enemyDead;
    #endregion

    #region Conditions
    private Func<bool> _aggro;
    private Func<bool> _aggroChase;
    private Func<bool> _targetTooFarToAttack;
    private Func<bool> _targetTooFarToFollow;
    private Func<bool> _targetTooClose;
    private Func<bool> _targetDisappeared;
    private Func<bool> _targetDied;
    private Func<bool> _lowHealth;
    private Func<bool> _died;
    #endregion

    private void Awake()
    {
        StateMachine = new StateMachine();

        _enemyIdle = new EnemyIdle(_enemy);
        _enemyWandering = new EnemyWandering(_enemy);
        _enemyChasing = new EnemyChasing(_enemy);
        _enemyAttacking = new EnemyAttacking(_enemy);
        _enemyFleeing = new EnemyFleeing(_enemy);
        _enemyDead = new EnemyDead(_enemy);

        _aggro = () => _enemy.target != null && _enemy.target.health.current > 0;
        //_aggroChase = () => _enemy.target != null && _enemy.target.health.current > 0 && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.enemyShoot.projectileAttribute.moveRange * 0.8f;
        _targetTooFarToAttack = () => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.enemyShoot.projectileAttribute.moveRange * 0.8f;
        _targetTooFarToFollow = () => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.followDistance;
        //_targetTooClose = () => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) < _enemy.enemyShoot.projectileAttribute.moveRange * 0.2f;
        _targetDisappeared = () => _enemy.target == null;
        _targetDied = () => _enemy.target != null && _enemy.target.health.current <= 0;
        _lowHealth = () => _enemy.health.current < _enemy.health.max * 0.2f; //make a field for percentage
        _died = () => _enemy.health.current <= 0;

        foreach (var transition in _transitions)
            StateMachine.AddTransition(GetState(transition.From), GetState(transition.To), GetCondition(transition.Condition));

        foreach (var anyTransition in _anyTransitions)
            StateMachine.AddAnyTransition(GetState(anyTransition.To), GetCondition(anyTransition.Condition));

        //StateMachine.AddTransition(enemyWandering, enemyChasing, targetTooFarToAttack);
        //StateMachine.AddTransition(enemyWandering, enemyAttacking, aggro);

        //StateMachine.AddTransition(enemyChasing, enemyWandering, targetDied);
        //StateMachine.AddTransition(enemyChasing, enemyWandering, targetDisappeared);
        //StateMachine.AddTransition(enemyChasing, enemyWandering, targetTooFarToFollow);
        //StateMachine.AddTransition(enemyChasing, enemyChasing, targetTooFarToAttack);
        //StateMachine.AddTransition(enemyChasing, enemyAttacking, aggro);

        //StateMachine.AddTransition(enemyAttacking, enemyWandering, targetDied);
        //StateMachine.AddTransition(enemyAttacking, enemyWandering, targetDisappeared);
        //StateMachine.AddTransition(enemyAttacking, enemyWandering, targetTooFarToFollow);
        //StateMachine.AddTransition(enemyAttacking, enemyChasing, targetTooFarToAttack);

        //StateMachine.AddAnyTransition(enemyDead, died);
        //StateMachine.AddAnyTransition(enemyFleeing, lowHealth);
    }

    private void Start()
    {
        //Set the initial state at Start instead of Awake to prevent any execution order conflict
        //(e.g. accessing NavMeshAgent before it's set)
        StateMachine.SetState(_enemyWandering);
    }

    private IState GetState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                return _enemyIdle;
            case EnemyState.Wandering:
                return _enemyWandering;
            case EnemyState.Chasing:
                return _enemyChasing;
            case EnemyState.Attacking:
                return _enemyAttacking;
            case EnemyState.Fleeing:
                return _enemyFleeing;
            case EnemyState.Dead:
                return _enemyDead;
            default:
                return null;
        }
    }

    private Func<bool> GetCondition(EnemyCondition condition)
    {
        switch (condition)
        {
            case EnemyCondition.Aggro:
                return _aggro;
            case EnemyCondition.AggroChase:
                return _aggroChase;
            case EnemyCondition.TargetTooFarToAttack:
                return _targetTooFarToAttack;
            case EnemyCondition.TargetTooFarToFollow:
                return _targetTooFarToFollow;
            case EnemyCondition.TargetTooClose:
                return _targetTooClose;
            case EnemyCondition.TargetDisappeared:
                return _targetDisappeared;
            case EnemyCondition.TargetDied:
                return _targetDied;
            case EnemyCondition.LowHealth:
                return _lowHealth;
            case EnemyCondition.Died:
                return _died;
            default:
                return null;
        }
    }
}

[Serializable]
public class EnemyTransition
{
    public EnemyState From;
    public EnemyState To;
    public EnemyCondition Condition;
}

[Serializable]
public class EnemyAnyTransition
{
    public EnemyState To;
    public EnemyCondition Condition;
}

public enum EnemyState { Idle, Wandering, Chasing, Attacking, Fleeing, Dead }

public enum EnemyCondition { Aggro, AggroChase, TargetTooFarToAttack, TargetTooFarToFollow, TargetTooClose, TargetDisappeared, TargetDied, LowHealth, Died }
