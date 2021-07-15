using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Components")] // to be assigned in inspector
    public Player player;
    public PlayerShoot playerShoot;

    private float turnSpd = 10f;//目前方塊角色旋轉時會用到
    private double lastSentMovementTime;
    //private float movementResetTime = 0.5f;//若經過__秒過後沒有收到client端的移動指令則停下

    [Header("Joystick Controller")]
    private DynamicJoystick MoveJoystick;

    void Start()
    {
        if (base.hasAuthority)
        {
            MoveJoystick = JoystickControl.singleton.MoveJoystick;
        }
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            if(player.health.current == 0)//沒血量不能控制
            {
                GetComponent<Rigidbody2D>().velocity = Vector3.zero;
                return;
            }

            // simply accept input
#if UNITY_STANDALONE
            PlayerTryMove_Keyboard();
            //PlayerTryMove_Joystick();
#endif
#if UNITY_ANDROID
            PlayerTryMove_Joystick();
#endif
#if UNITY_IOS
            PlayerTryMove_Joystick();
#endif

        }
    }

    [Client]
    void PlayerTryMove_Keyboard()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        bool IsPressingMoveKeys = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        player.movementVector = new Vector3(x, y);//movement在entity裡面，在player腳本裡只有client端知道

        if (Vector3.Magnitude(player.movementVector) > 1)
            player.movementVector = Vector3.Normalize(player.movementVector);

        PlayerMove(player.movementVector, IsPressingMoveKeys);
    }

    [Client]
    void PlayerTryMove_Joystick()
    {
        float x = MoveJoystick.Horizontal;
        float y = MoveJoystick.Vertical;

        bool IsPressingMoveKeys = MoveJoystick.Direction != Vector2.zero;

        player.movementVector = new Vector3(x, y);

        //if (Vector3.Magnitude(player.movementVector) > 1)
            //player.movementVector = Vector3.Normalize(player.movementVector);

        PlayerMove(player.movementVector.normalized, IsPressingMoveKeys);
    }

    [Command]
    void CmdPlayerMovement(Vector3 movement, bool IsPressingMoveKeys)
    {
        lastSentMovementTime = NetworkTime.time;
        //transform.position += movement * walkSpd * Time.deltaTime;
        //transform.Translate((Vector3)movement * walkSpd * Time.deltaTime,Space.World );
        //GetComponent<Rigidbody2D>().MovePosition(transform.position + (movement * walkSpd * Time.deltaTime));
        GetComponent<Rigidbody2D>().velocity = movement * player.speed;
        //transform.up = Vector3.Lerp(transform.up, movement, turnSpd* Time.deltaTime);
        if (IsPressingMoveKeys)//movement.x != 0f || movement.y != 0f)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * turnSpd);
        }
    }

    void PlayerMove(Vector3 movement, bool IsPressingMoveKeys)
    {
        if (UIUtils.AnyInputActive())
        {
            GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            return;
        }

        //lastSentMovementTime = NetworkTime.time;
        //transform.position += movement * walkSpd * Time.deltaTime;
        GetComponent<Rigidbody2D>().velocity = movement * player.speed;
        //transform.up = Vector3.Lerp(transform.up, movement, turnSpd* Time.deltaTime);
        if (playerShoot.IsPressingShootingKeys())
        {
            float angle = Mathf.Atan2(playerShoot.shootDirection.y, playerShoot.shootDirection.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * turnSpd);
        }
        else if (IsPressingMoveKeys)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * turnSpd);
        }
    }
}
