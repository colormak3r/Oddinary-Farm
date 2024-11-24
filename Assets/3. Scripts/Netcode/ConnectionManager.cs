using Netcode.Transports.Facepunch;
using System.Collections;
using System.Collections.Generic;
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

    private NetworkManager networkManager;
    private bool launched = false;

    private UnityTransport unityTransport;
    private FacepunchTransport facepunchTransport;

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();        
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
        var mode = networkManager.IsHost ?
            "Host" : networkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            networkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }


    private void StartButtons()
    {
        if (GUILayout.Button("Host"))
        {
            StartCoroutine(LaunchCoroutine(true));

        }

        if (GUILayout.Button("Client"))
        {
            StartCoroutine(LaunchCoroutine(false));
        }
    }

    private IEnumerator LaunchCoroutine(bool isHost)
    {
        launched = true;
        yield return TransitionUI.Main.ShowCoroutine();

        if (SceneManager.GetActiveScene().name != mainGameScene)
        {
            SceneManager.LoadScene(mainGameScene);
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
            //networkManager.NetworkConfig.NetworkTransport = facepunchTransport;
        }
        else
        {
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
        }

        //networkManager.ConnectionApprovalCallback = ApprovalCheck;
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
