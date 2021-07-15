using Mirror;
using System.Collections;
using UnityEngine;

public class RandomizePosition : NetworkBehaviour
{
    [SerializeField]
    private Transform _child;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.hasAuthority)
            StartCoroutine(__RandomizePosition());
    }
    
    private IEnumerator __RandomizePosition()
    {
        WaitForSeconds wait = new WaitForSeconds(3f);
        Vector3 startPosition = transform.position;

        while (true)
        {
            transform.position = startPosition + ReturnRandom();
            _child.localPosition = ReturnRandom(0.2f);
            yield return wait;
        }
    }

    private Vector3 ReturnRandom(float multiplier = 1f)
    {
        return new Vector3(
            Random.Range(-6f, 6f),
            Random.Range(-6f, 6f),
            0f) * multiplier;
    }
}
