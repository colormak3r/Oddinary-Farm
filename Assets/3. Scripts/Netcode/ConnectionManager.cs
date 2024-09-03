using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
        if (networkManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            StartButtons();
        }
        else
        {
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
            networkManager.StartHost();

        }

        if (GUILayout.Button("Client"))
        {
            networkManager.StartClient();
        }
    }
}
