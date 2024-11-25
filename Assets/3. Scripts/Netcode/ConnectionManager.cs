using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    private static ConnectionManager Main;

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
    private string mainGameScene = "Main Game";

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool showStatus = false;

    private NetworkManager networkManager;
    private bool launched = false;

    private UnityTransport unityTransport;
    private FacepunchTransport facepunchTransport;

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        unityTransport = GetComponent<UnityTransport>();
        facepunchTransport = GetComponent<FacepunchTransport>();
    }

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
        StartCoroutine(LaunchCoroutine(false, facepunchTransport));
    }

    private IEnumerator LaunchCoroutine(bool isHost, NetworkTransport networkTransport)
    {
        launched = true;
        yield return TransitionUI.Main.ShowCoroutine();

        networkManager.NetworkConfig.NetworkTransport = networkTransport;

        if (SceneManager.GetActiveScene().name != mainGameScene)
        {
            // Load the desired scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainGameScene, LoadSceneMode.Single);

            // Optionally, display a loading progress bar
            while (!asyncLoad.isDone)
            {
                // You can add UI feedback here if desired
                yield return null;
            }
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
