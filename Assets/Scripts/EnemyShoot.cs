using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnemyShoot : EntityProjectileLauncher
{
    [Header("Component")]
    public Enemy enemy;

    [Header("Projectile")]
    public float _baseAttackSpeed = 2f;
    public float attackSpeed
    {
        get { return _baseAttackSpeed *shootPattern.shootBonusRate; }
    }
    public float shootInterval
    {
        get 
        { 
            if(attackSpeed > 0)
                return 1f / attackSpeed;
            else
                Debug.LogError("attackSpeed is equal or less than zero!");
                return 1f;
        }
    }

    [Header("Weapon")]
    public ShootPattern shootPattern;
    public ProjectileAttribute projectileAttribute;

    //public ProjectileLauncher launcher;
    private Vector2 shootDirection = Vector2.zero;
    private double lastShootTime = -1f;

    public void Shoot()
    {
        ///Rotate
        Vector3 direction = enemy.target.collider.ClosestPointOnBounds(transform.position) - (Vector2)this.transform.position;
        direction = Vector3.Normalize(direction);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
        //transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * enemy.turnSpd);
        //Projectile
        double cd = shootInterval;
        if (lastShootTime == -1f || (lastShootTime + cd) < NetworkTime.time)//確認CD
        {
            //滿足所有發射條件，開始執行發射
            //print("enemy fire")
            double networkTimeWhenShoot = NetworkTime.time;//就算在同一幀內執行的networkTime也會不相同，需要統一networktime
            shootDirection = enemy.target.transform.position - this.transform.position;
            LaunchProjectile(shootPattern, projectileAttribute, enemy, shootDirection, networkTimeWhenShoot, false);

            RpcShoot(transform.position, shootDirection, networkTimeWhenShoot);
            lastShootTime = NetworkTime.time;
        }
    }
    
    [ClientRpc]
    public void RpcShoot(Vector3 pos, Vector2 direction, double networkTimeWhenShoot)
    {
        if (isServer)
            return;

        LaunchProjectile(shootPattern, projectileAttribute, enemy, direction, networkTimeWhenShoot, false);
    }
}
