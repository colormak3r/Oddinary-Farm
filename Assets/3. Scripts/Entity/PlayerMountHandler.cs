using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMountHandler : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private SortingGroup sortingGroup;

    private Rigidbody2D rBody;
    private PlayerStatus playerStatus;
    private DrownController drownController;
    private DrownGraphic drownGraphic;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<bool> IsControlled = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);
    public bool IsControlledValue => IsControlled.Value;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody2D>();
        playerStatus = GetComponent<PlayerStatus>();
        drownController = GetComponent<DrownController>();
        drownGraphic = GetComponent<DrownGraphic>();
    }

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
        Debug.Log($"PlayerMountHandler: SetControlInternal called with isControlled: {isControlled}");
        sortingGroup.enabled = isControlled;
        drownController.SetCanBeDrowned(isControlled);
        drownGraphic.SetCanBeWet(isControlled);

        if (isControlled)
            rBody.bodyType = RigidbodyType2D.Dynamic;
        else
            rBody.bodyType = RigidbodyType2D.Kinematic;
    }
}
