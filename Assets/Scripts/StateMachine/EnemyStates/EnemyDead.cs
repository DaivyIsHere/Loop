public class EnemyDead : IState
{
    public string Name => "DEAD";

    private readonly Enemy _enemy;

    public EnemyDead(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.OnDeath();
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
    }
}
