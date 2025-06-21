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
    private DrownController drownController;
    [SerializeField]
    private DrownGraphic drownGraphic;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<bool> IsControlled = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);
    public bool IsControlledValue => IsControlled.Value;

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
        if (IsServer)
        {
            SetControlRpc(isControlled);
        }
        else
        {
            SetControlInternal(isControlled);
        }

    }

    // Toggle control over player
    [Rpc(SendTo.Owner)]
    private void SetControlRpc(bool isControlled)
    {
        SetControlInternal(isControlled);
    }

    // Toggle player immunity and input
    private void SetControlInternal(bool isControlled)
    {
        physicCollider.enabled = !isControlled;             // Toggle player collider
        moveableController.SetMoveable(!isControlled);      // Toggle player movement
        entityMovement.SetCanBeKnockback(!isControlled);    // Toggle player knockback
        IsControlled.Value = isControlled;                  // Toggle Controlled value
        drownController.SetCanBeDrowned(!isControlled);     // Toggle player drown
        drownGraphic.SetCanBeWet(!isControlled);            // Toggle player wet effect
    }
}
