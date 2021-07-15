using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[System.Serializable]
public class EntityProjectileLauncher : NetworkBehaviour
{
    public GameObject projectilePrefab;
    //public ShootPattern shootPattern;
    //public ProjectileAttribute projectileAttribute;
    
    //some launch function here
    public void LaunchProjectile(ShootPattern shootPattern, ProjectileAttribute projectileAttribute, Entity owner, Vector2 direction, double networkTimeWhenShoot, bool isFromPlayer)
    {
        //計算已經過時間
        double timePassed = NetworkTime.time - networkTimeWhenShoot;
        
        float angleDifference = shootPattern.shootSpreadAngle/(shootPattern.shootAmount);//算出每顆子彈的角度差
        float startAngle = shootPattern.shootMainAngle - ((-0.5f) * shootPattern.shootSpreadAngle);//第一個子彈的起始點角度, 順時針發射
        for (int i = 0; i < shootPattern.shootAmount; i++)
        {
            float angle = startAngle - ((i+ 0.5f) * angleDifference);
            //下行networkTimeWhenShoot乘以1000是因為randomSeed只能用int，因此同一秒的Random會完全相同，透過*1000來放大差距
            //同一波的子彈間距會相同，隨機的只有整組子彈的MainAngle
            float randomDeviation = Utils.RandomFloatBySeed((-1 * shootPattern.angleDeviation), shootPattern.angleDeviation, networkTimeWhenShoot*1000);
            angle += randomDeviation;//Random.Range((-1 * shootPattern.angleDeviation), shootPattern.angleDeviation);
            
            Projectile p = Instantiate(projectilePrefab, owner.transform.position, Quaternion.identity).GetComponent<Projectile>();
            p.transform.up = direction;
            p.transform.Rotate(0,0,angle);
            p.attribute = projectileAttribute;
            p.owner = owner.name;
            p.Initialize((float)timePassed);
            p.IsFromPlayer = isFromPlayer;
            //用seed隨機化傷害
            p.damage = (int)Utils.RandomFloatBySeed(projectileAttribute.minDamage, projectileAttribute.maxDamage, networkTimeWhenShoot*1000);
        }
    }
}
