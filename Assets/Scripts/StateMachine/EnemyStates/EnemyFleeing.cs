using Mirror;
using UnityEngine;

public class EnemyFleeing : IState
{
    public string Name => "FLEEING";

    private readonly Enemy _enemy;

    private double _time;

    public EnemyFleeing(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        if (NetworkTime.time < _time)
            return;

        Vector2 direction = ((Vector2)_enemy.transform.position - (Vector2)_enemy.target.transform.position).normalized * 3.5f;
        var angle = Quaternion.Euler(0, 0, Random.Range(-90f, 90f));
        var destination = (Vector2)_enemy.transform.position + (Vector2)(angle * direction);

        _enemy.movement.Navigate(destination, 0);
        _enemy.movement.SetSpeed(3.5f);

        _time = NetworkTime.time + 0.2f;
    }
}
