using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Main;

    public static string LOBBY_PASSWORD_KEY = "!E18@tfR!urQSsTxYmbM";
    public static string LOBBY_PASSWORD_VAL = "zYVMH1mhH41*%FQaFm41";

    public static string LOBBY_STATUS_KEY = "LOBBY_STATUS";
    public static string LOBBY_INGAME_VAL = "INGAME";

    private UIBehaviour[] hidableUI;

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

        DontDestroyOnLoad(gameObject);
    }

    [Header("Settings")]
    [SerializeField]
    private int maxPlayer = 16;
    [SerializeField]
    private bool useFacepunchTransport = false;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool showStatus = false;

    private NetworkManager networkManager;
    private bool launched = false;

    private UnityTransport unityTransport;
    private FacepunchTransport facepunchTransport;

    public Lobby? CurrentLobby { get; private set; }

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        unityTransport = GetComponent<UnityTransport>();
        facepunchTransport = GetComponent<FacepunchTransport>();

        facepunchTransport.enabled = useFacepunchTransport;

        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;

        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

        networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    private void OnApplicationQuit() => Disconnect(true);

    private IEnumerator LaunchCoroutine(bool isHost, NetworkTransport networkTransport)
    {
        launched = true;
        CurrentLobby?.SetData(LOBBY_STATUS_KEY, LOBBY_INGAME_VAL);
        yield return TransitionUI.Main.ShowCoroutine();

        networkManager.NetworkConfig.NetworkTransport = networkTransport;

        if (SceneManager.GetActiveScene().name != SceneDirectory.MAIN_GAME)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneDirectory.MAIN_GAME, LoadSceneMode.Single);
            yield return new WaitUntil(() => asyncLoad.isDone);
        }

        // TODO: Handle passworded servers
        // networkManager.ConnectionApprovalCallback = ApprovalCheck;

        if (isHost)
        {
            networkManager.StartHost();
        }
        else
        {
            networkManager.StartClient();
        }
    }

    public void Disconnect(bool quit)
    {
        if (quit)
        {
            LeaveLobby();
            if (networkManager) networkManager.Shutdown();
            return;
        }
        else
        {
            StartCoroutine(DisconnectCoroutine());
        }
    }

    private IEnumerator DisconnectCoroutine()
    {
        LeaveLobby();
        yield return TransitionUI.Main.ShowCoroutine();

        if (networkManager) networkManager.Shutdown();

        if (SceneManager.GetActiveScene().name != SceneDirectory.MAIN_MENU)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneDirectory.MAIN_MENU, LoadSceneMode.Single);
            yield return new WaitUntil(() => asyncLoad.isDone);
        }

        TransitionUI.Main.Hide();
    }

    public async void CreateLobby()
    {
        try
        {
            CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayer);
        }
        catch (Exception e)
        {
            if (showDebugs) Debug.LogError("Failed to create lobby: " + e.Message);
            return;
        }
    }

    public async void JoinLobby(SteamId lobbyId)
    {
        try
        {
            var lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        }
        catch (Exception e)
        {
            if (showDebugs) Debug.LogError("Failed to join lobby: " + e.Message);
            return;
        }
    }

    private void SetLobby(Lobby lobby)
    {
        if (showDebugs) Debug.Log("Setting lobby " + lobby.Id + " with owner " + lobby.Owner.Id + " as current lobby");

        CurrentLobby = lobby;
        facepunchTransport.targetSteamId = lobby.Owner.Id;
    }

    public void LeaveLobby()
    {
        if (showDebugs) Debug.Log("Leaving lobby " + CurrentLobby?.Id);
        CurrentLobby?.Leave();
        CurrentLobby = null;
    }


    #region Steam Callback

    [ContextMenu("Test Game Lobby Join Requested")]
    private void TestGameLobbyJoinRequested()
    {
        OnGameLobbyJoinRequested(new Lobby(), new SteamId());
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        // On invite accepted
        if (showDebugs) Debug.Log("Joining lobby " + lobby.Id);
        if (showDebugs) Debug.Log("OnLobbyEntered Host: lobby = " + lobby.Id + ", id = " + lobby.Owner.Id);

        SetLobby(lobby);
        StartCoroutine(JoinLobbyFromInviteCoroutine());
    }

    private IEnumerator JoinLobbyFromInviteCoroutine()
    {
        if (showDebugs) Debug.Log("Joining lobby from invite");

        if (networkManager.IsServer || networkManager.IsClient)
        {
            yield return TransitionUI.Main.ShowCoroutine();
            if (showDebugs) Debug.Log("Already in a game, disconnecting");

            CurrentLobby?.Leave();

            if (networkManager) networkManager.Shutdown();

            if (SceneManager.GetActiveScene().name != SceneDirectory.MAIN_MENU)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneDirectory.MAIN_MENU, LoadSceneMode.Single);
                yield return new WaitUntil(() => asyncLoad.isDone);
            }
        }

        yield return UIManager.Main.HideUI(true, true);
        LobbyUI.Main.Client();

        yield return TransitionUI.Main.HideCoroutine();
    }

    private void OnLobbyGameCreated(Lobby lobby, uint arg2, ushort arg3, SteamId id)
    {
        if (showDebugs) Debug.Log($"Game created in lobby {lobby.Id}, maxPlayer = {lobby.MaxMembers}");

        SetLobby(lobby);
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        if (showDebugs) Debug.Log($"Entered lobby {lobby.Id}, maxPlayer = {lobby.MaxMembers}");

        if (lobby.MemberCount <= 0)
        {
            if (showDebugs) Debug.Log("Invalid lobby " + lobby.Id);
            lobby.Leave();
            return;
        }

        SetLobby(lobby);
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

    #endregion

    #region Network Callbacks

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (showDebugs) Debug.Log($"Client connected, clientId={clientId}", this);
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (showDebugs) Debug.Log($"Client disconnected, clientId={clientId}", this);
    }

    #endregion

    #region Editor Start

    private void OnGUI()
    {
        if (networkManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            if (!launched) StartButtons();
        }
        else
        {
            launched = false;
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    private void StatusLabels()
    {
        if (!showStatus) return;

        var mode = networkManager.IsHost ?
            "Host" : networkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            networkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }


    private void StartButtons()
    {
        if (!Application.isEditor) return;

        if (GUILayout.Button("Host"))
        {
            StartCoroutine(LaunchCoroutine(true, unityTransport));
        }

        if (GUILayout.Button("Client"))
        {
            StartCoroutine(LaunchCoroutine(false, unityTransport));
        }
    }

    #endregion

    #region UI Methods

    public void StartGameSinglePlayer()
    {
        StartCoroutine(LaunchCoroutine(true, unityTransport));
    }

    public void StartGameMultiplayerLocalHost()
    {
        StartCoroutine(LaunchCoroutine(true, unityTransport));
    }

    public void StartGameMultiplayerLocalClient()
    {
        StartCoroutine(LaunchCoroutine(false, unityTransport));
    }

    public void StartGameMultiplayerOnlineHost()
    {
        StartCoroutine(LaunchCoroutine(true, facepunchTransport));
    }

    public void StartGameMultiplayerOnlineClient()
    {
        //SteamFriends.OpenOverlay("friends");
        StartCoroutine(LaunchCoroutine(false, facepunchTransport));
    }
    #endregion

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        // Your approval logic determines the following values
        response.Approved = true;
        response.CreatePlayerObject = true;

        // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
        response.PlayerPrefabHash = null;

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        // If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
        // On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
        response.Reason = "Some reason for not approving the client";

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
    }
}
