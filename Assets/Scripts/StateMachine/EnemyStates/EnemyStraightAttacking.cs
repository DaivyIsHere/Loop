﻿public class EnemyStraightAttacking : IState
{
    public string Name => "ATTACKING";

    private readonly Enemy _enemy;

    public EnemyStraightAttacking(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.enemyShoot.Shoot();

        //_enemy.movement.Reset();
        _enemy.movement.SetSpeed(_enemy.attackingMoveSpeed);
    }

    public void OnExit()
    {
        _enemy.enemyShoot.StopShoot();
    }

    public void Tick()
    {
        _enemy.movement.Navigate(_enemy.target.transform.position, 0);
    }
}