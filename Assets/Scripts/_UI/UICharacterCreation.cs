﻿using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UICharacterCreation : MonoBehaviour
{
    public NetworkManagerMMO manager; // singleton is null until update
    public GameObject panel;
    public InputField nameInput;
    public Dropdown classDropdown;
    public Button createButton;
    public Button cancelButton;

    void Update()
    {
        // only update while visible (after character selection made it visible)
        if (panel.activeSelf)
        {
            // still in lobby?
            if (manager.state == NetworkState.Lobby)
            {
                Show();

                // copy player classes to class selection
                classDropdown.options = manager.playerClasses.Select(
                    p => new Dropdown.OptionData(p.name)
                ).ToList();

                // create
                createButton.interactable = true;//manager.IsAllowedCharacterName(nameInput.text);
                createButton.onClick.SetListener(() => {
                    CharacterCreateMsg message = new CharacterCreateMsg{
                        //id = nameInput.text.ToInt(),//????????????????????????改為INT，原為string
                        classIndex = classDropdown.value
                    };
                    NetworkClient.Send(message);
                    Hide();
                });

                // cancel
                cancelButton.onClick.SetListener(() => {
                    nameInput.text = "";
                    Hide();
                });
            }
            else Hide();
        }
        else Hide();
    }

    public void Hide() { panel.SetActive(false); }
    public void Show() { panel.SetActive(true); }
    public bool IsVisible() { return panel.activeSelf; }
}
