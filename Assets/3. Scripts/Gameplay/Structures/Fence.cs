using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fence : Structure
{
    [Header("Fence Settings")]
    [SerializeField]
    private Collider2D movementBlocker;

    private SpriteBlender spriteBlender;

    override protected void Awake()
    {
        base.Awake();
        spriteBlender = GetComponentInChildren<SpriteBlender>();
        if (movementBlocker == null)
            movementBlocker = GetComponent<Collider2D>();
    }

    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();
        spriteBlender.Blend(true);
    }

    protected override void RemoveOnClient()
    {
        movementBlocker.enabled = false;
        spriteBlender.ReblendNeighbors();
        base.RemoveOnClient();
    }
}


