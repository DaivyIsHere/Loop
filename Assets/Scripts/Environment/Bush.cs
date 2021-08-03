using Mirror;
using UnityEngine;

public class Bush : InteractableArea
{
    [SerializeField] private ScriptableItem _leaf;
    [SerializeField] private double _refreshInterval = 300;
    [SerializeField] private float _checkInterval = 30;

    public int id;
    //[SyncVar]
    public int RemaingingAmount;
    //[SyncVar]
    public double RefreshEnd;

    public override void OnStartServer() => InvokeRepeating(nameof(CheckRefresh), 0, _checkInterval);

    [Server]
    protected override bool CanInteract(NetworkIdentity netIdentity)
    {
        //if (Player.localPlayer == null || !Player.localPlayer.playerGathering.CanGather || RemaingingAmount <= 0)
        //    return false;

        if (netIdentity == null)
            return false;

        if (!netIdentity.GetComponent<PlayerGathering>().CanGather)
            return false;

        if (RemaingingAmount <= 0)
            return false;

        return true;
    }

    [Server]
    protected override void DoAction(NetworkIdentity netIdentity)
    {
        var leaf = new Item(_leaf);

        //if (Player.localPlayer.playerGathering.TryGather(leaf, 1))
        //{
        //    Player.localPlayer.playerGathering.CmdGather(leaf, 1);
        //    CmdUpdateBush();
        //}

        if (netIdentity.GetComponent<PlayerGathering>().TryGather(leaf, 1))
        {
            netIdentity.GetComponent<PlayerGathering>().Gather(leaf, 1);
            RemaingingAmount--;
            RefreshEnd = NetworkTime.time + _refreshInterval;
        }
    }

    [Server]
    private void CheckRefresh()
    {
        if (NetworkTime.time >= RefreshEnd)
            RemaingingAmount = 10;
    }

    //[Command(ignoreAuthority = true)]
    //private void CmdUpdateBush()
    //{
    //    RemaingingAmount--;
    //    RefreshEnd = NetworkTime.time + _refreshInterval;
    //}
}
