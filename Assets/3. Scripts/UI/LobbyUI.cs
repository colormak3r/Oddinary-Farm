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
    private TMP_Text startButtonText;

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
        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;

        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;

        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }

    #region Steam Callbacks

    public void Host()
    {
        lobbyMode = LobbyMode.Host;
        lobbyIdInputField.readOnly = true;
        startButtonText.text = "Joining...";
        ConnectionManager.Main.CreateLobby();
    }

    public void Client()
    {
        lobbyMode = LobbyMode.Client;
        lobbyIdInputField.readOnly = false;
        startButtonText.text = "Join";
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        // On invite accepted
        if (showDebugs) Debug.Log("Joining lobby " + lobby.Id);

        ConnectionManager.Main.SetLobby(lobby, id);
        ConnectionManager.Main.StartGameMultiplayerOnlineClient();
    }

    private void OnLobbyGameCreated(Lobby lobby, uint arg2, ushort arg3, SteamId id)
    {
        if (showDebugs) Debug.Log("Game created in lobby " + lobby.Id);
    }

    private void OnLobbyInvite(Friend friend, Lobby lobby)
    {
        if (showDebugs) Debug.Log("Received lobby invite from " + friend.Name);
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        if (showDebugs) Debug.Log("Player " + friend.Name + " left the lobby");
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        if (showDebugs) Debug.Log("Player " + friend.Name + " joined the lobby");
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        if (showDebugs) Debug.Log("Entered lobby " + lobby.Id);

        if (lobbyMode == LobbyMode.Host)
        {
            startButtonText.text = "Start";
            ConnectionManager.Main.SetLobby(lobby, 0);
        }
        else
        {
            startButtonText.text = "Ready";
            ConnectionManager.Main.SetLobby(lobby, lobby.Owner.Id);
        }
    }

    public void StarButtonClicked()
    {
        if (lobbyMode == LobbyMode.Host)
        {
            ConnectionManager.Main.StartGameMultiplayerOnlineHost();
        }
        else
        {
            if (ConnectionManager.Main.CurrentLobby == null)
            {
                Lobby lobby = new Lobby(ulong.Parse(lobbyIdInputField.text));
                try
                {
                    lobby.Join();
                }
                catch (System.Exception e)
                {
                    if (showDebugs) Debug.LogError("Failed to join lobby: " + e.Message);
                    lobbyInfoText.text = "Failed to join lobby: " + e.Message;
                    return;
                }
            }
            else
            {
                ConnectionManager.Main.StartGameMultiplayerOnlineClient();
            }
        }
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            if (showDebugs) Debug.LogError("Failed to create lobby: " + result);
            return;
        }

        lobby.SetFriendsOnly();
        lobby.SetData("lobbyName", "Test Lobby");
        lobby.SetJoinable(true);

        lobbyIdInputField.text = lobby.Id.ToString();
        lobbyInfoText.text = $"Lobby Name:{lobby.GetData("lobbyName")}" +
            $"\nLobby Type = Friend Only" +
            $"\nLobby Owner = {lobby.Owner.Name}";

        if (showDebugs) Debug.Log("Created lobby " + lobby.Id);
    }

    #endregion
}
