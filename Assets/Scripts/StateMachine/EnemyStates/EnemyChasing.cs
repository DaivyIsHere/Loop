public class EnemyChasing : IState
{
    private readonly Enemy _enemy;

    public string Name => "CHASING";

    public EnemyChasing(Enemy enemy)
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
        _enemy.movement.Navigate(_enemy.target.collider.ClosestPointOnBounds(_enemy.transform.position), _enemy.enemyShoot.projectileAttribute.moveRange * _enemy.attackToMoveRangeRatio);
    }
}
