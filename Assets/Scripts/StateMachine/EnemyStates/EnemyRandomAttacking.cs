using Mirror;
using UnityEngine;

public class EnemyRandomAttacking : IState
{
    public string Name => "ATTACKING";

    private readonly Enemy _enemy;

    private double _time;
    private Vector2 _position;

    public EnemyRandomAttacking(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.enemyShoot.Shoot();

        //_enemy.movement.Reset();
        _enemy.movement.SetSpeed(_enemy.attackingMoveSpeed);
        _position = _enemy.transform.position;

        _time = NetworkTime.time + 0.2f;
    }

    public void OnExit()
    {
        _enemy.enemyShoot.StopShoot();
    }

    public void Tick()
    {
        if (NetworkTime.time < _time)
            return;

        Vector2 circle2D = Random.insideUnitCircle * _enemy.attackingMoveDistance;
        Vector2 destination = _position + circle2D;

        _enemy.movement.Navigate(destination, 0);

        // TODO: 0.2f is a hard coded value, consider move it into a field
        _time = NetworkTime.time + 0.2f;
    }
}
