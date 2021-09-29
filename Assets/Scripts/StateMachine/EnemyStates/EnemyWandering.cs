using UnityEngine;

public class EnemyWandering : IState
{
    public string Name => "WANDERING";

    private readonly Enemy _enemy;
    private readonly float _moveProbability;
    private readonly float _roamDistance;
    private readonly Vector2 _startPosition;

    public EnemyWandering(Enemy enemy, float moveProbability, float roamDistance, Vector2 startPosition)
    {
        _enemy = enemy;
        _moveProbability = moveProbability;
        _roamDistance = roamDistance;
        _startPosition = startPosition;
    }

    public void OnEnter()
    {
        _enemy.target = null;
        _enemy.movement.Navigate(_startPosition, 0);
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        if (_enemy.movement.IsMoving())
            return;

        if (Random.value > _moveProbability * Time.deltaTime)
            return;

        Vector2 circle2D = Random.insideUnitCircle * _roamDistance;
        //Set different speed for different state.
        //_enemy.movement.SetSpeed(2.5f);
        _enemy.movement.Navigate(_startPosition + circle2D, 0);
    }
}
