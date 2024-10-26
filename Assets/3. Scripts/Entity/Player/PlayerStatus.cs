using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerStatus : EntityStatus
{
    [Header("Player Settings")]
    private Transform respawnPoint;

    private NetworkVariable<FixedString128Bytes> GUID = new NetworkVariable<FixedString128Bytes>();

    public string GUIDValue => GUID.Value.ToString();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            GUID.Value = Guid.NewGuid().ToString();
        }        
    }

    protected override void HandleCurrentHealthChange(uint previousValue, uint newValue)
    {
        base.HandleCurrentHealthChange(previousValue, newValue);
        if (!IsOwner) return;

        PlayerStatusUI.Main.UpdateHealth(CurrentHealthValue);
    }

    protected override void OnEntityDeathOnServer()
    {
        OnEntitySpawnOnServer();
    }

    protected override void OnEntityDeathOnClient()
    {
        if (respawnPoint != null)
            transform.position = respawnPoint.position;
        else
            transform.position = Vector3.zero;
    }
}
