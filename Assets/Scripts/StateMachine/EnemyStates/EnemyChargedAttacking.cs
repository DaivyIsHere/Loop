using Mirror;
using UnityEngine;

public class EnemyChargedAttacking : IState
{
    public string Name => "ATTACKING";

    private readonly Enemy _enemy;

    private double _time;
    private Vector2 _position;

    public EnemyChargedAttacking(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        //_enemy.movement.Reset();
        _enemy.movement.SetSpeed(_enemy.attackingMoveSpeed);
        _position = _enemy.transform.position;

        _time = NetworkTime.time + 2f;
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        if (!_enemy.movement.IsMoving())
            _enemy.enemyShoot.Shoot();

        if (NetworkTime.time < _time)
            return;

        Vector2 circle2D = Random.insideUnitCircle * _enemy.attackingMoveDistance;
        Vector2 destination = _position + circle2D;

        _enemy.movement.Navigate(destination, 0);

        _time = NetworkTime.time + 2f;
    }
}
