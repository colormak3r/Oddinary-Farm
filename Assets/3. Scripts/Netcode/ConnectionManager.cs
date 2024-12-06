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
    private int maxPlayer = 8;
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

        if (useFacepunchTransport)
        {
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;

            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
        }

        networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private void OnDestroy()
    {
        if (useFacepunchTransport)
        {
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;

            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        }

        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    private void OnApplicationQuit() => Disconnect(true);

    private IEnumerator LaunchCoroutine(bool isHost, NetworkTransport networkTransport)
    {
        launched = true;
        yield return TransitionUI.Main.ShowCoroutine();

        networkManager.NetworkConfig.NetworkTransport = networkTransport;
        facepunchTransport.enabled = networkTransport == facepunchTransport;

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

            if (facepunchTransport.enabled) CreateLobby();

        }
        else
        {
            networkManager.StartClient();
        }
    }

    public async void CreateLobby()
    {
        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayer);
    }

    public void Disconnect(bool quit)
    {
        if (quit)
        {
            CurrentLobby?.Leave();
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
        CurrentLobby?.Leave();
        yield return TransitionUI.Main.ShowCoroutine();

        if (networkManager) networkManager.Shutdown();

        if (SceneManager.GetActiveScene().name != SceneDirectory.MAIN_MENU)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneDirectory.MAIN_MENU, LoadSceneMode.Single);
            yield return new WaitUntil(() => asyncLoad.isDone);
        }

        TransitionUI.Main.Hide();
    }

    #region Steam Callbacks


    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        // On invite accepted
        if (showDebugs) Debug.Log("Joining lobby " + lobby.Id);
        facepunchTransport.targetSteamId = id;

        StartCoroutine(LaunchCoroutine(false, facepunchTransport));
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

        LobbyUI.Main?.OnLobbyEntered(lobby);

        if (networkManager.IsHost) return;

        // Only client can join a lobby. Host should create a lobby
        StartGameMultiplayerLocalClient();
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

        if (showDebugs) Debug.Log("Created lobby " + lobby.Id);
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
        SteamFriends.OpenOverlay("friends");
        //StartCoroutine(LaunchCoroutine(false, facepunchTransport));
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
