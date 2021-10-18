﻿using Mirror;
using UnityEngine;

public class EnemyFleeing : IState
{
    public string Name => "FLEEING";

    private readonly Enemy _enemy;

    private double _time;

    public EnemyFleeing(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _enemy.movement.Reset();
        _enemy.movement.SetSpeed(_enemy.fleeingMoveSpeed);
    }

    public void OnExit()
    {
    }

    public void Tick()
    {
        if (NetworkTime.time < _time)
            return;

        var angle = Quaternion.Euler(0, 0, Random.Range(-90f, 90f));
        Vector2 direction = ((Vector2)_enemy.transform.position - (Vector2)_enemy.target.transform.position).normalized * _enemy.fleeingMoveDistance;
        Vector2 destination = (Vector2)_enemy.transform.position + (Vector2)(angle * direction);

        _enemy.movement.Navigate(destination, 0);

        _time = NetworkTime.time + 0.2f;
    }
}
