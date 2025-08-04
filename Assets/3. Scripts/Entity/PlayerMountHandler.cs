/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   08/01/2025 (Khoa)
 * Notes:           This script will handle player state when mounting or dismounting, both physically and graphically
*/

using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMountHandler : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private SortingGroup sortingGroup;
    [SerializeField]
    private Collider2D physicCollider;

    private Rigidbody2D rBody;
    private PlayerStatus playerStatus;
    private DrownController drownController;
    private DrownGraphic drownGraphic;
    private NetworkTransform networkTransform;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private NetworkVariable<bool> IsMounting = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);
    public bool IsMountingValue => IsMounting.Value;

    public Action<bool> OnMountingChanged;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody2D>();
        playerStatus = GetComponent<PlayerStatus>();
        drownController = GetComponent<DrownController>();
        drownGraphic = GetComponent<DrownGraphic>();
        networkTransform = GetComponent<NetworkTransform>();
    }

    public override void OnNetworkSpawn()
    {
        IsMounting.OnValueChanged += HandleIsControlledChanged;
        HandleIsControlledChanged(false, IsMounting.Value); // Initialize state
    }

    public override void OnNetworkDespawn()
    {
        IsMounting.OnValueChanged -= HandleIsControlledChanged;
    }

    /// <summary>
    /// Run owner's behaviour when the parent NetworkObject changes.
    /// </summary>
    /// <param name="parentNetworkObject"></param>
    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if (parentNetworkObject)
        {
            mountController = parentNetworkObject.GetComponent<MountController>();
            if (IsOwner)
            {
                mountController.SetIsBeingControlled(true);
                Spectator.Main.SetCamera(mountController.CameraPoint);
            }
            if (showDebugs) Debug.Log($"Mount Interaction: Set mountController to {mountController} on parent change.");
        }
        else
        {
            if (IsOwner && mountController)
            {
                mountController.SetIsBeingControlled(false);
                Spectator.Main.SetCamera(transform);
            }
            mountController = null;
            if (showDebugs) Debug.Log("Mount Interaction: Parent NetworkObject is null");
        }
    }

    private void Update()
    {
        // Location update after parent change is ambiguous, so we force set the local position here
        if (mountController) transform.position = mountController.MountingPoint.position;
    }

    private void HandleIsControlledChanged(bool previousValue, bool isMounting)
    {
        sortingGroup.enabled = !isMounting;
        physicCollider.enabled = !isMounting;
        drownController.SetCanBeDrowned(!isMounting);
        drownGraphic.SetCanBeWet(!isMounting);

        if (!isMounting)
        {
            rBody.bodyType = RigidbodyType2D.Dynamic;
            //rBody.simulated = true;
        }
        else
        {
            rBody.bodyType = RigidbodyType2D.Kinematic;
            //rBody.simulated = false;
        }

        OnMountingChanged?.Invoke(isMounting);
    }

    private MountController mountController;
    public void SetDirection(Vector2 direcion)
    {
        // Generalized for all mounts
        if (mountController) mountController.Move(direcion);
    }

    // Can set speed multiplier of the mount here in the future
    // Can set special skill/keybinds of the mount here in the future

    #region SetIsMounting
    public void SetIsMounting(bool isMounting)
    {
        SetMountingRpc(isMounting);
    }

    [Rpc(SendTo.Owner)]
    private void SetMountingRpc(bool isMounting)
    {
        Debug.Log($"PlayerMountHandler: SetMountingRpc called with isMounting: {isMounting}");
        IsMounting.Value = isMounting;
    }
    #endregion
}
