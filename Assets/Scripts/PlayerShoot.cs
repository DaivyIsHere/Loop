using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerShoot : EntityProjectileLauncher
{
    [Header("Components")] // to be assigned in inspector
    public Player player;
    public PlayerEquipment equipment;

    [Header("Projectile")]
    public float _baseAttackSpeed = 2f;
    public float attackSpeed
    {
        get { return equipment.HasWeaponEquiped() ? 
        _baseAttackSpeed *equipment.GetWeapon().shootPattern.shootBonusRate : 
        _baseAttackSpeed; }
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

    //public ProjectileLauncher launcher;
    public Vector2 shootDirection = Vector2.zero;
    private double lastShootTime = -1f;//伺服器驗證用，-1表示並未發射過(第一次發射)

    [Header("Joystick Controller")]
    private DynamicJoystick ShootJoystick;
    public LayerMask autoAimLayer;
#pragma warning disable 0414 //Ignore warning
    private float autoAimMagnitude = 0.15f;
#pragma warning restore 0414

    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            ShootJoystick = JoystickControl.singleton.ShootJoystick;
            StartCoroutine(PlayerShooting());
            //print("Shoot Ready");
        }
        //print(equipment.HasWeaponEquiped());
        //print(player.inventory.slots[0].amount);
        //print(player.equipment.slots[0].item.hash);
    }

    [Client]
    public bool IsPressingShootingKeys()
    {
#if UNITY_STANDALONE
        if (Utils.IsCursorOverUserInterface())//如果滑鼠在UI上
            return false;
            
        return Input.GetMouseButton(0);
#endif

#if UNITY_ANDROID
        //return ShootJoystick.Direction != Vector2.zero;
        return ShootJoystick.IsPressingJoysitck;
#endif

#if UNITY_IOS
        //return ShootJoystick.Direction != Vector2.zero;
        return ShootJoystick.IsPressingJoysitck;
#endif
    }

    GameObject GetClosestTarget()
    {
        if (!equipment.HasWeaponEquiped())//if no weapon equiped
            return null;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, equipment.GetWeapon().projectileAttribute.moveRange, autoAimLayer);
        GameObject closestTarget = null;
        float closestDistance = -1f;//如果是-1則表示沒有目標，而有第一個目標時就會被更改為第一個目標的距離
        foreach (var c in colliders)
        {
            if (c.GetComponent<Enemy>())//確認是否為enemy
            {
                float distance = Vector2.Distance(transform.position, c.transform.position);//cache
                if (closestTarget == null)//如果目標為空
                {
                    closestTarget = c.gameObject;
                    closestDistance = distance;
                }
                else
                {
                    if (distance < closestDistance)
                    {
                        closestTarget = c.gameObject;
                        closestDistance = distance;
                    }
                }
            }
        }
        return closestTarget;
    }

    [Client]
    IEnumerator PlayerShooting()
    {
        while (true)
        {
            if (player.health.current != 0 && IsPressingShootingKeys())//沒有血量不能攻擊
            {
#if UNITY_ANDROID
                if (ShootJoystick.Direction.magnitude > autoAimMagnitude)
                {
                    shootDirection = ShootJoystick.Direction;//joystick
                }
                else
                {
                    GameObject closestTarget = GetClosestTarget();
                    if (closestTarget == null)
                    {
                        shootDirection = Vector2.zero;
                    }
                    else
                    {
                        shootDirection = (closestTarget.transform.position - transform.position).normalized;
                    }
                }
#endif

#if UNITY_IOS
                if (ShootJoystick.Direction.magnitude > autoAimMagnitude)
                {
                    shootDirection = ShootJoystick.Direction;//joystick
                }
                else
                {
                    GameObject closestTarget = GetClosestTarget();
                    if (closestTarget == null)
                    {
                        shootDirection = Vector2.zero;
                    }
                    else
                    {
                        shootDirection = (closestTarget.transform.position - transform.position).normalized;
                    }
                }
#endif

#if UNITY_STANDALONE
                Vector3 mousePos = Input.mousePosition;//mouse
                //mousePos = CameraFollowing.instance.GetComponent<Camera>().ScreenToWorldPoint(mousePos);
                Camera cam = CameraFollowing.instance.GetComponent<Camera>();
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit rayCastHit, 999f, CameraFollowing.instance.mousePlaneLayer))
                {
                    mousePos = rayCastHit.point;
                }
                shootDirection = mousePos - transform.position;
#endif

                if (equipment.HasWeaponEquiped())
                {
                    //滿足所有發射條件，開始執行發射
                    //print("fire")
                    double networkTimeWhenShoot = NetworkTime.time;//就算在同一幀內執行的networkTime也會不相同，需要統一networktime
                    if (!base.isServer)
                    {
                        LaunchProjectile(equipment.GetWeapon().shootPattern, equipment.GetWeapon().projectileAttribute, player, shootDirection, networkTimeWhenShoot, true);
                    }

                    CmdShoot(transform.position, shootDirection, networkTimeWhenShoot);
                    yield return new WaitForSeconds(shootInterval);
                }
                else
                {
                    print("There is no weapon equipped!!!");
                    yield return new WaitForSeconds(0.5f);
                }

            }

            yield return 0;
        }
    }

    [Command]
    public void CmdShoot(Vector3 pos, Vector2 direction, double networkTimeWhenShoot)
    {
        //驗證
        double cd = shootInterval;
        if (lastShootTime != -1 && networkTimeWhenShoot < (lastShootTime + cd))
            return;

        LaunchProjectile(equipment.GetWeapon().shootPattern, equipment.GetWeapon().projectileAttribute, player, direction, networkTimeWhenShoot, true);

        lastShootTime = networkTimeWhenShoot;
        RpcShoot(pos, direction, networkTimeWhenShoot);//client rcp
    }

    [ClientRpc]
    public void RpcShoot(Vector3 pos, Vector2 direction, double networkTimeWhenShoot)
    {
        if (isLocalPlayer)
            return;
        if (isServer)
            return;

        LaunchProjectile(equipment.GetWeapon().shootPattern, equipment.GetWeapon().projectileAttribute, player, direction, networkTimeWhenShoot, true);
    }
}
