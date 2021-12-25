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
    private EnemyRandomChasing _enemyRandomChasing;
    private EnemyChargedChasing _enemyChargedChasing;
    private EnemyRandomAttacking _enemyRandomAttacking;
    private EnemyChargedAttacking _enemyChargedAttacking;
    private EnemyStraightAttacking _enemyStraightAttacking;
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
        _enemyRandomChasing = new EnemyRandomChasing(_enemy);
        _enemyChargedChasing = new EnemyChargedChasing(_enemy);
        _enemyRandomAttacking = new EnemyRandomAttacking(_enemy);
        _enemyChargedAttacking = new EnemyChargedAttacking(_enemy);
        _enemyStraightAttacking = new EnemyStraightAttacking(_enemy);
        _enemyFleeing = new EnemyFleeing(_enemy);
        _enemyDead = new EnemyDead(_enemy);

        // TODO: Move hard coded values into Enemy's decision parameters.
        _aggro = () => _enemy.target != null && _enemy.target.health.current > 0;
        //_aggroChase = () => _enemy.target != null && _enemy.target.health.current > 0 && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.enemyShoot.projectileAttribute.moveRange * 0.8f;
        _targetTooFarToAttack = () => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.enemyShoot.projectileAttribute.moveRange * 0.8f;
        _targetTooFarToFollow = () => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.followDistance;
        //_targetTooClose = () => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) < _enemy.enemyShoot.projectileAttribute.moveRange * 0.2f;
        _targetDisappeared = () => _enemy.target == null;
        _targetDied = () => _enemy.target != null && _enemy.target.health.current <= 0;
        _lowHealth = () => _enemy.health.current < _enemy.health.max * 0.2f;
        _died = () => _enemy.health.current <= 0;

        foreach (var transition in _transitions)
            StateMachine.AddTransition(GetState(transition.From), GetState(transition.To), GetCondition(transition.Condition));

        foreach (var anyTransition in _anyTransitions)
            StateMachine.AddAnyTransition(GetState(anyTransition.To), GetCondition(anyTransition.Condition));

        #region Test
        StateMachine.AddAnyTransition(_enemyDead, _died);
        StateMachine.AddAnyTransition(_enemyWandering, _targetDied);
        StateMachine.AddAnyTransition(_enemyWandering, _targetDisappeared);
        StateMachine.AddAnyTransition(_enemyWandering, _targetTooFarToFollow);
        StateMachine.AddAnyTransition(_enemyFleeing, _lowHealth);

        /// Random
        StateMachine.AddAnyTransition(_enemyRandomChasing, _targetTooFarToAttack);
        StateMachine.AddAnyTransition(_enemyRandomAttacking, _aggro);

        /// Charged
        //StateMachine.AddAnyTransition(_enemyChargedChasing, _targetTooFarToAttack);
        //StateMachine.AddAnyTransition(_enemyChargedAttacking, _aggro);

        /// Straight
        //StateMachine.AddAnyTransition(_enemyStraightAttacking, _aggro);
        #endregion
    }

    // Set the initial state at Start instead of Awake to prevent any execution order conflict
    // (e.g. accessing NavMeshAgent before it's set).
    private void Start() => StateMachine.SetState(_enemyWandering);

    private IState GetState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                return _enemyIdle;
            case EnemyState.Wandering:
                return _enemyWandering;
            case EnemyState.RandomChasing:
                return _enemyRandomChasing;
            case EnemyState.ChargedChasing:
                return _enemyChargedChasing;
            case EnemyState.RandomAttacking:
                return _enemyRandomAttacking;
            case EnemyState.ChargedAttacking:
                return _enemyChargedAttacking;
            case EnemyState.StraightAttacking:
                return _enemyStraightAttacking;
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

public enum EnemyState { Idle, Wandering, RandomChasing, ChargedChasing, RandomAttacking, ChargedAttacking, StraightAttacking, Fleeing, Dead }

public enum EnemyCondition { Aggro, AggroChase, TargetTooFarToAttack, TargetTooFarToFollow, TargetTooClose, TargetDisappeared, TargetDied, LowHealth, Died }
