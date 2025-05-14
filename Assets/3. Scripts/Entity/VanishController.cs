using System;
using Unity.Netcode;
using UnityEngine;

public class VanishController : NetworkBehaviour
{
    private NetworkVariable<bool> IsVanished = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] colliders;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponentsInChildren<Collider2D>();
    }

    override public void OnNetworkSpawn()
    {
        IsVanished.OnValueChanged += HandleIsVanishedChanged;
        HandleIsVanishedChanged(false, IsVanished.Value);
    }

    override public void OnNetworkDespawn()
    {
        IsVanished.OnValueChanged -= HandleIsVanishedChanged;
    }

    private void HandleIsVanishedChanged(bool previousValue, bool newValue)
    {
        foreach (var spriteRenderer in spriteRenderers)
        {
            spriteRenderer.enabled = !newValue;
        }

        foreach (var collider in colliders)
        {
            collider.enabled = !newValue;
        }
    }

    public void SetVanished(bool value)
    {
        IsVanished.Value = value;
    }
}
