using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    private NetworkManager networkManager;
    private bool launched = false;

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
        if (isHost)
            networkManager.StartHost();
        else
            networkManager.StartClient();
    }
}
