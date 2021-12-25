using UnityEngine;

public class EnemyWandering : IState
{
    public string Name => "WANDERING";

    private readonly Enemy _enemy;

    public EnemyWandering(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.enemyShoot.ResetShoot();

        _enemy.movement.SetSpeed(_enemy.wanderingMoveSpeed);
        _enemy.movement.Navigate(_enemy.startPosition, 0);
        _enemy.target = null;
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        if (_enemy.movement.IsMoving())
            return;

        if (Random.value > _enemy.moveProbability * Time.deltaTime)
            return;

        Vector2 circle2D = Random.insideUnitCircle * _enemy.roamDistance;
        Vector2 destination = _enemy.startPosition + circle2D;

        _enemy.movement.Navigate(destination, 0);
    }
}
