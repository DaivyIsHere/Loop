using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Spawner : NetworkBehaviour
{
    public static Spawner singleton;//be sure only run on server

    public GameObject enemyPref;
    public GameObject blackScreenPref;
    public GameObject fireworkPref;

    public List<GameObject> blackScreenList;

    public override void OnStartServer()
    {
        if (singleton == null)
            singleton = this;
    }

    public void SpawnEnemy(Vector3 pos)
    {
        Vector2 offset = new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f));
        GameObject enemy = Instantiate(enemyPref, pos + (Vector3)offset, Quaternion.identity);
        NetworkServer.Spawn(enemy);
    }

    public void SpawnBlackScreen()
    {
        GameObject blackScreen = Instantiate(blackScreenPref, Vector3.zero, Quaternion.identity);
        NetworkServer.Spawn(blackScreen);
        blackScreenList.Add(blackScreen);
    }

    public void Day()
    {
        for (int i = 0; i < blackScreenList.Count; i++)
        {
            NetworkServer.Destroy(blackScreenList[i]);
        }
    }

    public void SpawnFirework(Vector3 pos)
    {
        GameObject firework = Instantiate(fireworkPref, pos, Quaternion.identity);
        NetworkServer.Spawn(firework);
        StartCoroutine(NetworkDestroy(firework,5f));
    }

    IEnumerator NetworkDestroy(GameObject gameObject, float afterTime)
    {
        yield return new WaitForSeconds(afterTime);
        NetworkServer.Destroy(gameObject);
        yield return 0;
    }
}
