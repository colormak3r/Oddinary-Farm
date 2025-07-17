/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/16/2025 (Khoa)
 * Notes:           <write here>
*/

using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class StructureNetworkTransform : NetworkTransform
{
    /*[Header("Debugs")]
    [SerializeField]
    private bool showDebugs;*/

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

    // Temporarily remove due to performance impact
    // TODO: add position cache to avoid unnecessary updates
    // TODO: rework hammer, adapt to new transform system
    /*protected override void OnBeforeUpdateTransformState()
    {
        base.OnBeforeUpdateTransformState();
        Debug.Log($"OnBeforeUpdateTransformState: {transform.position}");
        PreMove();
    }

    protected override void OnTransformUpdated()
    {
        base.OnTransformUpdated();
        Debug.Log($"OnTransformUpdated: {transform.position}");
        PostMove();
    }*/
}
