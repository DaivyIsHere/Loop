using Mirror;
using UnityEngine;

public class PlayerGathering : NetworkBehaviour
{
    [SerializeField] private Player _player;

    //根據玩家的採集冷卻時間回傳玩家是否可以採集
    public bool CanGather => NetworkTime.time >= _cooldownEnd;

    //採集冷卻結束的時間
    private double _cooldownEnd;

    //或許PlayerInventory滿了之類的而不能採集
    public bool TryGather(Item item, int amount)
    {
        //嘗試採集或許也需要冷卻(?)
        _cooldownEnd = NetworkTime.time + 1;

        //利用Inventory裡的CanAdd來檢查是否可以新增物件到PlayerInventory
        return _player.inventory.CanAdd(item, amount);
    }

    [Command]
    public void CmdGather(Item item, int amount)
    {
        //採集成功就設定下次可採集的時間
        _cooldownEnd = NetworkTime.time + 1;

        //利用Inventory裡的Add來新增物件到PlayerInventory
        _player.inventory.Add(item, amount);
    }
}
