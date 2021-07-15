using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class UIChat : MonoBehaviour
{
    public static UIChat singleton;
    public GameObject panel;
    public InputField messageInput;
    public Button sendButton;
    public Transform content;
    public ScrollRect scrollRect;
    public KeyCode[] activationKeys = {KeyCode.Return, KeyCode.KeypadEnter};
    public int keepHistory = 100; // only keep 'n' messages

    public string lastInput = "";//重複上一次輸入，未完成

    bool eatActivation;

    [Header("History")]
    public KeyCode lastInputSwitchKey = KeyCode.PageUp;
    public KeyCode nextInputSwitchKey = KeyCode.PageDown;
    public List<string> inputHistory = new List<string>();
    public int maxHistoryCount = 30;
    int currentHistoryIndex = 0;

    public UIChat()
    {
        // assign singleton only once (to work with DontDestroyOnLoad when
        // using Zones / switching scenes)
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            //messageInput.gameObject.SetActive(true);
            //sendButton.gameObject.SetActive(true);
            panel.SetActive(true);
            //新功能，輸入文字時會放大視窗，關閉時會縮小
            sendButton.interactable = messageInput.text != "";
            if(EventSystem.current.currentSelectedGameObject == messageInput.gameObject)
                panel.GetComponent<RectTransform>().sizeDelta = new Vector2(290,350);
            else
                panel.GetComponent<RectTransform>().sizeDelta = new Vector2(290,150);

            // character limit
            PlayerChat chat = player.GetComponent<PlayerChat>();
            messageInput.characterLimit = chat.maxLength;

            // activation (ignored once after deselecting, so it doesn't immediately
            // activate again)
            if (Utils.AnyKeyDown(activationKeys) && !eatActivation)
                messageInput.Select();
            eatActivation = false;

            //switchHistory
            if(messageInput.isFocused)
            {
                if(Input.GetKeyDown(lastInputSwitchKey))
                {
                    if(currentHistoryIndex >= 1)
                    {
                        currentHistoryIndex -= 1;
                        messageInput.text = inputHistory[currentHistoryIndex];
                    }
                }
                else if(Input.GetKeyDown(nextInputSwitchKey))
                {
                    if(currentHistoryIndex <= inputHistory.Count -2)
                    {
                        currentHistoryIndex += 1;
                        messageInput.text = inputHistory[currentHistoryIndex];
                    }
                }
            }


            // end edit listener
            messageInput.onEndEdit.SetListener((value) =>
            {
                // submit key pressed? then submit and set new input text
                if (Utils.AnyKeyDown(activationKeys))
                {
                    AddToInputHistory(messageInput.text);
                    string newinput = chat.OnSubmit(value);
                    messageInput.text = newinput;
                    messageInput.MoveTextEnd(false);
                    eatActivation = true;
                }

                // unfocus the whole chat in any case. otherwise we would scroll or
                // activate the chat window when doing wsad movement afterwards
                UIUtils.DeselectCarefully();
            });

            // send button
            sendButton.onClick.SetListener(() =>
            {
                AddToInputHistory(messageInput.text);
                // submit and set new input text
                string newinput = chat.OnSubmit(messageInput.text);
                messageInput.text = newinput;
                messageInput.MoveTextEnd(false);

                // unfocus the whole chat in any case. otherwise we would scroll or
                // activate the chat window when doing wsad movement afterwards
                UIUtils.DeselectCarefully();
            });
        }
        else panel.SetActive(false);
    }

    void AutoScroll()
    {
        // update first so we don't ignore recently added messages, then scroll
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }

    public void AddMessage(ChatMessage message)
    {
        // delete old messages so the UI doesn't eat too much performance.
        // => every Destroy call causes a lag because of a UI rebuild
        // => it's best to destroy a lot of messages at once so we don't
        //    experience that lag after every new chat message
        if (content.childCount >= keepHistory)
        {
            for (int i = 0; i < content.childCount / 2; ++i)
                Destroy(content.GetChild(i).gameObject);
        }

        // instantiate and initialize text prefab
        GameObject go = Instantiate(message.textPrefab, content.transform, false);
        go.GetComponent<Text>().text = message.Construct();
        go.GetComponent<UIChatEntry>().message = message;

        AutoScroll();
    }

    // called by chat entries when clicked
    public void OnEntryClicked(UIChatEntry entry)
    {
        // any reply prefix?
        if (!string.IsNullOrWhiteSpace(entry.message.replyPrefix))
        {
            // set text to reply prefix
            messageInput.text = entry.message.replyPrefix;

            // activate
            messageInput.Select();

            // move cursor to end (doesn't work in here, needs small delay)
            Invoke(nameof(MoveTextEnd), 0.1f);
        }
    }

    void MoveTextEnd()
    {
        messageInput.MoveTextEnd(false);
    }

    void AddToInputHistory(string input)
    {
        if(input == "")
            return;
        //如果大於maxCount
        if(inputHistory.Count >= maxHistoryCount)
            inputHistory.RemoveAt(0);

        if( inputHistory.Count == 0 || inputHistory[inputHistory.Count -1] != input)
            inputHistory.Add(input);

        currentHistoryIndex = inputHistory.Count;
    }
}
