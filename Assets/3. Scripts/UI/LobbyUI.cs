using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LobbyMode
{
    Host,
    Client
}

public class LobbyUI : UIBehaviour
{
    public static LobbyUI Main;

    public static string LOBBY_STARTED_KEY = "LOBBY_STARTED";
    public static string LOBBY_NAME_KEY = "LOBBY_NAME";

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Required Components")]
    [SerializeField]
    private TMP_InputField lobbyIdInputField;
    [SerializeField]
    private TMP_Text lobbyNameText;
    [SerializeField]
    private TMP_Text lobbyInfoText;
    [SerializeField]
    private TMP_Text lobbyPlayerText;
    [SerializeField]
    private TMP_Text lobbyInstructionText;
    [SerializeField]
    private TMP_Text startButtonText;
    [SerializeField]
    private TMP_Text inviteButtonText;
    [SerializeField]
    private TMP_Text copyPasteButtonText;
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private Button inviteButton;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private LobbyMode lobbyMode;

    protected override void Start()
    {
        base.Start();

        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
    }

    #region Initialization
    public void Host()
    {
        lobbyMode = LobbyMode.Host;
        lobbyIdInputField.readOnly = true;
        lobbyIdInputField.text = "";

        lobbyInfoText.text = "";

        lobbyInstructionText.text = "Invite or share Lobby ID to your friend.";

        lobbyPlayerText.text = "Waiting for player...";

        startButton.gameObject.SetActive(true);

        inviteButton.interactable = false;
        inviteButtonText.text = "Loading";
        copyPasteButtonText.text = "Copy";

        ConnectionManager.Main.CreateLobby();

        Show();
    }

    public void Client()
    {
        lobbyMode = LobbyMode.Client;
        lobbyIdInputField.readOnly = false;

        lobbyIdInputField.text = "";

        lobbyInstructionText.text = "Enter Lobby ID or join a friend from steam overlay.";

        lobbyInfoText.text = "";

        lobbyPlayerText.text = "Waiting for player...";

        startButton.gameObject.SetActive(false);

        inviteButton.interactable = true;
        inviteButtonText.text = "Join";
        copyPasteButtonText.text = "Paste";

        if (ConnectionManager.Main.CurrentLobby.HasValue)
        {
            ConnectionManager.Main.JoinLobby(ConnectionManager.Main.CurrentLobby.Value.Id);
        }
        else
        {
            lobbyIdInputField.text = "";
        }

        Show();
    }
    #endregion

    #region Steam Callbacks 
    public void InviteJoinButtonClicked()
    {
        if (lobbyMode == LobbyMode.Host)
        {
            //SteamFriends.OpenOverlay("friends");
            SteamFriends.OpenGameInviteOverlay(ConnectionManager.Main.CurrentLobby.Value.Id);
        }
        else
        {
            if (!ConnectionManager.Main.CurrentLobby.HasValue && lobbyIdInputField.text != "" && ConnectionManager.Main.CurrentLobby.Value.Id != 0)
            {
                ConnectionManager.Main.JoinLobby(ulong.Parse(lobbyIdInputField.text));
            }
            else
            {
                //SteamFriends.OpenOverlay("friends");
                SteamFriends.OpenGameInviteOverlay(ConnectionManager.Main.CurrentLobby.Value.Id);
            }
        }
    }

    public void StarButtonClicked()
    {
        if (!ConnectionManager.Main.CurrentLobby.HasValue) return;

        if (lobbyMode == LobbyMode.Host)
        {
            ConnectionManager.Main.StartGameMultiplayerOnlineHost();
        }
        else
        {
            if (ConnectionManager.Main.CurrentLobby?.GetData(ConnectionManager.LOBBY_STATUS_KEY) != ConnectionManager.LOBBY_INGAME_VAL)
            {
                lobbyInstructionText.text = "Host has not started the game. Please try again later!";
            }
            else
            {
                ConnectionManager.Main.StartGameMultiplayerOnlineClient();
            }
        }
    }

    public void CopyPasteButtonClicked()
    {
        if (lobbyMode == LobbyMode.Host)
        {
            GUIUtility.systemCopyBuffer = lobbyIdInputField.text;
            copyPasteButtonText.text = "Copied!";
        }
        else
        {
            lobbyIdInputField.text = GUIUtility.systemCopyBuffer;
            copyPasteButtonText.text = "Pasted!";
        }
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            if (showDebugs) Debug.LogError("Failed to create lobby: " + result);
            return;
        }

        if (showDebugs) Debug.Log("Created lobby " + lobby.Id);

        lobby.SetFriendsOnly();
        lobby.SetData("lobbyName", "Test Lobby");
        lobby.SetJoinable(true);

        lobbyIdInputField.text = lobby.Id.ToString();
        lobbyInfoText.text = $"Lobby Name: {lobby.GetData("lobbyName")}" +
            $"\nLobby Type: Friend Only" +
            $"\nLobby Owner: {lobby.Owner.Name}";
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        if (showDebugs) Debug.Log("Entered lobby " + lobby.Id);

        if (lobby.MemberCount == 0 || lobby.Id.Value == 0)
        {
            if (showDebugs) Debug.LogError($"Invalid lobby ID: {lobby.Id}, playerCount = {lobby.MemberCount}");
            lobbyInstructionText.text = "Invalid Lobby ID. Try again!";
        }
        else
        {
            var builder = $"Player Count = {lobby.MemberCount}/100" +
               $"\n\nPlayer List:\n";
            foreach (var member in lobby.Members)
            {
                builder += "> " + member.Name + "\n";
            }
            lobbyPlayerText.text = builder;

            if (lobbyMode == LobbyMode.Host)
            {
                startButtonText.text = "Start";
                copyPasteButtonText.text = "Copy";
                lobbyIdInputField.text = lobby.Id.ToString();
                lobbyInstructionText.text = "Invite more friends or click Start to launch the game!";
            }
            else
            {
                startButtonText.text = "Join";
                copyPasteButtonText.text = "Paste";
                lobbyIdInputField.text = lobby.Id.ToString();
                lobbyInstructionText.text = "Invite more friends or click Join to launch the game!";
            }
            startButton.gameObject.SetActive(true);

            inviteButton.interactable = true;
            inviteButtonText.text = "Invite";
        }
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        lobbyInfoText.text += $"\nPlayer {friend.Name} left the lobby";
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        lobbyInfoText.text += $"\nPlayer {friend.Name} joined the lobby";
    }

    public void LeaveLobby()
    {
        ConnectionManager.Main.LeaveLobby();
    }

    #endregion
}
