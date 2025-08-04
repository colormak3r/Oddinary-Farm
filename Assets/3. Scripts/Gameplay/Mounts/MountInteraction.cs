/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   08/03/2025 (Khoa)
 * Notes:           Handles all interactions with a mount
 *                  
 *                  [--------------------------Mount Object-------------------------]     [------------Player Object-------------]
 *                  [HotAirBalloon(Structure) -> MountInteraction -> MountController] <-> [PlayerMountHandler <- PlayerController] 
 *                  
 *                  [Mount Object--------------------------------------------------------------------------------------]
 *                  Structure:          Physical object that can be mounted, does not handle mounting.
 *                                      Can be Animal, Structure. Sanity check and dictate if the player can mount it.
 *                  MountInteraction:   Source of truth for mount and dismount, does not handle movement or graphics.
 *                                      All other scripts react to this script's events.
 *                  MountController:    Handles movement of the mount, can be specialized for different mount types,
 *                                      apply constraints to movement sent from PlayerMountHandler
 *  
 *                  [Player Object-------------------------------------------------------------------------------------]
 *                  PlayerMountHandler: Reacts to mounting and dismounting events, manages player state while mounted
 *                  PlayerController:   Only handles player inputs, does not know about the mount.
*/
using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles all interactions with a mount.
/// Releases/binds the controls of the player and mount.
/// Handles changes in player parenting when mounting.
/// </summary>
public class MountInteraction : NetworkBehaviour, IInteractable
{
    [Header("Debugging")]
    [SerializeField] private bool _debug = false;

    // Network variable to store the current owner of the mount
    [SerializeField]
    private NetworkVariable<NetworkObjectReference> CurrentOwner = new NetworkVariable<NetworkObjectReference>();
    public bool HasOwner => CurrentOwner.Value.TryGet(out NetworkObject _); // Check if there is a current owner

    // Network variable to determine if the mount can be interacted with
    [SerializeField]
    private NetworkVariable<bool> CanMount = new NetworkVariable<bool>(false); // Can the mount be interacted with?
    public bool CanMountValue => CanMount.Value; // Public getter for CanMount

    // Interface implementation
    public bool IsHoldInteractable => false;

    public Action<Transform> OnMountOnOwner;
    public Action<Transform> OnDismountOnOwner;
    public Action<Transform> OnMountOnServer;
    public Action<Transform> OnDismountOnServer;
    public Action<Transform> OnMountOnClient;      // Called when a player mounts; can be used to signal animal behaviors on client
    public Action<Transform> OnDismountOnClient;   // Called when a player dismounts; can be used to signal animal behaviors on client

    private SelectorModifier selectorModifier;

    private void Awake()
    {
        selectorModifier = GetComponent<SelectorModifier>();
    }

    public override void OnNetworkSpawn()
    {
        CurrentOwner.OnValueChanged += HandleCurrentOwnerChange;
        HandleCurrentOwnerChange(default, CurrentOwner.Value); // Initialize the current owner state, even for late joiners
    }

    public override void OnNetworkDespawn()
    {
        CurrentOwner.OnValueChanged -= HandleCurrentOwnerChange;
        DismountPlayer(); // Ensure the player is dismounted when the object is despawned
    }

    /// <summary>
    /// Handles all cases when a player interacts with the mount. 
    /// Sanity checks only, no logic execution inside.
    /// Case 1: New owner is detected, mount is free.
    /// Case 2: New owner is detected, mount is occupied.
    /// Case 3: Prev owner wants to release, mount is freed.
    /// </summary>
    /// <param name="source">Player's transform.</param>
    public void Interact(Transform source)
    {
        if (_debug) Debug.Log($"Mount Interaction: Interacted with {source.name} on {gameObject.name}");
        if (!CanMountValue) return;

        if (_debug) Debug.Log("Attempting Mount...");

        // Try getting a network object comp from the source
        if (!source.gameObject.TryGetComponent<NetworkObject>(out var sourceNetObj))
        {
            Debug.LogWarning("Mount Interaction Warning: Interacted source was not a NetworkObject");
            return;
        }

        if (CurrentOwner.Value.TryGet(out var ownerNetObj))     // There is already an owner
        {
            // Case 1
            if (ownerNetObj == sourceNetObj)        // Unmount
            {
                if (_debug) Debug.Log("Mount Interaction: Player has Unmounted");
                DismountPlayer();
            }
            // Case 2
            else                                // Cannot mount because there's already a mounter
            {
                if (_debug) Debug.Log("Mount Interaction: Already has a mounter.");
            }
        }
        // Case 3
        else                    // There is no owner of the object -> Mount
        {
            if (_debug) Debug.Log("Mount Interaction: Player has Mounted");
            MountPlayer(source);
            // TODO: Handle multiple seats; parent under new child objects
        }
    }

    #region Mounting and Dismounting
    public void MountPlayer(Transform source)
    {
        if (!CanMountValue)
        {
            Debug.LogWarning("Mount Interaction Warning: Cannot mount, CanMount is false.");
            return; // Cannot mount if CanMount is false
        }

        // Maybe check if the source is a valid player if they have PlayerStatus here
        // No one but the player can interact tho
        MountPlayerRpc(source.gameObject);
    }

    [Rpc(SendTo.Server)]
    private void MountPlayerRpc(NetworkObjectReference sourceRef)
    {
        if (!sourceRef.TryGet(out var sourceNetworkObject) || !sourceNetworkObject.IsSpawned)
        {
            Debug.LogWarning("Mount Interaction Warning: Source reference is not a valid NetworkObject or is not spawned.");
            return; // No owner to mount
        }

        SetCurrentOwnerOnServer(sourceNetworkObject); // Set owner on server
    }

    public void DismountPlayer()
    {
        if (IsSpawned) DismountPlayerRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void DismountPlayerRpc()    // Mirror MountPlayerRpc, but more relaxed since the owner can be null
    {
        SetCurrentOwnerOnServer(default);
    }
    #endregion

    /// <summary>
    /// Handles changes to the CurrentOwner NetworkVariable
    /// </summary>
    /// <param name="source">New owner reference</param>
    private void SetCurrentOwnerOnServer(NetworkObject networkObject)      // Set owner on server
    {
        if (!IsServer) return; // Only server can set the owner

        CurrentOwner.Value = networkObject == null ? new NetworkObjectReference() : networkObject;
    }

    /// <summary>
    /// Reparent the player and change the mount's ownership when mounting or dismounting.
    /// Send events to all clients and the server to handle the mount state change.
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
                if (_debug) Debug.Log("Mount Interaction: Parent on Server.");

                // Mount the player
                nextNetObj.transform.SetParent(transform);             // Set parent on server
                NetworkObject.ChangeOwnership(nextNetObj.OwnerClientId);    // Owner is client

                // Listen for player death
                // Use Get here to force the game to crash if the owner is not valid
                nextNetObj.GetComponent<PlayerStatus>().OnDeathOnServer.AddListener(DismountPlayer);

                // Notify the server that the player has mounted
                OnMountOnServer?.Invoke(nextNetObj.transform); // Notify server of mount
            }
            else if (isDismounting) // Player is prev owner
            {
                if (_debug) Debug.Log("Mount Interaction: Release parent on Server.");

                // Dismount the player
                prevNetObj.transform.SetParent(null);   // Release parent on server
                NetworkObject.ChangeOwnership(0);       // Owner is now server

                // Stop listening for player death
                prevNetObj.GetComponent<PlayerStatus>().OnDeathOnServer.RemoveListener(DismountPlayer);

                // Notify the server that the player has dismounted
                OnDismountOnServer?.Invoke(prevNetObj.transform); // Notify server of dismount
            }
        }

        // Handle changes on all clients
        if (isMounting)
        {
            if (_debug) Debug.Log("Mount Interaction: Transform on Client.");

            // Only run one instance of the coroutine at a time
            // Parent player then reset position
            // if (_mountingTransformCoroutine != null) StopCoroutine(_mountingTransformCoroutine);
            // _mountingTransformCoroutine = StartCoroutine(MountingTransformCoroutine(nextNetObj.transform));

            // Disable colliders/control of player
            if (nextNetObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)      // If the player is the owner
            {
                Debug.Log("Mount Interaction: Player is Mounting on Owner.");
                SetPlayerMountState(nextNetObj, true);

                // Reset player position relative to mount
                //nextNetObj.transform.localPosition = _mountOffset;

                // Notify the player that they have mounted
                OnMountOnOwner?.Invoke(nextNetObj.transform); // Notify owner of mount
            }

            // Disable selection of the mount while mounted
            if (selectorModifier)
            {
                Selector.Main.Show(false); // Hide selector
                selectorModifier.SetCanBeSelected(false);
            }

            // Notify all client that the player has mounted
            OnMountOnClient?.Invoke(nextNetObj.transform); // Notify client of mount
        }
        else if (isDismounting)
        {
            // Enable player movement and release from mount if there is one
            if (prevNetObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)     //  If the player is the owner
            {
                Debug.Log("Mount Interaction: Player is Dismounting on Owner.");
                SetPlayerMountState(prevNetObj, false);

                // Notify the player that they have dismounted
                OnDismountOnOwner?.Invoke(prevNetObj.transform); // Notify owner of dismount
            }

            // Enable selection of the mount when dismounted
            if (selectorModifier) selectorModifier.SetCanBeSelected(true);

            // Notify all clients that the player has dismounted
            OnDismountOnClient?.Invoke(prevNetObj.transform); // Notify client of dismount
        }
    }

    // Note: When the player dies, use DismountPlayer to release the mount
    /*private void HandleOnPlayerDieOnServer()
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
    }*/

    /// <summary>
    /// Releases/binds control of the player movement to input.
    /// </summary>
    /// <param name="playerNetObj"></param>
    /// <param name="isMounting"></param>
    private void SetPlayerMountState(NetworkObject playerNetObj, bool isMounting)
    {
        if (playerNetObj == null)
        {
            Debug.LogWarning("Mount Interaction: Source NetworkObject is null. Cannot toggle player input.");
            return; // Cannot toggle input if source is null
        }

        if (playerNetObj.TryGetComponent<PlayerMountHandler>(out var controller))
        {
            if (_debug) Debug.Log($"Mount Interaction: IsMounting = {isMounting}");
            controller.SetIsMounting(isMounting); // Enable or disable player movement input
        }
        else
        {
            Debug.LogError($"Mount Interaction Error: PlayerMountHandler could not be found.");
        }
    }

    #region Can Mount
    public void SetCanMount(bool value)
    {
        SetCanMountRpc(value);
    }

    [Rpc(SendTo.Server)]
    private void SetCanMountRpc(bool value)
    {
        CanMount.Value = value;
        Debug.Log($"CanMount = {value}");
    }
    #endregion

    // Note: Transform position logic moved to PlayerMountHandler.cs
    /*/// <summary>
    /// Need to wait for parenting before resetting position
    /// </summary>
    /// <param name="transform">Player transform</param>
    private IEnumerator MountingTransformCoroutine(Transform transform)
    {
        if (_debug) Debug.Log("Mount Interaction: Waiting For Parent");
        yield return new WaitUntil(() => transform.parent != null);     // Parent player under mount
        //transform.GetComponent<NetworkTransform>().Teleport(transform.position + _mountOffset, Quaternion.identity, Vector3.one);
        transform.localPosition = _mountOffset;    // Reset player position
        yield return new WaitForSeconds(0.1f); // Wait a bit to ensure the position is set correctly
        transform.localPosition = _mountOffset;
        _mountingTransformCoroutine = null;        // Reset coroutine
        if (_debug) Debug.Log($"Mount Interaction: Parenting Done, player offset relative to parent = {transform.localPosition}");
    }*/

    public void InteractionStart(Transform source)
    {
        throw new NotImplementedException();
    }

    public void InteractionEnd(Transform source)
    {
        throw new NotImplementedException();
    }
}

