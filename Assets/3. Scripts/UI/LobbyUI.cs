/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

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
    private GameObject copyButton;
    [SerializeField]
    private TMP_Text copyButtonText;
    [SerializeField]
    private GameObject pasteButton;
    [SerializeField]
    private TMP_Text pasteButtonText;
    [SerializeField]
    private Button startGameButton;
    [SerializeField]
    private Button joinGameButton;
    [SerializeField]
    private Button inviteFriendButton;
    [SerializeField]
    private Button joinRoomButton;

    [Header("LobbyUI Debugs")]
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

        startGameButton.gameObject.SetActive(true);
        joinGameButton.gameObject.SetActive(false);
        inviteFriendButton.gameObject.SetActive(true);
        joinRoomButton.gameObject.SetActive(false);
        copyButton.SetActive(true);
        copyButtonText.text = "Copy";
        pasteButton.SetActive(false);
        pasteButtonText.text = "Paste";

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

        startGameButton.gameObject.SetActive(false);
        joinGameButton.gameObject.SetActive(false);
        joinRoomButton.gameObject.SetActive(true);
        inviteFriendButton.gameObject.SetActive(false);
        copyButton.SetActive(false);
        copyButtonText.text = "Copy";
        pasteButton.SetActive(true);
        pasteButtonText.text = "Paste";

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

    #region Button Hooks
    public void InviteButtonClicked()
    {
        AudioManager.Main.PlayClickSound();

        SteamFriends.OpenGameInviteOverlay(ConnectionManager.Main.CurrentLobby.Value.Id);
    }

    public void JoinRoomButtonClicked()
    {
        AudioManager.Main.PlayClickSound();

        if (!ConnectionManager.Main.CurrentLobby.HasValue && lobbyIdInputField.text != "")
        {
            ConnectionManager.Main.JoinLobby(ulong.Parse(lobbyIdInputField.text));
        }
        else
        {
            SteamFriends.OpenOverlay("friends");
        }
    }

    public void StartGameButtonClicked()
    {
        AudioManager.Main.PlayClickSound();

        if (!ConnectionManager.Main.CurrentLobby.HasValue) return;

        ConnectionManager.Main.StartGameMultiplayerOnlineHost();
    }

    public void JoinGameButtonClicked()
    {
        AudioManager.Main.PlayClickSound();

        if (!ConnectionManager.Main.CurrentLobby.HasValue) return;

        if (ConnectionManager.Main.CurrentLobby?.GetData(ConnectionManager.LOBBY_STATUS_KEY) != ConnectionManager.LOBBY_INGAME_VAL)
        {
            lobbyInstructionText.text = "Host has not started the game. Please try again later!";
        }
        else
        {
            ConnectionManager.Main.StartGameMultiplayerOnlineClient();
        }
    }

    public void CopyButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        GUIUtility.systemCopyBuffer = lobbyIdInputField.text;
        copyButtonText.text = "Copied!";
    }

    public void PasteButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        lobbyIdInputField.text = GUIUtility.systemCopyBuffer;
        pasteButtonText.text = "Pasted!";
    }

    public void OnAppearanceButtonClicked()
    {
        AppearanceUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnPetSelectionButtonClicked()
    {
        PetSelectionUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnBackButtonClicked()
    {
        HideNoFade();
        SteamMenuUI.Main.Show();
        AudioManager.Main.PlayClickSound();
        LeaveLobby();
    }
    #endregion

    #region Steam Callbacks 
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
            RefreshPlayerList(lobby);
            lobbyIdInputField.text = lobby.Id.ToString();

            if (lobbyMode == LobbyMode.Host)
            {
                lobbyInstructionText.text = "Invite more friends or click Start to launch the game!";
            }
            else
            {
                joinRoomButton.gameObject.SetActive(false);
                inviteFriendButton.gameObject.SetActive(true);
                pasteButton.SetActive(false);
                copyButton.SetActive(true);
                joinGameButton.gameObject.SetActive(true);
                lobbyInstructionText.text = "Invite more friends or click Join to launch the game!";
            }
        }
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        lobbyInfoText.text += $"\nPlayer {friend.Name} left the lobby";

        RefreshPlayerList(lobby);
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        lobbyInfoText.text += $"\nPlayer {friend.Name} joined the lobby";

        RefreshPlayerList(lobby);
    }

    #endregion

    #region Utility
    private void RefreshPlayerList(Lobby lobby)
    {
        var builder = $"Player Count = {lobby.MemberCount}/{lobby.MaxMembers}" +
               $"\n\nPlayer List:\n";
        foreach (var member in lobby.Members)
        {
            builder += "> " + member.Name + "\n";
        }
        lobbyPlayerText.text = builder;
    }

    public void LeaveLobby()
    {
        Debug.Log("LobbyUI: Leaving lobby...");
        ConnectionManager.Main.LeaveLobby();
    }
    #endregion

}
