using Mirror;
using UnityEngine;

public class Bush : InteractableArea
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite _withFruit;
    [SerializeField] private Sprite _withoutFruit;

    [SerializeField] private ScriptableItem _fruit;
    [SerializeField] private double _refreshInterval = 300;
    [SerializeField] private float _checkInterval = 30;

    public int id;
    [SyncVar(hook = nameof(UpdateSprite))]
    public int RemainingAmount;
    [SyncVar]
    public double RefreshEnd;

    public override void OnStartServer() => InvokeRepeating(nameof(CheckRefresh), 0, _checkInterval);

    public override void OnStartClient() => _renderer.sprite = RemainingAmount > 0 ? _withFruit : _withoutFruit;

    protected override bool CanInteract(Player player)
    {
        if (player == null)
            return false;

        if (!player.playerGathering.CanGather)
            return false;

        if (RemainingAmount <= 0)
            return false;

        return true;
    }

    [Server]
    protected override void DoAction(Player player)
    {
        var fruit = new Item(_fruit);

        if (player.playerGathering.TryGather(fruit, 1))
        {
            player.playerGathering.Gather(fruit, 1);
            RemainingAmount--;
            RefreshEnd = NetworkTime.time + _refreshInterval;
        }
    }

    [Server]
    private void CheckRefresh()
    {
        if (NetworkTime.time >= RefreshEnd)
            RemainingAmount = 10;
    }

    private void UpdateSprite(int oldAmount, int newAmount) => _renderer.sprite = newAmount > 0 ? _withFruit : _withoutFruit;
}
