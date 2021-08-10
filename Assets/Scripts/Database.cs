// Saves Character Data in a SQLite database. We use SQLite for several reasons
//
// - SQLite is file based and works without having to setup a database server
//   - We can 'remove all ...' or 'modify all ...' easily via SQL queries
//   - A lot of people requested a SQL database and weren't comfortable with XML
//   - We can allow all kinds of character names, even chinese ones without
//     breaking the file system.
// - We will need MYSQL or similar when using multiple server instances later
//   and upgrading is trivial
// - XML is easier, but:
//   - we can't easily read 'just the class of a character' etc., but we need it
//     for character selection etc. often
//   - if each account is a folder that contains players, then we can't save
//     additional account info like password, banned, etc. unless we use an
//     additional account.xml file, which over-complicates everything
//   - there will always be forbidden file names like 'COM', which will cause
//     problems when people try to create accounts or characters with that name
//
// About item mall coins:
//   The payment provider's callback should add new orders to the
//   character_orders table. The server will then process them while the player
//   is ingame. Don't try to modify 'coins' in the character table directly.
//
// Tools to open sqlite database files:
//   Windows/OSX program: http://sqlitebrowser.org/
//   Firefox extension: https://addons.mozilla.org/de/firefox/addon/sqlite-manager/
//   Webhost: Adminer/PhpLiteAdmin
//
// About performance:
// - It's recommended to only keep the SQLite connection open while it's used.
//   MMO Servers use it all the time, so we keep it open all the time. This also
//   allows us to use transactions easily, and it will make the transition to
//   MYSQL easier.
// - Transactions are definitely necessary:
//   saving 100 players without transactions takes 3.6s
//   saving 100 players with transactions takes    0.38s
// - Using tr = conn.BeginTransaction() + tr.Commit() and passing it through all
//   the functions is ultra complicated. We use a BEGIN + END queries instead.
//
// Some benchmarks:
//   saving 100 players unoptimized: 4s
//   saving 100 players always open connection + transactions: 3.6s
//   saving 100 players always open connection + transactions + WAL: 3.6s
//   saving 100 players in 1 'using tr = ...' transaction: 380ms
//   saving 100 players in 1 BEGIN/END style transactions: 380ms
//   saving 100 players with XML: 369ms
//   saving 1000 players with mono-sqlite @ 2019-10-03: 843ms
//   saving 1000 players with sqlite-net  @ 2019-10-03:  90ms (!)
//
// Build notes:
// - requires Player settings to be set to '.NET' instead of '.NET Subset',
//   otherwise System.Data.dll causes ArgumentException.
// - requires sqlite3.dll x86 and x64 version for standalone (windows/mac/linux)
//   => found on sqlite.org website
// - requires libsqlite3.so x86 and armeabi-v7a for android
//   => compiled from sqlite.org amalgamation source with android ndk r9b linux
using UnityEngine;
using Mirror;
using System;
using System.IO;
using System.Collections.Generic;
using SQLite; // from https://github.com/praeclarum/sqlite-net
using UnityEngine.AI;

public partial class Database : MonoBehaviour
{
    // singleton for easier access
    public static Database singleton;

    // file name
    public string databaseFile = "Database.sqlite";

    // connection
    SQLiteConnection connection;

    // database layout via .NET classes:
    // https://github.com/praeclarum/sqlite-net/wiki/GettingStarted
    class accounts
    {
        [PrimaryKey] // important for performance: O(log n) instead of O(n)
        public string accountname { get; set; }
        public string password { get; set; }
        public string username { get; set; }
        // created & lastlogin for statistics like CCU/MAU/registrations/...
        public DateTime created { get; set; }
        public DateTime lastlogin { get; set; }
        public bool banned { get; set; }
    }
    class characters
    {
        [PrimaryKey] // important for performance: O(log n) instead of O(n)
        //[Collation("NOCASE")] // [COLLATE NOCASE for case insensitive compare. this way we can't both create 'Archer' and 'archer' as characters]
        public int id { get; set; } //由ManagerMMO.OnServerCharacterCreate 自動給予ID
        [Indexed] // add index on account to avoid full scans when loading characters
        public string account { get; set; }
        public string name { get; set; }//UserName
        public string classname { get; set; } // 'class' isn't available in C#
        public float x { get; set; }
        public float y { get; set; }
        public int health { get; set; }
        public int waterAmount { get; set; }
        /*
        public int level { get; set; }
        public int mana { get; set; }
        public int strength { get; set; }
        public int intelligence { get; set; }
        public long experience { get; set; } // TODO does long work?
        public long skillExperience { get; set; } // TODO does long work?
        public long gold { get; set; } // TODO does long work?
        public long coins { get; set; } // TODO does long work?
        // online status can be checked from external programs with either just
        // just 'online', or 'online && (DateTime.UtcNow - lastsaved) <= 1min)
        // which is robust to server crashes too.
        */
        public bool online { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime lastsaved { get; set; }
        public bool deleted { get; set; }
    }
    class character_inventory
    {
        public int characterId { get; set; }
        public int slot { get; set; }
        public string name { get; set; }
        public int amount { get; set; }
        // PRIMARY KEY (character, slot) is created manually.
    }
    class character_equipment : character_inventory // same layout
    {
        // PRIMARY KEY (character, slot) is created manually.
    }
    class character_itemcooldowns
    {
        [PrimaryKey] // important for performance: O(log n) instead of O(n)
        public int characterId { get; set; }
        public string category { get; set; }
        public float cooldownEnd { get; set; }
    }
    class Envir_WaterData
    {
        [PrimaryKey]
        public int id { get; set; }//Is always 0, it means the main Data.
        public int groundWater { get; set; }
        public int groundWater_Pure { get; set; }
        public int airWater_Pure { get; set; }
        //public int groundWater_Polluted { get; set; }
        //public int airWater { get; set; }
        //public int airWater_Pure { get; set; }
        //public int airWater_Polluted { get; set; }
        public DateTime lastsaved { get; set; }
    }
    class Envir_RainArea
    {
        [PrimaryKey]
        public int id { get; set; }//用Vector2組成的string，例 3,-2
        public float position_X { get; set; }
        public float position_Y { get; set; }
        public DateTime RainFinishedTime { get; set; }
        public DateTime lastsaved { get; set; }
    }
    class Envir_Fountain
    {
        //static
        [PrimaryKey]
        public int id { get; set; }
        public float position_X { get; set; }
        public float position_Y { get; set; }
        public string builder { get; set; } //player account, but display ign instead
        public DateTime builtTime { get; set; }
        //public string typeName { get; set; } 未來用來記錄不同種類的水泉

        //dynamic
        public int totalusedTimes { get; set; }
        public int totalDrankAmount { get; set; }
        public DateTime lastsaved { get; set; }
    }

    class Envir_Bush
    {
        [PrimaryKey]
        public int id { get; set; }
        public float positionX { get; set; }
        public float positionY { get; set; }
        public int remaningAmount { get; set; }
        public float refreshTime { get; set; }
    }

    void Awake()
    {
        // initialize singleton
        if (singleton == null) singleton = this;
    }

    // connect /////////////////////////////////////////////////////////////////
    // only call this from the server, not from the client. otherwise the client
    // would create a database file / webgl would throw errors, etc.
    public void Connect()
    {
        // database path: Application.dataPath is always relative to the project,
        // but we don't want it inside the Assets folder in the Editor (git etc.),
        // instead we put it above that.
        // we also use Path.Combine for platform independent paths
        // and we need persistentDataPath on android
#if UNITY_EDITOR
        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, databaseFile);
        //print(path);
        //print(Path.Combine(Application.dataPath, databaseFile));
#elif UNITY_ANDROID
        string path = Path.Combine(Application.persistentDataPath, databaseFile);
#elif UNITY_IOS
        string path = Path.Combine(Application.persistentDataPath, databaseFile);
#else
        string path = Path.Combine(Application.dataPath, databaseFile);
#endif

        // open connection
        // note: automatically creates database file if not created yet
        connection = new SQLiteConnection(path);

        // create tables if they don't exist yet or were deleted
        connection.CreateTable<accounts>();
        connection.CreateTable<characters>();
        connection.CreateTable<character_inventory>();
        connection.CreateIndex(nameof(character_inventory), new[] { "character", "slot" });
        connection.CreateTable<character_equipment>();
        connection.CreateIndex(nameof(character_equipment), new[] { "character", "slot" });
        connection.CreateTable<character_itemcooldowns>();
        connection.CreateTable<Envir_WaterData>();
        connection.CreateTable<Envir_Fountain>();
        connection.CreateTable<Envir_RainArea>();
        connection.CreateTable<Envir_Bush>();

        AddTestAccount();

        //Debug.Log("connected to database");
    }

    // close connection when Unity closes to prevent locking
    void OnApplicationQuit()
    {
        connection?.Close();
    }

    ///TEST ACCOUNTS
    void AddTestAccount()
    {
        if (!TryLogin("admin", "FBF8195D15DB0E579E541244412D74314ECFA263"))
        {
            if (TryRegister("admin", "FBF8195D15DB0E579E541244412D74314ECFA263", "Admin"))
                print("Admin created successfully!");
            else
                print("Admin created Failed!!!!");
        }

        if (!TryLogin("Dd", "E6FAE5CA2A50A53561671B906B138ECE77BF0B81"))
        {
            if (TryRegister("Dd", "E6FAE5CA2A50A53561671B906B138ECE77BF0B81", "Dd"))
                print("Dd created successfully!");
            else
                print("Dd created Failed!!!!");
        }

        if (!TryLogin("Aa", "1D56EA8DB6DA672CA6EFD38DDD53107F1107681F"))
        {
            if (TryRegister("Aa", "1D56EA8DB6DA672CA6EFD38DDD53107F1107681F", "Aa"))
                print("Aa created successfully!");
            else
                print("Aa created Failed!!!!");
        }

        if (Database.singleton.CharactersForAccount("admin").Count == 0)
        {
            int newId = GetNewCharacterId();
            string userName = "Admin";
            NetworkManagerMMO manager = (NetworkManagerMMO)NetworkManagerMMO.singleton;
            Player player = manager.CreateCharacter(manager.playerClasses[0].gameObject, newId, userName, "admin");
            print("Successfully created a character for Admin!");
            CharacterSave(player, false);
            Destroy(player.gameObject);
        }

        if (Database.singleton.CharactersForAccount("Dd").Count == 0)
        {
            int newId = GetNewCharacterId();
            string userName = "Dd";
            NetworkManagerMMO manager = (NetworkManagerMMO)NetworkManagerMMO.singleton;
            Player player = manager.CreateCharacter(manager.playerClasses[0].gameObject, newId, userName, "Dd");
            print("Successfully created a character for Dd!");
            CharacterSave(player, false);
            Destroy(player.gameObject);
        }

        if (Database.singleton.CharactersForAccount("Aa").Count == 0)
        {
            int newId = GetNewCharacterId();
            string userName = "Aa";
            NetworkManagerMMO manager = (NetworkManagerMMO)NetworkManagerMMO.singleton;
            Player player = manager.CreateCharacter(manager.playerClasses[0].gameObject, newId, userName, "Aa");
            print("Successfully created a character for Aa!");
            CharacterSave(player, false);
            Destroy(player.gameObject);
        }
    }

    // account data ////////////////////////////////////////////////////////////
    // try to log in with an account.
    // -> not called 'CheckAccount' or 'IsValidAccount' because it both checks
    //    if the account is valid AND sets the lastlogin field
    public bool TryLogin(string account, string password)
    {
        // this function can be used to verify account credentials in a database
        // or a content management system.
        //
        // for example, we could setup a content management system with a forum,
        // news, shop etc. and then use a simple HTTP-GET to check the account
        // info, for example:
        //
        //   var request = new WWW("example.com/verify.php?id="+id+"&amp;pw="+pw);
        //   while (!request.isDone)
        //       print("loading...");
        //   return request.error == null && request.text == "ok";
        //
        // where verify.php is a script like this one:
        //   <?php
        //   // id and pw set with HTTP-GET?
        //   if (isset($_GET['id']) && isset($_GET['pw'])) {
        //       // validate id and pw by using the CMS, for example in Drupal:
        //       if (user_authenticate($_GET['id'], $_GET['pw']))
        //           echo "ok";
        //       else
        //           echo "invalid id or pw";
        //   }
        //   ?>
        //
        // or we could check in a MYSQL database:
        //   var dbConn = new MySql.Data.MySqlClient.MySqlConnection("Persist Security Info=False;server=localhost;database=notas;uid=root;password=" + dbpwd);
        //   var cmd = dbConn.CreateCommand();
        //   cmd.CommandText = "SELECT id FROM accounts WHERE id='" + account + "' AND pw='" + password + "'";
        //   dbConn.Open();
        //   var reader = cmd.ExecuteReader();
        //   if (reader.Read())
        //       return reader.ToString() == account;
        //   return false;
        //
        // as usual, we will use the simplest solution possible:
        // create account if not exists, compare password otherwise.
        // no CMS communication necessary and good enough for an Indie MMORPG.

        // not empty?
        if (!string.IsNullOrWhiteSpace(account) && !string.IsNullOrWhiteSpace(password))
        {
            // demo feature: create account if it doesn't exist yet.
            // note: sqlite-net has no InsertOrIgnore so we do it in two steps
            if (connection.FindWithQuery<accounts>("SELECT * FROM accounts WHERE accountname=?", account) == null)
                return false;//沒找到對應帳號
                             //connection.Insert(new accounts{ name=account, password=password, created=DateTime.UtcNow, lastlogin=DateTime.Now, banned=false});

            // check account name, password, banned status
            if (connection.FindWithQuery<accounts>("SELECT * FROM accounts WHERE accountname=? AND password=? and banned=0", account, password) != null)
            {
                // save last login time and return true
                connection.Execute("UPDATE accounts SET lastlogin=? WHERE accountname=?", DateTime.UtcNow, account);
                return true;
            }
        }
        return false;
    }

    public bool TryRegister(string account, string password, string username)
    {
        // not empty?
        if (!string.IsNullOrWhiteSpace(account) && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(username))
        {
            if (connection.FindWithQuery<accounts>("SELECT * FROM accounts WHERE accountname=?", account) != null)
                return false;//帳號已存在

            if (connection.FindWithQuery<accounts>("SELECT * FROM accounts WHERE username=?", username) != null)
                return false;//使用者名稱名稱已存在

            connection.Insert(new accounts { accountname = account, password = password, username = username, created = DateTime.UtcNow, lastlogin = DateTime.Now, banned = false });
            return true;//註冊成功
        }
        return false;
    }

    // character data //////////////////////////////////////////////////////////
    public bool CharacterExists(int characterId)
    {
        // checks deleted ones too so we don't end up with duplicates if we un-
        // delete one
        return connection.FindWithQuery<characters>("SELECT * FROM characters WHERE id=?", characterId) != null;
    }

    public void CharacterDelete(int characterId)
    {
        // soft delete the character so it can always be restored later
        connection.Execute("UPDATE characters SET deleted=1 WHERE id=?", characterId);
    }

    // returns the list of character names for that account
    // => all the other values can be read with CharacterLoad!
    public List<int> CharactersForAccount(string account)
    {
        List<int> result = new List<int>();
        foreach (characters character in connection.Query<characters>("SELECT * FROM characters WHERE account=? AND deleted=0", account))
            result.Add(character.id);
        return result;
    }

    void LoadInventory(PlayerInventory inventory)
    {
        // fill all slots first
        for (int i = 0; i < inventory.size; ++i)
            inventory.slots.Add(new ItemSlot());

        // then load valid items and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        foreach (character_inventory row in connection.Query<character_inventory>("SELECT * FROM character_inventory WHERE characterId=?", inventory.player.id))
        {
            if (row.slot < inventory.size)
            {
                if (ScriptableItem.All.TryGetValue(row.name.GetStableHashCode(), out ScriptableItem itemData))
                {
                    Item item = new Item(itemData);
                    //item.summonedHealth = row.summonedHealth;
                    //item.summonedLevel = row.summonedLevel;
                    //item.summonedExperience = row.summonedExperience;
                    inventory.slots[row.slot] = new ItemSlot(item, row.amount);
                }
                else Debug.LogWarning("LoadInventory: skipped item " + row.name + " for " + inventory.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
            }
            else Debug.LogWarning("LoadInventory: skipped slot " + row.slot + " for " + inventory.name + " because it's bigger than size " + inventory.size);
        }
    }

    void LoadEquipment(PlayerEquipment equipment)
    {
        // fill all slots first
        for (int i = 0; i < equipment.slotInfo.Length; ++i)
            equipment.slots.Add(new ItemSlot());

        // then load valid equipment and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        foreach (character_equipment row in connection.Query<character_equipment>("SELECT * FROM character_equipment WHERE characterId=?", equipment.player.id))
        {
            if (row.slot < equipment.slotInfo.Length)
            {
                if (ScriptableItem.All.TryGetValue(row.name.GetStableHashCode(), out ScriptableItem itemData))
                {
                    Item item = new Item(itemData);
                    //item.durability = Mathf.Min(row.durability, item.maxDurability);
                    //item.summonedHealth = row.summonedHealth;
                    //item.summonedLevel = row.summonedLevel;
                    //item.summonedExperience = row.summonedExperience;
                    equipment.slots[row.slot] = new ItemSlot(item, row.amount);
                }
                else Debug.LogWarning("LoadEquipment: skipped item " + row.name + " for " + equipment.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
            }
            else Debug.LogWarning("LoadEquipment: skipped slot " + row.slot + " for " + equipment.name + " because it's bigger than size " + equipment.slotInfo.Length);
        }
    }

    void LoadItemCooldowns(Player player)
    {
        // then load cooldowns
        // (one big query is A LOT faster than querying each slot separately)
        foreach (character_itemcooldowns row in connection.Query<character_itemcooldowns>("SELECT * FROM character_itemcooldowns WHERE characterId=?", player.id))
        {
            // cooldownEnd is based on NetworkTime.time which will be different
            // when restarting a server, hence why we saved it as just the
            // remaining time. so let's convert it back again.
            player.itemCooldowns.Add(row.category, row.cooldownEnd + NetworkTime.time);
        }
    }

    public string GetAccountUsername(string account)
    {
        accounts acc = connection.FindWithQuery<accounts>("SELECT * FROM accounts WHERE accountname=? ", account);
        if (acc != null)
            return acc.username;
        else
            return "*No UserName Found*";
    }

    public int GetNewCharacterId()
    {
        int count = connection.ExecuteScalar<int>("SELECT COUNT(id) FROM characters");
        return count + 1;
    }

    public GameObject CharacterLoad(int characterId, List<Player> prefabs, bool isPreview)
    {
        characters row = connection.FindWithQuery<characters>("SELECT * FROM characters WHERE id=? AND deleted=0", characterId);
        if (row != null)
        {
            // instantiate based on the class name
            Player prefab = prefabs.Find(p => p.name == row.classname);
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab.gameObject);
                Player player = go.GetComponent<Player>();

                player.id = row.id;
                player.account = row.account;
                player.name = row.name;
                player.className = row.classname;
                player.transform.position = new Vector2(row.x, row.y);
                player.createdTime = row.createdTime;

                LoadInventory(player.inventory);
                LoadEquipment((PlayerEquipment)player.equipment);
                LoadItemCooldowns(player);

                // assign health / mana after max values were fully loaded
                // (they depend on equipment, buffs, etc.)       
                player.health.current = row.health;
                player.playerWater.waterAmount = row.waterAmount;

                // set 'online' directly. otherwise it would only be set during
                // the next CharacterSave() call, which might take 5-10 minutes.
                // => don't set it when loading previews though. only when
                //    really joining the world (hence setOnline flag)
                if (!isPreview)
                    connection.Execute("UPDATE characters SET online=1, lastsaved=? WHERE id=?", DateTime.UtcNow, characterId);

                // addon system hooks
                //Utils.InvokeMany(typeof(Database), this, "CharacterLoad_", player);

                return go;
            }
            else Debug.LogError("no prefab found for class: " + row.classname);
        }
        return null;
    }

    void SaveInventory(PlayerInventory inventory)
    {
        // inventory: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        connection.Execute("DELETE FROM character_inventory WHERE characterId=?", inventory.player.id);
        for (int i = 0; i < inventory.slots.Count; ++i)
        {
            ItemSlot slot = inventory.slots[i];
            if (slot.amount > 0) // only relevant items to save queries/storage/time
            {
                // note: .Insert causes a 'Constraint' exception. use Replace.
                connection.InsertOrReplace(new character_inventory
                {
                    characterId = inventory.player.id,
                    slot = i,
                    name = slot.item.name,
                    amount = slot.amount,
                    //durability = slot.item.durability,
                    //summonedHealth = slot.item.summonedHealth,
                    //summonedLevel = slot.item.summonedLevel,
                    //summonedExperience = slot.item.summonedExperience
                });
            }
        }
    }

    void SaveEquipment(PlayerEquipment equipment)
    {
        // equipment: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        connection.Execute("DELETE FROM character_equipment WHERE characterId=?", equipment.player.id);
        for (int i = 0; i < equipment.slots.Count; ++i)
        {
            ItemSlot slot = equipment.slots[i];
            if (slot.amount > 0) // only relevant equip to save queries/storage/time
            {
                connection.InsertOrReplace(new character_equipment
                {
                    characterId = equipment.player.id,
                    slot = i,
                    name = slot.item.name,
                    amount = slot.amount,
                    //durability = slot.item.durability,
                    //summonedHealth = slot.item.summonedHealth,
                    //summonedLevel = slot.item.summonedLevel,
                    //summonedExperience = slot.item.summonedExperience
                });
            }
        }
    }

    void SaveItemCooldowns(Player player)
    {
        // equipment: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        connection.Execute("DELETE FROM character_itemcooldowns WHERE characterId=?", player.id);
        foreach (KeyValuePair<string, double> kvp in player.itemCooldowns)
        {
            // cooldownEnd is based on NetworkTime.time, which will be different
            // when restarting the server, so let's convert it to the remaining
            // time for easier save & load
            // note: this does NOT work when trying to save character data
            //       shortly before closing the editor or game because
            //       NetworkTime.time is 0 then.
            float cooldown = player.GetItemCooldown(kvp.Key);
            if (cooldown > 0)
            {
                connection.InsertOrReplace(new character_itemcooldowns
                {
                    characterId = player.id,
                    category = kvp.Key,
                    cooldownEnd = cooldown
                });
            }
        }
    }

    //Give an online player items
    public void AddItemToCharacter(string playerName, int amount, string itemName)
    {
        if (amount <= 0)
        {
            PrintAndAdminDebug("Add item failed. Amount should be greater than one.");
            return;
        }

        int itemHash = itemName.GetStableHashCode();
        if (!ScriptableItem.All.ContainsKey(itemHash))
        {
            PrintAndAdminDebug("Add item failed. There is no ScriptableItem with name=" + itemName+ ".");
            return;
        }

        Item item;
        item.hash = itemHash;

        Player p;
        if (!Player.onlinePlayers.TryGetValue(playerName, out p))
        {
            PrintAndAdminDebug("Add item failed. Cannot find player " + playerName+ ".");
            return;
        }

        if (p.inventory.Add(item, amount))
        {
            CharacterSave(p, false);
            PrintAndAdminDebug("Add "+ playerName+ " " + amount + " " + itemName+" successfully.");
        }
        else
        {
            PrintAndAdminDebug("Inventory add failed: " + item.name + " " + amount);
        }
    }

    //Remove an online player items
    public void RemoveItemFromCharacter(string playerName, int amount, string itemName)
    {
        if (amount <= 0)
        {
            PrintAndAdminDebug("Remove item failed. Amount should be greater than one.");
            return;
        }

        int itemHash = itemName.GetStableHashCode();
        if (!ScriptableItem.All.ContainsKey(itemHash))
        {
            PrintAndAdminDebug("Remove item failed. There is no ScriptableItem with name=" + itemName+ ".");
            return;
        }

        Item item;
        item.hash = itemHash;

        Player p;
        if (!Player.onlinePlayers.TryGetValue(playerName, out p))
        {
            PrintAndAdminDebug("Remove item failed. Cannot find player " + playerName+ ".");
            return;
        }

        if (p.inventory.Remove(item, amount))
        {
            CharacterSave(p, false);
            PrintAndAdminDebug("Remove "+ playerName+ " " + amount + " " + itemName+" successfully.");
        }
        else
        {
            CharacterSave(p, false);
            PrintAndAdminDebug("Inventory removed : " + item.name + " " + amount + " Not enough amounts of item are removed.");
        }
    }

    // adds or overwrites character data in the database
    public void CharacterSave(Player player, bool online, bool useTransaction = true)
    {
        // only use a transaction if not called within SaveMany transaction
        if (useTransaction) connection.BeginTransaction();

        connection.InsertOrReplace(new characters
        {
            id = player.id,
            account = player.account,
            name = player.name,
            classname = player.className,
            x = player.transform.position.x,
            y = player.transform.position.y,
            health = player.health.current,
            waterAmount = player.playerWater.waterAmount,
            /*
            level = player.level,
            health = player.health,
            mana = player.mana,
            strength = player.strength,
            intelligence = player.intelligence,
            experience = player.experience,
            skillExperience = player.skillExperience,
            gold = player.gold,
            coins = player.coins,
            */
            createdTime = player.createdTime,
            online = online,
            lastsaved = DateTime.UtcNow
        });

        SaveInventory(player.inventory);
        SaveItemCooldowns(player);
        SaveEquipment((PlayerEquipment)player.equipment);
        //SaveSkills(player);
        //SaveBuffs(player);
        //SaveQuests(player);
        //if (player.InGuild()) SaveGuild(player.guild, false); // TODO only if needs saving? but would be complicated

        // addon system hooks
        //Utils.InvokeMany(typeof(Database), this, "CharacterSave_", player);

        if (useTransaction) connection.Commit();
    }

    // save multiple characters at once (useful for ultra fast transactions)
    public void CharacterSaveMany(IEnumerable<Player> players, bool online = true)
    {
        connection.BeginTransaction(); // transaction for performance
        foreach (Player player in players)
            CharacterSave(player, online, false);
        connection.Commit(); // end transaction
    }

    public void LoadEnvironmentData()//call by NetworkManager when serverStarted
    {
        LoadWaterData();
        LoadFountain();
        LoadRainArea();
        LoadBush();
    }

    public void SaveEnvironmentData()//call by NetworkManager when serverStarted
    {
        SaveWaterData();
        SaveFountain();
        SaveRainArea();
        SaveBush();
    }

    public void LoadWaterData()
    {
        EnvironmentManager em = EnvironmentManager.singleton;

        if (connection.ExecuteScalar<int>("SELECT COUNT(id) FROM Envir_WaterData") < 1)
        {
            //初始值
            connection.InsertOrReplace(new Envir_WaterData
            {
                id = 0,
                groundWater = 1000,
                groundWater_Pure = 0,
                airWater_Pure = 0,
                //groundWater_Polluted = 0,
                //airWater = 0,
                //airWater_Polluted = 0,
                //lastsaved = DateTime.UtcNow
            });
        }

        Envir_WaterData envir_WaterData = connection.FindWithQuery<Envir_WaterData>("SELECT * FROM Envir_WaterData WHERE id=?", 0);
        em.groundWater = envir_WaterData.groundWater;
        em.groundWater_Pure = envir_WaterData.groundWater_Pure;
        em.airWater_Pure = envir_WaterData.airWater_Pure;
    }

    public void SaveWaterData()
    {
        EnvironmentManager em = EnvironmentManager.singleton;

        connection.InsertOrReplace(new Envir_WaterData
        {
            id = 0,
            groundWater = em.groundWater,
            groundWater_Pure = em.groundWater_Pure,
            airWater_Pure = em.airWater_Pure,
            lastsaved = DateTime.UtcNow
        });

    }

    public void LoadFountain()
    {
        EnvironmentManager em = EnvironmentManager.singleton;

        foreach (Envir_Fountain row in connection.Query<Envir_Fountain>("SELECT * FROM Envir_Fountain"))
        {
            GameObject NewFountain = Instantiate(em.fountainPref);
            Fountain fountain = NewFountain.GetComponent<Fountain>();
            fountain.id = row.id;
            NewFountain.transform.position = new Vector2(row.position_X, row.position_Y);
            fountain.builder = row.builder;
            fountain.builtTime = row.builtTime;
            fountain.totalusedTimes = row.totalusedTimes;
            fountain.totalDrankAmount = row.totalDrankAmount;
            em.SpawnFountain(NewFountain);
        }
    }

    public void SaveFountain()
    {
        EnvironmentManager em = EnvironmentManager.singleton;
        //make sure no ghost
        connection.Execute("DELETE FROM Envir_Fountain");

        foreach (Fountain row in em.fountainList)
        {
            connection.InsertOrReplace(new Envir_Fountain
            {
                id = row.id,
                position_X = row.transform.position.x,
                position_Y = row.transform.position.y,
                builder = row.builder,
                builtTime = row.builtTime,
                totalusedTimes = row.totalusedTimes,
                totalDrankAmount = row.totalDrankAmount,
                lastsaved = DateTime.UtcNow
            });
        }
    }

    public void LoadRainArea()
    {
        EnvironmentManager em = EnvironmentManager.singleton;

        foreach (Envir_RainArea row in connection.Query<Envir_RainArea>("SELECT * FROM Envir_RainArea"))
        {
            GameObject NewRainArea = Instantiate(em.RainAreaPref);
            RainArea rainArea = NewRainArea.GetComponent<RainArea>();
            rainArea.id = row.id;
            rainArea.RainFinishedTime = row.RainFinishedTime;
            NewRainArea.transform.position = new Vector2(row.position_X, row.position_Y);
            em.SpawnRainArea(NewRainArea);
        }
    }

    public void SaveRainArea()
    {
        EnvironmentManager em = EnvironmentManager.singleton;
        //make sure no ghost
        connection.Execute("DELETE FROM Envir_RainArea");

        foreach (RainArea row in em.RainAreaList)
        {
            connection.InsertOrReplace(new Envir_RainArea
            {
                id = row.id,
                position_X = row.transform.position.x,
                position_Y = row.transform.position.y,
                RainFinishedTime = row.RainFinishedTime,
                lastsaved = DateTime.UtcNow
            });
        }
    }

    public void LoadBush()
    {
        foreach (var row in connection.Query<Envir_Bush>("SELECT * FROM Envir_Bush"))
        {
            var bushGO = Instantiate(EnvironmentManager.singleton.bushPref);
            var bush = bushGO.GetComponent<Bush>();
            bush.id = row.id;
            bush.transform.position = new Vector2(row.positionX, row.positionY);
            bush.RemainingAmount = row.remaningAmount;
            bush.RefreshEnd = NetworkTime.time + row.refreshTime;
            EnvironmentManager.singleton.SpawnBush(bushGO);
        }
    }

    public void SaveBush()
    {
        connection.Execute("DELETE FROM Envir_Bush");

        foreach (var bush in EnvironmentManager.singleton.bushList)
        {
            connection.InsertOrReplace(new Envir_Bush
            {
                id = bush.id,
                positionX = bush.transform.position.x,
                positionY = bush.transform.position.y,
                remaningAmount = bush.RemainingAmount,
                refreshTime = NetworkTime.time >= bush.RefreshEnd ? 0 : (float)(bush.RefreshEnd - NetworkTime.time)
            });
        }
    }

    public void PrintAndAdminDebug(string text)
    {
        print(text);
        if(Player.localPlayer.chat)
            Player.localPlayer.chat.AddMsgInfo(text);
    }

}