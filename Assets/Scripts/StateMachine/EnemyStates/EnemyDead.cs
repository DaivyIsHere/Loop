using System;

public class EnemyDead : IState
{
    public string Name => "DEAD";

    private readonly Enemy _enemy;
    private readonly Action _onDeath;

    public EnemyDead(Enemy enemy, Action onDeath)
    {
        _enemy = enemy;
        _onDeath = onDeath;
    }

    public void OnEnter()
    {
        _onDeath();
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
    }
}
