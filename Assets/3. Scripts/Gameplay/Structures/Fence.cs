using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fence : Structure
{
    private Collider2D fenceHitbox;
    private SpriteBlender spriteBlender;

    override protected void Awake()
    {
        base.Awake();
        spriteBlender = GetComponentInChildren<SpriteBlender>();
        fenceHitbox = GetComponentInChildren<Collider2D>();
    }

    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();
        spriteBlender.Blend(true);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        fenceHitbox.enabled = false;
        spriteBlender.ReblendNeighbors();
    }
}


