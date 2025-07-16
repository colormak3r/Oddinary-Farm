/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   06/27/2025 (Ryan)
 * Notes:           Handles all interactions with a mount
*/
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles all interactions with a mount.
/// Releases/binds the controls of the player and mount.
/// Handles changes in player transform when mounting.
/// </summary>
public class MountInteraction : NetworkBehaviour, IInteractable
{
    [Header("Events")]
    [SerializeField] public Action<Transform> OnMount;       // Called when a player mounts; can be used to signal animal behaviors 
    [SerializeField] public Action<Transform> OnDismount;    // Called when a player dismounts; can be used to signal animal behaviors 

    [Header("Settings")]
    [SerializeField] private Vector3 _mountOffset;     // Offset determines where the player stands relative to the mount

    [Header("Debugging")]
    [SerializeField] private bool _debug = false;

    public bool CanMount { get; set; } = true;

    private NetworkVariable<NetworkObjectReference> CurrentOwner = new NetworkVariable<NetworkObjectReference>();
    private Coroutine _mountingTransformCo;

    public override void OnNetworkSpawn()
    {
        CurrentOwner.OnValueChanged += HandleCurrentOwnerChange;
    }

    public override void OnNetworkDespawn()
    {
        CurrentOwner.OnValueChanged -= HandleCurrentOwnerChange;

        if (IsServer)
        {
            HandleOnPlayerDieOnServer();
        }
    }

    /// <summary>
    /// Handles all cases when a player interacts with the mount .
    /// Case 1: New owner is detected, mount is free
    /// Case 2: New owner is detected, mount is occupied
    /// Case 3: Prev owner wants to release, mount is freed
    /// </summary>
    /// <param name="source">Player's transform.</param>
    public void Interact(Transform source)
    {
        if (!CanMount)
            return;

        if (_debug) Debug.Log("Attempting Mount...");

        // Try getting a network object comp from the source
        if (!source.gameObject.TryGetComponent<NetworkObject>(out var sourceNetObj))
        {
            Debug.LogWarning("Mount Interaction Warning: Interacted source was not a NetworkObject");
            return;
        }

        if (CurrentOwner.Value.TryGet(out var networkObject))     // There is already an owner
        {
            // Case 1
            if (networkObject == sourceNetObj)        // Unmount
            {
                if (_debug) Debug.Log("Mount Interaction: Player has Unmounted");
                SetCurrentOwnerRpc(default);        // Change CurrentOwner Network Variable
                OnDismount?.Invoke(source);
            }
            // Case 2
            else                                // Cannot mount because there's already a mounter
                if (_debug) Debug.Log("Mount Interaction: Already has a mounter.");
        }
        // Case 3
        else                    // There is no owner of the object -> Mount
        {
            if (_debug) Debug.Log("Mount Interaction: Player has Mounted");
            SetCurrentOwnerRpc(sourceNetObj);        // Change CurrentOwner Network Variable
            OnMount?.Invoke(source);
            // TODO: Handle multiple seats; parent under new child objects
        }
    }

    /// <summary>
    /// Handles changes to the CurrentOwner NetworkVariable
    /// </summary>
    /// <param name="source">New owner reference</param>
    [Rpc(SendTo.Server)]
    private void SetCurrentOwnerRpc(NetworkObjectReference source)      // Set owner on server
    {
        if (source.TryGet(out var networkObject))
        {
            var playerStatus = networkObject.GetComponent<PlayerStatus>();
            playerStatus.OnDeathOnServer.AddListener(HandleOnPlayerDieOnServer);
            CurrentOwner.Value = source;
        }
        else
        {
            HandleOnPlayerDieOnServer();
        }
    }

    private void HandleOnPlayerDieOnServer()
    {
        PlayerStatus playerStatus = null;
        if (CurrentOwner.Value.TryGet(out var networkObject))
        {
            var playerGO = networkObject.gameObject;
            // TODO: Release control of MountController
            playerStatus = networkObject.GetComponent<PlayerStatus>();
        }

        if (playerStatus)
            playerStatus.OnDeathOnServer.AddListener(HandleOnPlayerDieOnServer);

        CurrentOwner.Value = default;
    }

    /// <summary>
    /// Fixes the transform of the new owner to the mount and 
    /// releases/binds control of the input to the mount.
    /// (Executes when the CurrentOwner network variable is changed)
    /// </summary>
    /// <param name="prev">Prev value of CurrentOwner</param>
    /// <param name="next">New value of CurrentOwner</param>
    private void HandleCurrentOwnerChange(NetworkObjectReference prev, NetworkObjectReference next)
    {
        // 'next' Network Object reference will be relevant when mounting
        bool isMounting = next.TryGet(out NetworkObject nextNetObj);
        // 'prev' Network Object reference will be relevant when dismounting
        bool isDismounting = prev.TryGet(out NetworkObject prevNetObj);

        // Handle changes on server
        if (IsServer)
        {
            if (isMounting)         // Player is new owner
            {
                // Mount the player
                nextNetObj.transform.SetParent(transform);                  // Set parent on server
                NetworkObject.ChangeOwnership(nextNetObj.OwnerClientId);    // Owner is client
                if (_debug) Debug.Log("Mount Interaction: Parent on Server.");
            }
            else if (isDismounting) // Player is prev owner
            {
                // Dismount the player
                prevNetObj.transform.SetParent(null);   // Release parent on server
                NetworkObject.ChangeOwnership(0);       // Owner is now server
                if (_debug) Debug.Log("Mount Interaction: Release parent on Server.");
            }
        }

        // Handle changes on clients
        if (isMounting)
        {
            // Only run one instance of the coroutine at a time
            if (_mountingTransformCo != null)
                StopCoroutine(_mountingTransformCo);

            // Parent player then reset position
            _mountingTransformCo = StartCoroutine(MountingTransformCo(nextNetObj.transform));

            //TogglePlayerInput(prevNetObj, false);

            if (_debug) Debug.Log("Mount Interaction: Transform on Client.");
        }
        else if (isDismounting)
        {
            // Enable player movement and release from mount
            //TogglePlayerInput(prevNetObj, true);
        }
    }

    /*
    /// <summary>
    /// Releases/binds control of the player movement to input.
    /// </summary>
    /// <param name="source">Player Network Object</param>
    /// <param name="toggle">Can move = true, Cannot move = false</param>
    private void TogglePlayerInput(NetworkObject source, bool toggle)
    {
        if (source.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            if (_debug) Debug.Log($"Mount Interaction: Input Toggle = {toggle}");
        }
        else
            Debug.LogError($"Mount Interaction Error: Disabling Movement Input, PlayerController could not be found.");
    }
    */

    public void SetCanMount(bool value)
    {
        CanMount = value;
        Debug.Log($"CanMount = {value}");
    }

    /// <summary>
    /// Need to wait for parenting before resetting position
    /// </summary>
    /// <param name="transform">Player transform</param>
    private IEnumerator MountingTransformCo(Transform transform)
    {
        if (_debug) Debug.Log("Mount Interaction: Waiting For Parent");
        yield return new WaitUntil(() => transform.parent != null);     // Parent player under mount
        transform.localPosition = _mountOffset;    // Reset player position
        _mountingTransformCo = null;        // Reset coroutine
        if (_debug) Debug.Log($"Mount Interaction: Parenting Done, player offset relative to parent = {transform.localPosition}");
    }
}

