public class EnemyAttacking : IState
{
    private readonly Enemy _enemy;

    public string Name => "ATTACKING";

    public EnemyAttacking(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.movement.Reset();
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        _enemy.enemyShoot.Shoot();
    }
}
