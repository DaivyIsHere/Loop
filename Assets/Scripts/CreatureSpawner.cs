using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CreatureSpawner : NetworkBehaviour
{
    public GameObject creaturePref;
    public ScriptableCreature scriptableCreature;

    [Server]
    public void SpawnCreature()
    {
        Vector2 offset = new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f));
        GameObject creature = Instantiate(creaturePref, transform.position + (Vector3)offset, Quaternion.identity);
        NetworkServer.Spawn(creature);
    }
}
