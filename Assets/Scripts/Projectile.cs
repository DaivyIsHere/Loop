using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public ProjectileAttribute attribute;

    //Dynamic variable
    public int damage = 5;//由武器的最大最小攻擊值隨機決定，利用子彈的最初發射時間(NetworkTime)當作Seed隨機取值
    [HideInInspector]
    public string owner = "";//用來給敵人辨識子彈發射者
    public bool IsFromPlayer = false;
    private Vector3 originPos = Vector3.zero;
    private float catchupDistance = 0f;

    void Start()
    {
        originPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    void Move()
    {
        float moveValue = attribute.moveSpeed * Time.deltaTime;
        float catchupValue = 0f;

        if (catchupDistance > 0)
        {
            float step = (catchupValue * Time.deltaTime);
            catchupDistance -= step;//減掉目標距離

            catchupValue = step;

            if (catchupDistance < (moveValue * 0.1f))
            {
                catchupValue += catchupDistance;
                catchupDistance = 0;
            }

        }

        transform.position += transform.up * (moveValue + catchupValue);

        if (Vector3.SqrMagnitude(transform.position - originPos) > Mathf.Pow(attribute.moveRange, 2))
            Destroy(this.gameObject);
    }

    public void Initialize(float duration)
    {
        GetComponentInChildren<SpriteRenderer>().sprite = attribute.sprite;
        GetComponent<CircleCollider2D>().radius = attribute.radius;
        catchupDistance = duration * attribute.moveSpeed;
    }
}

[System.Serializable]
public class ProjectileAttribute
{
    //static attribute，dynamic的變數都在Projectile腳本本身
    public ProjectileMovePattern movePattern = ProjectileMovePattern.staright;
    public int maxDamage;//攻擊者會透過max~minDamage x 自身攻擊力來給予Porjectile裡面的Damage值
    public int minDamage;//給予Damage值的方式會透過NetworkTime當作Seed來隨機
    public float moveRange = 5f;
    public float moveSpeed = 10f;
    public float radius = 0.25f;//調整collider大小
    public Sprite sprite;

    //Not Implemented yet
    public bool piercingEnemy = false;
    public bool piercingObject = false;//穿越物件，但不可穿越牆壁
    public bool ignoreDefence = false;
}

public enum ProjectileMovePattern
{
    staright
    //curve,//彎曲
    //floating,//左右擺動
    //circling,//圓圈圍繞角色
    //infinitas,//八字形圍繞
    //tracking//追蹤敵人
}

[System.Serializable]
public class ShootPattern
{
    [Range(0f,360f)]
    public float shootMainAngle = 0f;//主要射擊角度，在不旋轉的boss發射不同角度的彈幕上常用到
    [Range(0f,360f)]
    public float shootSpreadAngle = 0f;//射擊涵蓋範圍，範圍/子彈數量 = 子彈角度差
    public int shootAmount = 1;//單次射擊的子彈數量
    [Range(0f,360f)]
    public float angleDeviation = 0f;//角度誤差，利用子彈的最初發射時間(NetworkTime)當作Seed隨機取值
    public float shootBonusRate = 1f;//額外攻速改動，1.5f= 150% 攻擊速度，0.75= 75% 攻擊速度
}
