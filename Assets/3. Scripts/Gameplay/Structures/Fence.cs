using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fence : Structure
{
    private SpriteBlender spriteBlender;
    private Collider2D collider2D;

    private void Awake()
    {
        spriteBlender = GetComponentInChildren<SpriteBlender>();
        collider2D = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        spriteBlender.Blend(true);
    }

    public override void Remove()
    {
        RemoveRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RemoveRpc()
    {
        collider2D.enabled = false;
        spriteBlender.ReblendNeighbors();
        if(IsServer)
            Destroy(gameObject);
    }
}


