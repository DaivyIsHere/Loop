public class EnemyIdle : IState
{
    private readonly Enemy _enemy;

    public string Name => "IDLE";

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
