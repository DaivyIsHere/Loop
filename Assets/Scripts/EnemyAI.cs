using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Enemy _enemy;

    public StateMachine StateMachine;

    private void Awake()
    {
        StateMachine = new StateMachine();

        var enemyIdle = new EnemyIdle(_enemy);
        var enemyWandering = new EnemyWandering(_enemy);
        var enemyChasing = new EnemyChasing(_enemy);
        var enemyAttacking = new EnemyAttacking(_enemy);
        var enemyFleeing = new EnemyFleeing(_enemy);
        var enemyDead = new EnemyDead(_enemy);

        bool aggro() => _enemy.target != null && _enemy.target.health.current > 0;
        //bool aggroChase() => _enemy.target != null && _enemy.target.health.current > 0 && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.enemyShoot.projectileAttribute.moveRange * 0.8f;
        bool targetTooFarToAttack() => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.enemyShoot.projectileAttribute.moveRange * 0.8f;
        bool targetTooFarToFollow() => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) > _enemy.followDistance;
        //bool targetTooClose() => _enemy.target != null && Vector2.Distance(transform.position, _enemy.target.collider.ClosestPointOnBounds(transform.position)) < _enemy.enemyShoot.projectileAttribute.moveRange * 0.2f;
        bool targetDisappeared() => _enemy.target == null;
        bool targetDied() => _enemy.target != null && _enemy.target.health.current <= 0;
        bool lowHealth() => _enemy.health.current < _enemy.health.max * 0.2f; //make a field for percentage
        bool died() => _enemy.health.current <= 0;

        StateMachine.AddTransition(enemyWandering, enemyChasing, targetTooFarToAttack);
        StateMachine.AddTransition(enemyWandering, enemyAttacking, aggro);

        StateMachine.AddTransition(enemyChasing, enemyWandering, targetDied);
        StateMachine.AddTransition(enemyChasing, enemyWandering, targetDisappeared);
        StateMachine.AddTransition(enemyChasing, enemyWandering, targetTooFarToFollow);
        StateMachine.AddTransition(enemyChasing, enemyChasing, targetTooFarToAttack);
        StateMachine.AddTransition(enemyChasing, enemyAttacking, aggro);

        StateMachine.AddTransition(enemyAttacking, enemyWandering, targetDied);
        StateMachine.AddTransition(enemyAttacking, enemyWandering, targetDisappeared);
        StateMachine.AddTransition(enemyAttacking, enemyWandering, targetTooFarToFollow);
        StateMachine.AddTransition(enemyAttacking, enemyChasing, targetTooFarToAttack);

        StateMachine.AddAnyTransition(enemyDead, died);
        StateMachine.AddAnyTransition(enemyFleeing, lowHealth);
    }

    private void Start()
    {
        //Set the initial state at Start to prevent any execution order conflict
        //(e.g. accessing NavMeshAgent before it's set)
        StateMachine.SetState(new EnemyWandering(_enemy));
    }
}
