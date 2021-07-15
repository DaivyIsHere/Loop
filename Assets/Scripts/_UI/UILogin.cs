// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UILogin : MonoBehaviour
{
    public UIPopup uiPopup;
    public NetworkManagerMMO manager; // singleton=null in Start/Awake
    public NetworkAuthenticatorMMO auth;
    public GameObject panel;
    public Text statusText;
    public InputField accountInput;
    public InputField passwordInput;
    public Dropdown serverDropdown;
    public Button loginButton;
    public Button registerButton;
    //[TextArea(1, 30)] public string registerMessage = "First time? Just log in and we will\ncreate an account automatically.";
    public Button hostButton;
    public Button dedicatedButton;
    public Button cancelButton;
    public Button quitButton;

    public GameObject RegisterPanel;
    public InputField newAccountInput;
    public InputField newPasswordInput;
    public InputField newUsernameInput;
    public Button CreateAccountButton;
    public Text registerHint;

    void Start()
    {
        /*
        #if UNITY_EDITOR
        accountInput.text = "admin";
        passwordInput.text = "admin";
        #endif
        */

        // load last server by name in case order changes some day.
        if (PlayerPrefs.HasKey("LastServer"))
        {
            string last = PlayerPrefs.GetString("LastServer", "");
            serverDropdown.value = manager.serverList.FindIndex(s => s.name == last);
        }
        if(PlayerPrefs.HasKey("LastAccount"))
        {
            accountInput.text = PlayerPrefs.GetString("LastAccount", "");
        }
        if(PlayerPrefs.HasKey("LastPassword"))
        {
            passwordInput.text = PlayerPrefs.GetString("LastPassword", "");
        }
    }

    void OnDestroy()
    {
        // save last server by name in case order changes some day
        PlayerPrefs.SetString("LastServer", serverDropdown.captionText.text);
        PlayerPrefs.SetString("LastAccount",accountInput.text);
        PlayerPrefs.SetString("LastPassword",passwordInput.text);
    }

    void Update()
    {
        // only show while offline
        // AND while in handshake since we don't want to show nothing while
        // trying to login and waiting for the server's response
        if (manager.state == NetworkState.Offline || manager.state == NetworkState.Handshake)
        {
            panel.SetActive(true);

            // status
            if (manager.IsConnecting())
                statusText.text = "Connecting...";
            else if (manager.state == NetworkState.Handshake)
                statusText.text = "Handshake...";
            else
                statusText.text = "";

            // buttons. interactable while network is not active
            // (using IsConnecting is slightly delayed and would allow multiple clicks)
            registerButton.interactable = !manager.isNetworkActive;
            //registerButton.onClick.SetListener(() => { uiPopup.Show(registerMessage); });
            registerButton.onClick.SetListener(() => { RegisterPanel.SetActive(!RegisterPanel.activeSelf); });
            loginButton.interactable = !manager.isNetworkActive && auth.IsAllowedAccountName(accountInput.text);
            loginButton.onClick.SetListener(() => { auth.RegisterClient(false); });//原本為manager.StartClient();，現在改為auth.StartClient(bool);以判斷是否為登入或註冊
            hostButton.interactable = Application.platform != RuntimePlatform.WebGLPlayer && !manager.isNetworkActive && auth.IsAllowedAccountName(accountInput.text);
            hostButton.onClick.SetListener(() => { auth.HostClient(); });//原本為manager.StartHost();，現在改為auth.StartHost(bool);以更新isRegister
            cancelButton.gameObject.SetActive(manager.IsConnecting());
            cancelButton.onClick.SetListener(() => { manager.StopClient(); });
            dedicatedButton.interactable = Application.platform != RuntimePlatform.WebGLPlayer && !manager.isNetworkActive;
            dedicatedButton.onClick.SetListener(() => { manager.StartServer(); });
            quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });

            //Register
            auth.registerAccount = newAccountInput.text;
            auth.registerPassword = newPasswordInput.text;
            auth.registerUsername = newUsernameInput.text;
            CreateAccountButton.interactable = !manager.isNetworkActive && auth.IsAllowedAccountName(newAccountInput.text) && auth.IsAllowedAccountName(newUsernameInput.text);
            CreateAccountButton.onClick.SetListener(() => { auth.RegisterClient(true); });

            // inputs
            auth.loginAccount = accountInput.text;
            auth.loginPassword = passwordInput.text;

            // copy servers to dropdown; copy selected one to networkmanager ip/port.
            serverDropdown.interactable = !manager.isNetworkActive;
            serverDropdown.options = manager.serverList.Select(
                sv => new Dropdown.OptionData(sv.name)
            ).ToList();
            manager.networkAddress = manager.serverList[serverDropdown.value].ip;
        }
        else panel.SetActive(false);
    }
}
