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
        _enemy.target = null;
        _enemy.movement.Navigate(_enemy.startPosition, 0);
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
        //Set different speed for different state.
        //_enemy.movement.SetSpeed(2.5f);
        _enemy.movement.Navigate(_enemy.startPosition + circle2D, 0);
    }
}
