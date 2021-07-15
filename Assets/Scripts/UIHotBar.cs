using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHotBar : MonoBehaviour
{
    public GameObject panel;

    public Button inventoryButton;
    public GameObject inventoryPanel;

    public Button equipmentButton;
    public GameObject equipmentPanel;

    void Update() 
    {
        Player player = Player.localPlayer;

        if (player)
        {
            panel.SetActive(true);

            inventoryButton.onClick.SetListener(() => {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            });

            equipmentButton.onClick.SetListener(() => {
                equipmentPanel.SetActive(!equipmentPanel.activeSelf);
            });    
        }
    }
}
