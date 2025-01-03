using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
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
    private Button startButton;
    [SerializeField]
    private Button inviteButton;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private LobbyMode lobbyMode;

    private void Start()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
    }

    #region Steam Callbacks

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
        inviteButtonText.text = "Creating Room";

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

        Show();
    }

    public void InviteButtonClicked()
    {
        if (lobbyMode == LobbyMode.Host)
        {
            SteamFriends.OpenOverlay("friends");
        }
        else
        {
            if (!ConnectionManager.Main.CurrentLobby.HasValue && lobbyIdInputField.text != "")
            {
                ConnectionManager.Main.JoinLobby(ulong.Parse(lobbyIdInputField.text));
            }
            else
            {
                SteamFriends.OpenOverlay("friends");
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
            ConnectionManager.Main.StartGameMultiplayerOnlineClient();
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

        if (lobby.MemberCount == 0)
        {
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

            lobbyInstructionText.text = "Enter Lobby ID or join a friend from steam overlay.";

            if (lobbyMode == LobbyMode.Host)
            {
                startButtonText.text = "Start";
            }
            else
            {
                startButtonText.text = "Join";
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
