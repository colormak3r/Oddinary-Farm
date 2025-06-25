/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   06/25/2025 (Ryan)
 * Notes:           Handles all interactions with a mount
*/
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles all interactions with a mount.
/// Releases/binds the controls of the player and mount.
/// Handles changes in player transform when mounting.
/// </summary>
public class MountInteraction : NetworkBehaviour
{
    [Header("Events")]
    [SerializeField] public UnityEvent OnMount;       // Called when a player mounts; can be used to signal animal behaviors 
    [SerializeField] public UnityEvent OnDismount;    // Called when a player dismounts; can be used to signal animal behaviors 

    [Header("Settings")]
    [SerializeField] private Vector3 _mountOffset;     // Offset determines where the player stands relative to the mount

    [Header("Debugging")]
    [SerializeField] private bool _debug = false;

    private NetworkVariable<NetworkObjectReference> CurrentOwner = new NetworkVariable<NetworkObjectReference>();
    private Coroutine _mountingTransformCo;

    public override void OnNetworkSpawn()
    {
        PlayerController.OnPlayerInteract += Interact;
        CurrentOwner.OnValueChanged += HandleCurrentOwnerChange;
    }

    public override void OnNetworkDespawn()
    {
        PlayerController.OnPlayerInteract -= Interact;
        CurrentOwner.OnValueChanged -= HandleCurrentOwnerChange;
    }

    /// <summary>
    /// Handles all cases when a player interacts with the mount .
    /// Case 1: New owner is detected, mount is free
    /// Case 2: New owner is detected, mount is occupied
    /// Case 3: Prev owner wants to release, mount is freed
    /// </summary>
    /// <param name="source">Player's NetworkObject</param>
    public void Interact(NetworkObject source)
    {
        if (CurrentOwner.Value.TryGet(out NetworkObject networkObject))     // There is already an owner
        {
            // Case 1
            if (networkObject == source)        // Unmount
            {
                if (_debug) Debug.Log("Player has Unmounted");
                SetCurrentOwnerRpc(default);        // Change CurrentOwner Network Variable
                OnDismount?.Invoke();
            }
            // Case 2
            else                                // Cannot mount because there's already a mounter
                if (_debug) Debug.Log("Already has a mounter.");
        }
        // Case 3
        else                    // There is no owner of the object -> Mount
        {
            if (_debug) Debug.Log("Player has Mounted");
            SetCurrentOwnerRpc(source);        // Change CurrentOwner Network Variable
            OnMount?.Invoke();
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
        CurrentOwner.Value = source;
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
                if (_debug) Debug.Log("Parent on Server.");
            }
            else if (isDismounting) // Player is prev owner
            {
                // Dismount the player
                prevNetObj.transform.SetParent(null);   // Release parent on server
                NetworkObject.ChangeOwnership(0);       // Owner is now server
                if (_debug) Debug.Log("Release parent on Server.");
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

            // Disable player movement and give to mount
            TogglePlayerMovement(nextNetObj, false);
            if (_debug) Debug.Log("Transform on Client.");
        }
        else if (isDismounting)
            // Enable player movement and release from mount
            TogglePlayerMovement(prevNetObj, true);
    }

    /// <summary>
    /// Releases/binds control of the player movement to input.
    /// </summary>
    /// <param name="source">Player Network Object</param>
    /// <param name="toggle">Can move = true, Cannot move = false</param>
    private void TogglePlayerMovement(NetworkObject source, bool toggle)
    {
        if (source.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            player.TogglePlayerMovement(toggle);
            if (_debug) Debug.Log($"Move Input Toggle = {toggle}");
        }
        else
            Debug.LogError($"Error Disabling Movement Input, PlayerController could not be found.");
    }

    /// <summary>
    /// Need to wait for parenting before resetting position
    /// </summary>
    /// <param name="transform">Player transform</param>
    private IEnumerator MountingTransformCo(Transform transform)
    {
        if (_debug) Debug.Log("Waiting For Parent");
        yield return new WaitUntil(() => transform.parent != null);     // Parent player under mount
        transform.localPosition = _mountOffset;    // Reset player position
        _mountingTransformCo = null;        // Reset coroutine
        if (_debug) Debug.Log($"Parenting Done: {transform.localPosition}");
    }
}

