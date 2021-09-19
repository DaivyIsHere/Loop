public class EnemyFleeing : IState
{
    private readonly Enemy _enemy;

    public string Name => "FLEEING";

    public EnemyFleeing(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
    }
}
