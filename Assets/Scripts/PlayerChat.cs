// We implemented a chat system that works directly with UNET. The chat supports
// different channels that can be used to communicate with other players:
//
// - **Local Chat:** by default, all messages that don't start with a **/** are
// addressed to the local chat. If one player writes a local message, then all
// players around him _(all observers)_ will be able to see the message.
// - **Whisper Chat:** a player can write a private message to another player by
// using the **/ name message** format.
// - **Guild Chat:** we implemented guild chat support with the **/g message**
// - **Info Chat:** the info chat can be used by the server to notify all
// players about important news. The clients won't be able to write any info
// messages.
//
// _Note: the channel names, colors and commands can be edited in the Inspector_
using System;
using UnityEngine;
using Mirror;

[Serializable]
public class ChannelInfo
{
    public string command; // /w etc.
    public string identifierOut; // for sending
    public string identifierIn; // for receiving
    public GameObject textPrefab;

    public ChannelInfo(string command, string identifierOut, string identifierIn, GameObject textPrefab)
    {
        this.command = command;
        this.identifierOut = identifierOut;
        this.identifierIn = identifierIn;
        this.textPrefab = textPrefab;
    }
}

[Serializable]
public struct ChatMessage
{
    public string sender;
    public string identifier;
    public string message;
    public string replyPrefix; // copied to input when clicking the message
    public GameObject textPrefab;

    public ChatMessage(string sender, string identifier, string message, string replyPrefix, GameObject textPrefab)
    {
        this.sender = sender;
        this.identifier = identifier;
        this.message = message;
        this.replyPrefix = replyPrefix;
        this.textPrefab = textPrefab;
    }

    // construct the message
    public string Construct()
    {
        return "<b>" + sender + identifier + ":</b> " + message;
    }
}

public partial class PlayerChat : NetworkBehaviour
{
    ///SpeechBubble
    [Header("SpeechBubble")]
    private MsgFrame msgFrame;
    private bool fakeMsgsFinished;

    public void Awake()
    {
        msgFrame = Instantiate(Resources.Load<GameObject>("MsgFrame"), this.transform).GetComponent<MsgFrame>();
    }

    public void OnSubmit_MsgFrame(string text)
    {
        if (!fakeMsgsFinished) return;

        //local chat
        if (!text.StartsWith("/"))
        {
            // find the space that separates the name and the message
            int i = text.IndexOf(": ");
            if (i >= 0)
            {
                text = text.Substring(i + 1);
            }

            msgFrame.ShowMessage(text);
        }
    }

    ///SpeechBubble_End

    [Header("Components")] // to be assigned in inspector
    public Player player;

    [Header("Channels")]
    public ChannelInfo adminControl = new ChannelInfo("!", "", "", null);
    public ChannelInfo whisper = new ChannelInfo("/w", "(TO)", "(FROM)", null);
    public ChannelInfo local = new ChannelInfo("", "", "", null);
    //public ChannelInfo party = new ChannelInfo("/p", "(Party)", "(Party)", null);
    //public ChannelInfo guild = new ChannelInfo("/g", "(Guild)", "(Guild)", null);
    public ChannelInfo info = new ChannelInfo("", "(Info)", "(Info)", null);

    [Header("Other")]
    public int maxLength = 70;

    public override void OnStartLocalPlayer()
    {
        ///SpeechBubble_AddOn
        fakeMsgsFinished = true;
        // test messages
        UIChat.singleton.AddMessage(new ChatMessage("", info.identifierIn, "Use /w NAME to whisper", "", info.textPrefab));
        UIChat.singleton.AddMessage(new ChatMessage("", info.identifierIn, "Or click on a message to reply", "", info.textPrefab));
        //UIChat.singleton.AddMessage(new ChatMessage("Someone", guild.identifierIn, "Anyone here?", "/g ",  guild.textPrefab));
        //UIChat.singleton.AddMessage(new ChatMessage("Someone", party.identifierIn, "Let's hunt!", "/p ",  party.textPrefab));
        //UIChat.singleton.AddMessage(new ChatMessage("Someone", whisper.identifierIn, "Are you there?", "/w Someone ",  whisper.textPrefab));
        //UIChat.singleton.AddMessage(new ChatMessage("Someone", local.identifierIn, "Hello!", "/w Someone ",  local.textPrefab));

        // addon system hooks
        //Utils.InvokeMany(typeof(PlayerChat), this, "OnStartLocalPlayer_");
    }

    // submit tries to send the string and then returns the new input text
    [Client]
    public string OnSubmit(string text)
    {
        // not empty and not only spaces?
        if (!string.IsNullOrWhiteSpace(text))
        {
            // command in the commands list?
            // note: we don't do 'break' so that one message could potentially
            //       be sent to multiple channels (see mmorpg local chat)
            string lastcommand = "";
            if (text.StartsWith(adminControl.command))
            {
                AddMsgAdmin(text);
                //long command with detail
                if (text.StartsWith("!add"))
                {
                    string[] result;
                    if (ParseAddOrRemoveItem("!add", text, out result))
                    {
                        player.CmdAddItem(result[0], result[1].ToInt(), result[2]);
                    }
                    return "";//do not submit to bubble
                }
                else if(text.StartsWith("!remove"))
                {
                    string[] result;
                    if (ParseAddOrRemoveItem("!remove", text, out result))
                    {
                        player.CmdRemoveItem(result[0], result[1].ToInt(), result[2]);
                    }
                    return "";//do not submit to bubble
                }
                //short command
                switch (text)
                {
                    
                    case "!rain":
                        player.CmdTestRain();
                        break;
                    case "!fountain":
                        player.CmdTestFountain();
                        break;
                    case "!enemy":
                        player.CmdTestSpawnEnemy();
                        break;
                    case "!e":
                        player.CmdTestSpawnEnemy();
                        break;
                    case "!save":
                        player.CmdTestSavePlayer();
                        break;
                    case "!night":
                        player.CmdTestNight();
                        break;
                    case "!day":
                        player.CmdTestDay();
                        break;
                    case "!f":
                        player.CmdTestFirework();
                        break;

                    default:
                        AddMsgInfo("Unknown command");
                        Debug.Log("Unknown command");
                        break;
                }
                return "";//do not submit to bubble
            }
            else if (text.StartsWith(whisper.command))
            {
                // whisper
                string[] parsed = ParsePM(whisper.command, text);
                string user = parsed[0];
                string msg = parsed[1];
                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(msg))
                {
                    if (user != name)
                    {
                        lastcommand = whisper.command + " " + user + " ";
                        CmdMsgWhisper(user, msg);
                    }
                    else AddMsgInfo("cant whisper to self");
                }
                else AddMsgInfo("invalid whisper format: " + user + "/" + msg);
            }
            else if (!text.StartsWith("/"))
            {
                // local chat is special: it has no command
                lastcommand = "";
                CmdMsgLocal(text);
            }
            ///SpeechBubble
            OnSubmit_MsgFrame(text);

            // input text should be set to lastcommand
            return lastcommand;
        }

        // input text should be cleared
        return "";
    }

    // parse a message of form "/command message"
    static string ParseGeneral(string command, string msg)
    {
        // return message without command prefix (if any)
        return msg.StartsWith(command + " ") ? msg.Substring(command.Length + 1) : "";
    }

    static string[] ParsePM(string command, string pm)
    {
        // parse to /w content
        string content = ParseGeneral(command, pm);

        // now split the content in "user msg"
        if (content != "")
        {
            // find the first space that separates the name and the message
            int i = content.IndexOf(" ");
            if (i >= 0)
            {
                string user = content.Substring(0, i);
                string msg = content.Substring(i + 1);
                return new string[] { user, msg };
            }
        }
        return new string[] { "", "" };
    }

    public bool ParseAddOrRemoveItem(string command, string msg, out string[] result)
    {
        // parse to !add content
        string content = ParseGeneral(command, msg);
        string amountItem = "";
        string user = "";
        string amount = "";
        string item = "";
        result = new string[] { "" };

        //print(content);
        // now split the content in "user item&amount"
        if (content != "")
        {
            // find the first space that separates the name and the message
            int i = content.IndexOf(" ");
            if (i >= 0)
            {
                user = content.Substring(0, i);
                amountItem = content.Substring(i + 1);
            }
        }
        else
        {
            AddMsgInfo("Command failed. Add/Remove item format is !add/remove {playerName} {amount} {item}.");
            return false;
        }
        if (amountItem != "")
        {
            // find the first space that separates the item and the amount
            int i = amountItem.IndexOf(" ");
            if (i >= 0)
            {
                amount = amountItem.Substring(0, i);
                item = amountItem.Substring(i + 1);
                //check if amount is empty
                if (amount == "")
                {
                    AddMsgInfo("Command failed. Add/Remove item format is !add/remove {playerName} {amount} {item}.");
                    return false;
                }
                //check if amount is number only
                if (!int.TryParse(amount, out int n))
                {
                    AddMsgInfo("Command failed. Amount must be number only.");
                    return false;
                }
            }
        }
        else
        {
            AddMsgInfo("Command failed. Add/Remove item format is !add/remove {playerName} {amount} {item}.");
            return false;
        }

        result = new string[] { user, amount, item };
        return true;
    }

    // networking //////////////////////////////////////////////////////////////
    [Command]
    void CmdMsgLocal(string message)
    {
        if (message.Length > maxLength) return;

        // it's local chat, so let's send it to all observers via ClientRpc
        RpcMsgLocal(name, message);
    }

    [Command]
    void CmdMsgWhisper(string playerName, string message)
    {
        if (message.Length > maxLength) return;

        // find the player with that name
        Player onlinePlayer;
        if (Player.onlinePlayers.TryGetValue(playerName, out onlinePlayer))
        {
            // receiver gets a 'from' message, sender gets a 'to' message
            // (call TargetRpc on that GameObject for that connection)
            onlinePlayer.chat.TargetMsgWhisperFrom(name, message);
            TargetMsgWhisperTo(playerName, message);
        }
    }

    // send a global info message to everyone
    [Server]
    public void SendGlobalMessage(string message)
    {
        foreach (Player onlinePlayer in Player.onlinePlayers.Values)
            player.chat.TargetMsgInfo(message);
    }

    // message handlers ////////////////////////////////////////////////////////
    [TargetRpc] // only send to one client
    public void TargetMsgWhisperFrom(string sender, string message)
    {
        // add message with identifierIn
        string identifier = whisper.identifierIn;
        string reply = whisper.command + " " + sender + " "; // whisper
        UIChat.singleton.AddMessage(new ChatMessage(sender, identifier, message, reply, whisper.textPrefab));
    }

    [TargetRpc] // only send to one client
    public void TargetMsgWhisperTo(string receiver, string message)
    {
        // add message with identifierOut
        string identifier = whisper.identifierOut;
        string reply = whisper.command + " " + receiver + " "; // whisper
        UIChat.singleton.AddMessage(new ChatMessage(receiver, identifier, message, reply, whisper.textPrefab));
    }

    [ClientRpc]
    public void RpcMsgLocal(string sender, string message)
    {
        // add message with identifierIn or Out depending on who sent it
        string identifier = sender != name ? local.identifierIn : local.identifierOut;
        string reply = whisper.command + " " + sender + " "; // whisper
        UIChat.singleton.AddMessage(new ChatMessage(sender, identifier, message, reply, local.textPrefab));

        ///SpeechBubble
        Player p;//Player.onlinePlayers[sender];
        Player.onlinePlayers.TryGetValue(sender, out p);

        if (p)
            p.GetComponent<PlayerChat>().msgFrame.ShowMessage(message);
    }
    [TargetRpc] // only send to one client
    public void TargetMsgInfo(string message)
    {
        UIChat.singleton.AddMessage(new ChatMessage("", info.identifierIn, message, "", info.textPrefab));
    }

    // info message can be added from client too
    public void AddMsgInfo(string message)
    {
        UIChat.singleton.AddMessage(new ChatMessage("", info.identifierIn, message, "", info.textPrefab));
    }

    public void AddMsgAdmin(string message)
    {
        UIChat.singleton.AddMessage(new ChatMessage("", adminControl.identifierIn, message, "", adminControl.textPrefab));
    }
}
