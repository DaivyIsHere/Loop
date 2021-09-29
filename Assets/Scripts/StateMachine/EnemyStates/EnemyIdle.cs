public class EnemyIdle : IState
{
    public string Name => "IDLE";

    private readonly Enemy _enemy;

    public EnemyIdle(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.target = null;
        _enemy.movement.Reset();
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
    }
}
