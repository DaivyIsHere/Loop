using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnemyShoot : EntityProjectileLauncher
{
    [Header("Component")]
    public Enemy enemy;

    [SerializeField] private float _baseAttackSpeed = 2f;
    [SerializeField] private List<ShootMode> _shootModes;

    [HideInInspector]
    public bool IsPaused = false;

    private Vector2 _targetLastPosition;
    private Vector2 _predictedDirection;

    private IEnumerator _shootCoroutine;
    private IEnumerator _predictCoroutine;

    // TODO: Other classes have dependencies on moveRange, move the variable into Enemy's decision parameters.
    public ProjectileAttribute projectileAttribute;

    public void Shoot()
    {
        if (_predictCoroutine == null)
            _predictCoroutine = GetPrediction();

        if (_shootCoroutine == null)
            _shootCoroutine = LoopShoot();

        StartCoroutine(_predictCoroutine);
        StartCoroutine(_shootCoroutine);
    }

    public void StopShoot()
    {
        StopCoroutine(_shootCoroutine);
    }

    public void ResetShoot()
    {
        StopAllCoroutines();
        _predictCoroutine = GetPrediction();
        _shootCoroutine = LoopShoot();
    }

    private IEnumerator LoopShoot()
    {
        double cooldown = 0;

        while (true)
        {
            var index = 0;

            foreach (var shootMode in _shootModes)
            {
                var shootInterval = 1 / (_baseAttackSpeed * shootMode.ShootPattern.shootBonusRate);

                for (int i = 0; i < shootMode.RepeatedTime; i++)
                {
                    while (cooldown > NetworkTime.time || IsPaused)
                        yield return null;

                    var direction = shootMode.IsPredicted ? _predictedDirection : (Vector2)(enemy.target.transform.position - transform.position);
                    var networkTimeWhenShoot = NetworkTime.time;

                    LaunchProjectile(shootMode.ShootPattern, shootMode.ProjectileAttribute, enemy, direction, networkTimeWhenShoot, false);

                    RpcShoot(index, direction, networkTimeWhenShoot);

                    cooldown = NetworkTime.time + shootInterval;
                }

                cooldown = NetworkTime.time + shootMode.CooldownInterval;
                index++;
            }
        }
    }

    private IEnumerator GetPrediction()
    {
        _targetLastPosition = enemy.target.transform.position;

        while (true)
        {
            Vector2 offset = ((Vector2)enemy.target.transform.position - _targetLastPosition).normalized;
            _predictedDirection = (Vector2)enemy.target.transform.position + offset - (Vector2)enemy.transform.position;
            _targetLastPosition = enemy.target.transform.position;

            yield return null;
        }
    }

    [ClientRpc]
    private void RpcShoot(int index, Vector2 direciton, double networkTimeWhenShoot)
    {
        if (isServer)
            return;

        LaunchProjectile(_shootModes[index].ShootPattern, _shootModes[index].ProjectileAttribute, enemy, direciton, networkTimeWhenShoot, false);
    }

    /* Refactored
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
    //*/
}

[System.Serializable]
public class ShootMode
{
    public ProjectileAttribute ProjectileAttribute;
    public ShootPattern ShootPattern;
    public bool IsPredicted;
    public int RepeatedTime;
    public float CooldownInterval;
}
