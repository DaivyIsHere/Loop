using Mirror;
using UnityEngine;

public class Bush : InteractableArea
{
    //拉進Scriptable Oject創建的葉子
    [SerializeField] private ScriptableItem _leaf;
    //多久刷新可採集數量(秒)
    [SerializeField] private double _refreshInterval = 300;

    public int id;
    //剩餘可採集的數量，用SyncVar來同步(?)
    [SyncVar]
    public int RemaingingAmount;
    //下次刷新可採集數量的時間，用SyncVar來同步(?)
    [SyncVar]
    public double RefreshEnd;

    protected override void Update()
    {
        //看是否刷新可採集數量
        //↓或用ServerCallback應該也是一樣的效果(?)
        //CheckRefreshTime();
        if (isServer)
            if (NetworkTime.time >= RefreshEnd)
                RemaingingAmount = 10;

        //符合條件即執行DoAction()
        if (isClient && InRange && CanInteract() && Input.GetKeyDown(KeyCode.E))
            DoAction();

        base.Update();
    }

    protected override bool CanInteract()
    {
        if (Player.localPlayer == null)
            return false;

        //若玩家採集還在冷卻就return false
        if (!Player.localPlayer.playerGathering.CanGather)
            return false;

        //若可採集數量歸零就return false
        if (RemaingingAmount <= 0)
            return false;

        return true;
    }

    protected override void DoAction()
    {
        //創建要加入PlayerInventory的Item，並在constructor中傳入ScriptableItem _leaf
        var leaf = new Item(_leaf);

        //檢查玩家的inventory是否可以加入1片葉子
        if (Player.localPlayer.playerGathering.TryGather(leaf, 1))
        {
            //若可以就利用透過Command加入1片葉子
            Player.localPlayer.playerGathering.CmdGather(leaf, 1);
            //可採集數量-1
            RemaingingAmount--;
            //更新下次可採集數量刷新的時間
            RefreshEnd = NetworkTime.time + _refreshInterval;
        }
    }

    //[ServerCallback]
    //private void CheckRefreshTime()
    //{
    //    if (NetworkTime.time >= _refreshTime)
    //        _remainingAmount = 10;
    //}
}
