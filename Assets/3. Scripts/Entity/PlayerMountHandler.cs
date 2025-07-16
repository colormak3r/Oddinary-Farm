using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class PlayerMountHandler : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private DrownController drownController;
    [SerializeField]
    private DrownGraphic drownGraphic;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Rigidbody2D rigidbody2D;

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
        //spriteRenderer.enabled = newValue;
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

    [Rpc(SendTo.Owner)]
    private void SetControlRpc(bool isControlled)
    {
        SetControlInternal(isControlled);
    }

    private void SetControlInternal(bool isControlled)
    {
        spriteRenderer.enabled = !isControlled;
        drownController.SetCanBeDrowned(isControlled);
        drownGraphic.SetCanBeWet(isControlled);

        if (isControlled)
            rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        else
            rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
    }
}
