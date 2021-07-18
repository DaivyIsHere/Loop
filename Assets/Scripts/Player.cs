using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerChat))]
[RequireComponent(typeof(PlayerShoot))]
[RequireComponent(typeof(PlayerWater))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerLooting))]
[RequireComponent(typeof(NetworkNavMeshAgentRubberbanding2D))]

public class Player : Entity
{
    [Header("Components")]
    public PlayerChat chat;
    public PlayerInventory inventory;
    public PlayerEquipment equipment;
    public PlayerLooting playerLooting;
    public PlayerGathering playerGathering;
    public PlayerShoot playerShoot;
    public PlayerWater playerWater;
    public NetworkNavMeshAgentRubberbanding2D rubberbanding;

    [Header("Overlay")]
    public GameObject overlayPosition;
    public Vector3 overlayOffset = new Vector3(0, 1, 0);
    public GameObject amountOverlay;
    public Color waterTextColor = new Color32(62, 136, 219, 255);

    [Header("Text Meshes")]
    public TextMesh nameOverlay;
    public Color nameOverlayDefaultColor = Color.white;

    [Header("Coin")]
    [SyncVar, SerializeField] long _coin = 0;
    public long coin { get { return _coin; } set { _coin = Math.Max(value, 0); } }

    [Header("Movement")]
    public Vector3 movementVector = Vector3.zero;

    // some meta info
    public int id = -1;
    public string account = "";
    public string className = "";
    public DateTime createdTime;//只有server端的player會從database讀取與儲存，不會同步到客戶端

    //暫時當作分辨重生的flag，只在server端有用
    public bool isReviving = false;

    // localPlayer singleton for easier access from UI scripts etc.
    public static Player localPlayer;

    // item cooldowns
    // it's based on a 'cooldownCategory' that can be set in ScriptableItems.
    // -> they can use their own name for a cooldown that only applies to them
    // -> they can use a category like 'HealthPotion' for a shared cooldown
    //    amongst all health potions
    // => we could use hash(category) as key to significantly reduce bandwidth,
    //    but we don't anymore because it makes database saving easier.
    //    otherwise we would have to find the category from a hash.
    // => IMPORTANT: cooldowns need to be saved in database so that long
    //    cooldowns can't be circumvented by logging out and back in again.
    internal SyncDictionary<string, double> itemCooldowns = new SyncDictionary<string, double>();

    public bool isPreviewing = false;//When IsPreviewing, disable the nameOverlay

    // cache players to save lots of computations
    // (otherwise we'd have to iterate NetworkServer.objects all the time)
    // => on server: all online players
    // => on client: all observed players
    public static Dictionary<string, Player> onlinePlayers = new Dictionary<string, Player>();

    // first allowed logout time after combat
    public double allowedLogoutTime => /*lastCombatTime + */((NetworkManagerMMO)NetworkManager.singleton).combatLogoutDelay;
    public double remainingLogoutTime => NetworkTime.time < allowedLogoutTime ? (allowedLogoutTime - NetworkTime.time) : 0;

    [Header("Weather")]
    public float RainingStage;//Client端的變數，控制雨的顯示與大小//範圍介於0~1之間
    private float UpdateRainInterval = 0.5f;

    // Start is called before the first frame update
    protected override void Start()
    {
        //print(name +" started!");
        //print(name +" isServer : "+ isServer);
        //print(name +" isClient : "+ isClient);
        // do nothing if not spawned (=for character selection previews)
        if (!isServer && !isClient) return;
        base.Start();

        onlinePlayers[name] = this;
        //print(name +"Joined!");

        if (base.hasAuthority)//如果是本地play，呼叫Camera Follow
        {
            CameraFollowing.instance.transform.position = transform.position;
            CameraFollowing.instance.followObject = this.gameObject;

            InvokeRepeating(nameof(UpdateRain), 0, UpdateRainInterval);
        }
    }

    public override void OnStartLocalPlayer()
    {
        // set singleton
        localPlayer = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        playerWater.InvokeRepeating(nameof(playerWater.LoseWater), playerWater.waterLoseInterval, playerWater.waterLoseInterval);
    }

    public override void OnStartClient()
    {
        //關閉其他的Rigidbody，避免其他player的ridgidbody被碰撞的時候運算移動量
        GetComponent<Rigidbody2D>().isKinematic = !isLocalPlayer;
        //GetComponent<Rigidbody2D>().isKinematic = true;
        if (isLocalPlayer)
        {
            //StartCoroutine(PlayerShooting());
            //print("Shoot Ready");
        }
    }

    void OnDestroy()
    {
        // try to remove from onlinePlayers first, NO MATTER WHAT
        // -> we can not risk ever not removing it. do this before any early
        //    returns etc.
        // -> ONLY remove if THIS object was saved. this avoids a bug where
        //    a host selects a character preview, then joins the game, then
        //    only after the end of the frame the preview is destroyed,
        //    OnDestroy is called and the preview would actually remove the
        //    world player from onlinePlayers. hence making guild management etc
        //    impossible.
        if (onlinePlayers.TryGetValue(name, out Player entry) && entry == this)
            onlinePlayers.Remove(name);

        // do nothing if not spawned (=for character selection previews)
        if (!isServer && !isClient) return;

        if (isLocalPlayer) // requires at least Unity 5.5.1 bugfix to work
        {
            localPlayer = null;
        }
    }

    [Server]
    protected override string UpdateServer()
    {
        if (health.current == 0 && !isReviving)
        {
            RpcChangeSquareColor(true);
            isReviving = true;
            Invoke("Respawn", 3f);
        }
        return "IDLE";
    }

    // finite state machine - client ///////////////////////////////////////////
    [Client]
    protected override void UpdateClient()
    {
        /*
        if (isLocalPlayer)
        {
            if (!UIUtils.AnyInputActive() && Input.GetKeyDown(KeyCode.R))
                CmdTestRain();
            if (!UIUtils.AnyInputActive() && Input.GetKeyDown(KeyCode.F))
                CmdTestFountain();
            if (!UIUtils.AnyInputActive() && Input.GetKeyDown(KeyCode.G))
                CmdTestSpawnEnemy();
        }*/
        ///因為當物件被生成於客戶端時，Start裡面的isClient以及isServer有機率都是否，所以必須要驗證是否成功加入onlinePlayer名單內
        if(!onlinePlayers.ContainsKey(name))
        {
            onlinePlayers[name] = this;//未來可能會改在當發送ClientRPC的時候，進行檢測是否在onlinePlayers名單內
        }
    }

    [Server]
    public void Respawn()//因為目前由玩家端擁有移動的控制權，未來會再更改
    {
        RpcChangeSquareColor(false);
        health.current = health.max;
        isReviving = false;
    }

    [ClientRpc]
    public void RpcChangeSquareColor(bool dead)
    {
        if (dead)
        {
            GetComponentInChildren<SpriteRenderer>().color = Color.gray;
        }
        else
        {
            GetComponentInChildren<SpriteRenderer>().color = Color.white;
        }
    }

    protected override void UpdateOverlays()
    {
        base.UpdateOverlays();

        if (overlayPosition != null)
        {
            //更改成animation之後就不用了，現階段因為旋轉整個player
            //overlayPosition.transform.position = transform.position + new Vector3(0, 1f, 0);
            //overlayPosition.transform.eulerAngles = Vector3.zero;

            // only players need to copy names to name overlay. it never changes
            // for monsters / npcs.
            if (isPreviewing)
                nameOverlay.text = "";
            else
                nameOverlay.text = name;//name 是 UserName
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        if (other.tag == "Projectile")
        {
            if (!other.GetComponent<Projectile>())
                return;
            if (other.GetComponent<Projectile>().IsFromPlayer)
                return;
            if (health.current == 0)//死亡時不會被子彈打到
                return;

            Destroy(other.gameObject);
            ///SHOW DAMAGE POPUP
            int damage = other.GetComponent<Projectile>().damage;
            
            ///Popup
            combat.ShowDamagePopup(damage);
            /*
            string text;
            if (damage > 0)
                text = "-" + damage;
            else
                text = "";
            GameObject overlay = Instantiate(amountOverlay, transform.position + overlayOffset, Quaternion.identity, nameOverlay.transform);
            overlay.GetComponentInChildren<TextMesh>().color = damageTextColor;
            overlay.GetComponentInChildren<TextMesh>().text = text;
            */

            if (isServer)//only decrease hp if server
            {
                health.current -= damage;//Will be clamped By Health script
            }
        }
    }

    // item cooldowns //////////////////////////////////////////////////////////
    // get remaining item cooldown, or 0 if none
    public float GetItemCooldown(string cooldownCategory)
    {
        // find cooldown for that category
        if (itemCooldowns.TryGetValue(cooldownCategory, out double cooldownEnd))
        {
            return NetworkTime.time >= cooldownEnd ? 0 : (float)(cooldownEnd - NetworkTime.time);
        }

        // none found
        return 0;
    }

    // reset item cooldown
    public void SetItemCooldown(string cooldownCategory, float cooldown)
    {
        // save end time
        itemCooldowns[cooldownCategory] = NetworkTime.time + cooldown;
    }

    public void UpdateRain()
    {
        //print("UpdateRain");
        RainingStage = 0;
        Collider2D[] collider2Ds = Physics2D.OverlapPointAll(transform.position, LayerMask.GetMask("Area"));
        foreach (var c in collider2Ds)
        {
            if (c.GetComponent<RainArea>())
            {
                float stage = c.GetComponent<RainArea>().GetRainStage(transform.position);
                if (stage > RainingStage)//找出最大的stage
                {
                    RainingStage = stage;
                }
            }
        }
        //print("stage = " + RainingStage);
        WeatherController.singleton.ToggleRaining(RainingStage);
    }

    [Command]
    public void CmdTestRain()
    {
        GameObject NewRainArea = Instantiate(EnvironmentManager.singleton.RainAreaPref);
        NewRainArea.transform.position = this.transform.position;
        NewRainArea.GetComponent<RainArea>().id = EnvironmentManager.singleton.RainAreaList.Count + 1;
        NewRainArea.GetComponent<RainArea>().RainFinishedTime = DateTime.UtcNow.AddMinutes(10);
        EnvironmentManager.singleton.SpawnRainArea(NewRainArea);
        Database.singleton.SaveRainArea();
    }

    [Command]
    public void CmdTestFountain()
    {
        //之後要確認是否在同一位置上已有存在的fountain
        GameObject NewFountain = Instantiate(EnvironmentManager.singleton.fountainPref);
        Fountain fountain = NewFountain.GetComponent<Fountain>();
        fountain.id = EnvironmentManager.singleton.fountainList.Count + 1;
        NewFountain.transform.position = this.transform.position;
        fountain.builder = this.name;
        fountain.builtTime = DateTime.UtcNow;
        EnvironmentManager.singleton.SpawnFountain(NewFountain);
        Database.singleton.SaveFountain();
    }

    [Command]
    public void CmdTestBush()
    {
        var bush = Instantiate(EnvironmentManager.singleton.bushPref);
        bush.transform.position = transform.position;
        bush.GetComponent<Bush>().id = EnvironmentManager.singleton.bushList.Count + 1;

        EnvironmentManager.singleton.SpawnBush(bush);
        Database.singleton.SaveBush();
    }

    [Command]
    public void CmdTestSpawnEnemy()
    {
        Spawner.singleton.SpawnEnemy(transform.position);
    }

    [Command]
    public void CmdTestSavePlayer()
    {
        Database.singleton.CharacterSaveMany(Player.onlinePlayers.Values);
        if (Player.onlinePlayers.Count > 0)
            Debug.Log("saved " + Player.onlinePlayers.Count + " player(s)");
    }

    [Command]
    public void CmdTestSaveEnvir()
    {
        Database.singleton.SaveEnvironmentData();
        Debug.Log("EnvironmentData Saved");
    }

    [Command]
    public void CmdTestNight()
    {
        Spawner.singleton.SpawnBlackScreen();
    }

    [Command]
    public void CmdTestDay()
    {
        Spawner.singleton.Day();
    }

    [Command]
    public void CmdTestFirework()
    {
        Spawner.singleton.SpawnFirework(transform.position);
    }

    [Command]
    public void CmdAddItem(string playerName, int amount, string itemName)
    {
        Database.singleton.AddItemToCharacter(playerName, amount, itemName);
    }

    [Command]
    public void CmdRemoveItem(string playerName, int amount, string itemName)
    {
        Database.singleton.RemoveItemFromCharacter(playerName, amount, itemName);
    }

}
