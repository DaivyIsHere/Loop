using Mirror;
using UnityEngine;

public class EnemyChasing : IState
{
    public string Name => "CHASING";

    private readonly Enemy _enemy;

    private double _time;

    public EnemyChasing(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.movement.Reset();
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        if (NetworkTime.time < _time)
            return;

        Vector2 direction = ((Vector2)_enemy.target.transform.position - (Vector2)_enemy.transform.position).normalized * 3f;
        var angle = Quaternion.Euler(0, 0, Random.Range(-90f, 90f));
        var destination = (Vector2)_enemy.transform.position + (Vector2)(angle * direction);

        _enemy.movement.Navigate(destination, 0);
        _enemy.movement.SetSpeed(3f);

        _time = NetworkTime.time + 0.2f;

        //_enemy.movement.Navigate(_enemy.target.collider.ClosestPointOnBounds(_enemy.transform.position), _enemy.enemyShoot.projectileAttribute.moveRange * _enemy.attackToMoveRangeRatio);
    }
}
