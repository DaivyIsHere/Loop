using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

// inventory, attributes etc. can influence max health
public interface ICombatBonus
{
    int GetDamageBonus();
    int GetDefenseBonus();
}

[DisallowMultipleComponent]
public class Combat : NetworkBehaviour
{
    [Header("Components")]
    public Level level;
    public Entity entity;
    public new Collider2D collider;

    [Header("Overlay")]///傷害顯示
    public GameObject OverlayPosition;
    public Vector3 OverlayOffset = new Vector3(0, 1, 0);
    public GameObject AmountOverlayPrefab;
    public Color damageTextColor = new Color32(205, 60, 53, 255);

    [Header("Stats")]
    [SyncVar] public bool invincible = false; // GMs, Npcs, ...
    public LinearInt baseDamage = new LinearInt { baseValue = 1 };
    public LinearInt baseDefense = new LinearInt { baseValue = 1 };

    public int damage
    {
        get
        {
            return baseDamage.Get(level.current);
        }
    }

    public int defense
    {
        get
        {
            return baseDefense.Get(level.current);
        }
    }

    // no need to instantiate damage popups on the server
    // -> calculating the position on the client saves server computations and
    //    takes less bandwidth (4 instead of 12 byte)
    [Client]
    public void ShowDamagePopup(int damage)
    {
        //popup
        string text;
        if (damage > 0)
            text = "-" + damage;
        else
            text = "";
        GameObject overlay = Instantiate(AmountOverlayPrefab, transform.position + OverlayOffset, Quaternion.identity, OverlayPosition.transform);
        overlay.GetComponentInChildren<TextMesh>().color = damageTextColor;
        overlay.GetComponentInChildren<TextMesh>().text = text;
    }

    /*
    [ClientRpc]
    void RpcOnDamageReceived(int amount, DamageType damageType)
    {
        // show popup above receiver's head in all observers via ClientRpc
        ShowDamagePopup(amount, damageType);
    }*/
}
