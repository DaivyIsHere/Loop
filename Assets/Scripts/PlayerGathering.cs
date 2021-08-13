using Mirror;
using UnityEngine;

public class PlayerGathering : NetworkBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private double _cooldownTime = 1;

    public bool CanGather => NetworkTime.time >= _cooldownEnd;

    private double _cooldownEnd;

    public bool TryGather(Item item, int amount)
    {
        _cooldownEnd = NetworkTime.time + _cooldownTime;
        return _player.inventory.CanAdd(item, amount);
    }

    public void Gather(Item item, int amount)
    {
        _player.inventory.Add(item, amount);
    }
}
