using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITestAccount : MonoBehaviour
{
    public UILogin uiLogin;
    public GameObject panel;

    void Update() 
    {
        panel.SetActive(uiLogin.panel.activeSelf);
    }

    public void SetAdmin()
    {
        uiLogin.accountInput.text = "admin";
        uiLogin.passwordInput.text = "admin";
    }

    public void SetDd()
    {
        uiLogin.accountInput.text = "Dd";
        uiLogin.passwordInput.text = "dd";
    }

    public void SetAa()
    {
        uiLogin.accountInput.text = "Aa";
        uiLogin.passwordInput.text = "aa";
    }
}
