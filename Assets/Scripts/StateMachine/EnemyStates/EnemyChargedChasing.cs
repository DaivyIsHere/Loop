using Mirror;
using UnityEngine;

public class EnemyChargedChasing : IState
{
    public string Name => "CHASING";

    private readonly Enemy _enemy;

    private double _time;

    public EnemyChargedChasing(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        //_enemy.movement.Reset();
        _enemy.movement.SetSpeed(_enemy.chasingMoveSpeed);

        _time = NetworkTime.time + 1f;
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        if (NetworkTime.time < _time)
            return;

        var angle = Quaternion.Euler(0, 0, Random.Range(-90f, 90f));
        Vector2 direction = ((Vector2)_enemy.target.transform.position - (Vector2)_enemy.transform.position).normalized * _enemy.chasingMoveDistance;
        Vector2 destination = (Vector2)_enemy.transform.position + (Vector2)(angle * direction);

        _enemy.movement.Navigate(destination, 0);

        _time = NetworkTime.time + 1f;
    }
}
