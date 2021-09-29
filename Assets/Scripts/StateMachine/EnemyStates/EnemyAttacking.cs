using Mirror;
using UnityEngine;

public class EnemyAttacking : IState
{
    public string Name => "ATTACKING";

    private readonly Enemy _enemy;

    private double _time;
    private Vector2 _position;

    public EnemyAttacking(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.movement.Reset();
        _position = _enemy.transform.position;
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        _enemy.enemyShoot.Shoot();

        if (NetworkTime.time < _time)
            return;

        Vector2 circle2D = Random.insideUnitCircle * 3f;

        _enemy.movement.Navigate(_position + circle2D, 0);
        _enemy.movement.SetSpeed(3f);

        _time = NetworkTime.time + 0.2f;
    }
}
