using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class StructureNetworkTransform : NetworkTransform
{
    private SpriteBlender spriteBlender;
    private Collider2D hitBox;

    protected override void Awake()
    {
        base.Awake();
        spriteBlender = GetComponent<SpriteBlender>();
        hitBox = GetComponent<Collider2D>();
    }

    public void MoveTo(Vector2 position)
    {
        MoveToRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void MoveToRpc(Vector2 position)
    {
        PreMove();
        transform.position = position;
        PostMove();
    }

    private void PreMove()
    {
        if (hitBox) hitBox.enabled = false;
        if (spriteBlender) spriteBlender.ReblendNeighbors();
    }

    private void PostMove()
    {
        if (hitBox) hitBox.enabled = true;
        if (spriteBlender) spriteBlender.Blend(true);
    }

    protected override void OnBeforeUpdateTransformState()
    {
        base.OnBeforeUpdateTransformState();
        PreMove();
    }

    protected override void OnTransformUpdated()
    {
        base.OnTransformUpdated();
        PostMove();
    }
}
