using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fence : Structure
{
    [SerializeField]
    private Collider2D movementBlocker;

    SpriteBlender spriteBlender;

    private void Awake()
    {
        spriteBlender = GetComponentInChildren<SpriteBlender>();
        movementBlocker = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(DelayBlend());
    }

    private IEnumerator DelayBlend()
    {
        yield return null;
        spriteBlender.Blend(true);
    }

    public override void Removed()
    {
        RemoveRpc();
    }

    public override void DestroyOnClient()
    {
        movementBlocker.enabled = false;
        spriteBlender.ReblendNeighbors();
    }

    [Rpc(SendTo.Everyone)]
    private void RemoveRpc()
    {
        DestroyOnClient();
        if (IsServer) Destroy(gameObject);
    }
}


