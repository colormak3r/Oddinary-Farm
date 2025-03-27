using System;
using Unity.Netcode;
using UnityEngine;

public class HotAirBalloonController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Collider2D physicCollider;
    [SerializeField]
    private MoveableController moveableController;
    [SerializeField]
    private EntityMovement entityMovement;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private NetworkVariable<bool> IsControlled = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    override public void OnNetworkSpawn()
    {
        IsControlled.OnValueChanged += HandleIsControlledChanged;
    }

    override public void OnNetworkDespawn()
    {
        IsControlled.OnValueChanged -= HandleIsControlledChanged;
    }

    private void HandleIsControlledChanged(bool previousValue, bool newValue)
    {
        spriteRenderer.enabled = newValue;
    }

    public void SetControl(bool isControlled)
    {
        physicCollider.enabled = !isControlled;
        moveableController.SetMoveable(!isControlled);
        entityMovement.SetCanBeKnockback(!isControlled);
        IsControlled.Value = isControlled;
    }
}
