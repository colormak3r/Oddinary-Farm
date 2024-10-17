using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private GameObject playerPrefab;

    private void Start()
    {
        NetworkManager.Singleton.OnConnectionEvent += HandleConnectionEvent;
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnConnectionEvent -= HandleConnectionEvent;
    }

    private void HandleConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (!IsServer) return;

        if (data.EventType == ConnectionEvent.ClientConnected)
        {
            var player = Instantiate(playerPrefab);
            var networkObj = player.GetComponent<NetworkObject>();
            
            networkObj.Spawn();
            networkObj.ChangeOwnership(data.ClientId);
        }
    }
}
