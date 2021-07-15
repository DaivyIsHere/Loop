// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using UnityEngine;
using UnityEngine.UI;

public class UILoot : MonoBehaviour
{
    public static UILoot singleton;
    public GameObject panel;
    //public GameObject cointSlot;
    //public Text goldText;
    public UILootSlot itemSlotPrefab;
    public Transform content;

    public UILoot()
    {
        // assign singleton only once (to work with DontDestroyOnLoad when
        // using Zones / switching scenes)
        if (singleton == null) singleton = this;
    }

    void Update() 
    {
        Player player = Player.localPlayer;

        if (player != null &&
            panel.activeSelf &&
            player.playerLooting.CanLoot())
        {
            LootBag loot = player.playerLooting.currentloot;
            // IMPORTANT: when showing slots, we never filter out the empty ones.
            // the slot position should never change, otherwise party members
            // might accidentally click on the same slot at the same time, and
            // then the first person gets the item, the second person gets the
            // other item that he didn't click on because it was moved up.
            // => simply don't ever modify slot positions / indices!

            // instantiate/destroy enough slots
            // (we only want to show the non-empty slots)
            UIUtils.BalancePrefabs(itemSlotPrefab.gameObject, loot.slots.Count, content);

            // refresh all valid items
            for (int i = 0; i < loot.slots.Count; ++i)
            {
                ItemSlot itemSlot = loot.slots[i];

                UILootSlot slot = content.GetChild(i).GetComponent<UILootSlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index

                if (itemSlot.amount > 0)
                {
                    // refresh valid item
                    slot.button.interactable = player.inventory.CanAdd(itemSlot.item, itemSlot.amount);
                    int icopy = i;
                    slot.button.onClick.SetListener(() => {
                        player.playerLooting.CmdTakeItem(icopy);
                    });
                    // only build tooltip while it's actually shown. this
                    // avoids MASSIVE amounts of StringBuilder allocations.
                    slot.tooltip.enabled = true;
                    if (slot.tooltip.IsVisible())
                        slot.tooltip.text = itemSlot.ToolTip();
                    slot.image.color = Color.white;
                    slot.image.sprite = itemSlot.item.image;
                    //slot.nameText.text = itemSlot.item.name;
                    slot.amountOverlay.SetActive(itemSlot.amount > 1);
                    slot.amountText.text = itemSlot.amount.ToString();
                }
                else
                {
                    // refresh invalid item
                    slot.button.interactable = false;
                    slot.button.onClick.RemoveAllListeners();
                    slot.tooltip.enabled = false;
                    slot.tooltip.text = "";
                    slot.dragAndDropable.dragable = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    //slot.nameText.text = "";
                    slot.amountOverlay.SetActive(false);
                }
            }
        }
        else panel.SetActive(false);
    }

    public void Show() 
    {
        panel.SetActive(true); 
    }
}
