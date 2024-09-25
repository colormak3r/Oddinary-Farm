using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerStatus : NetworkBehaviour
{
    private NetworkVariable<FixedString128Bytes> GUID = new NetworkVariable<FixedString128Bytes>();

    public string GUIDValue => GUID.Value.ToString();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GUID.Value = Guid.NewGuid().ToString();
        }
    }
}
