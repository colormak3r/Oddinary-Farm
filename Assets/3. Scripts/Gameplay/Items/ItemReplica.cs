using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemReplica : NetworkBehaviour, INetworkObjectPoolBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private ItemProperty mockProperty;
    [SerializeField]
    private float pickupRadius = 5f;
    [SerializeField]
    private float pickupSpeed = 10f;
    [SerializeField]
    private float pickupRecovery = 3f;
    [SerializeField]
    private float pickupDelay = 1f;
    [SerializeField]
    private LayerMask playerLayer;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    private NetworkVariable<ItemProperty> CurrentItemProperty = new NetworkVariable<ItemProperty>();
    private NetworkVariable<NetworkObjectReference> Owner = new NetworkVariable<NetworkObjectReference>();
    private Transform serverIgnorePicker;
    private Transform serverPreferredPicker;
    private float nextPickupTime = 0f;
    private Vector3 pickupOffset = new Vector3(0, 0.25f); // Offset to apply when picking up

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D itemRigidbody;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemRigidbody = GetComponent<Rigidbody2D>();
    }

    public void NetworkSpawn()
    {
        // Reset server variables
        serverPreferredPicker = null;
        serverIgnorePicker = null;
        nextPickupTime = Time.time + pickupDelay;

        // Reset local variables
        velocity = Vector2.zero;
        targetRigidbody = null;
        localPreferredTarget = null;
    }

    public void NetworkDespawn()
    {
        if (IsServer)
        {
            NetworkObject.ChangeOwnership(0);
            Owner.Value = default;
        }

        StopAllCoroutines();
    }

    public override void OnNetworkSpawn()
    {
        CurrentItemProperty.OnValueChanged += HandlePropertyChanged;
        HandlePropertyChanged(null, CurrentItemProperty.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentItemProperty.OnValueChanged -= HandlePropertyChanged;
    }

    private void HandlePropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        if (newValue == null) return;
        spriteRenderer.sprite = newValue.IconSprite;
    }

    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        CurrentItemProperty.Value = mockProperty;
    }

    public void SetProperty(ItemProperty property)
    {
        CurrentItemProperty.Value = property;
        AddRandomForceRpc();
    }

    public void PreferPickerOnServer(Transform preferred)
    {
        if (preferCoroutine != null) StopCoroutine(preferCoroutine);
        preferCoroutine = StartCoroutine(PreferPickerCoroutine(preferred));
    }

    private Coroutine preferCoroutine;
    private IEnumerator PreferPickerCoroutine(Transform preferred)
    {
        while (Time.time < nextPickupTime) yield return null;
        var preferredNetworkObject = preferred.GetComponent<NetworkObject>();
        if (preferredNetworkObject != null && preferredNetworkObject.IsPlayerObject)
        {
            // Ownership request change on the server
            NetworkObject.ChangeOwnership(preferredNetworkObject.OwnerClientId);
            // Send target information to the to-be-owner ahead of time
            PreferPickerOwnerRpc(preferredNetworkObject, RpcTarget.Single(preferredNetworkObject.OwnerClientId, RpcTargetUse.Temp));
            // Track the preferred picker on the server
            serverPreferredPicker = preferred;
        }
        else
        {
            Debug.LogError("Preferred transform does not have a NetworkObject component or is not Player object.", this);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void PreferPickerOwnerRpc(NetworkObjectReference preferredRef, RpcParams param)
    {
        if (preferredRef.TryGet(out var preferredObj))
        {
            localPreferredTarget = preferredObj.transform;
        }
    }

    private Transform localPreferredTarget;
    private Rigidbody2D targetRigidbody;
    private Vector2 velocity;
    private float nextScan;
    private void FixedUpdate()
    {
        if (IsServer && Time.time > nextScan && serverPreferredPicker == null)
        {
            var player = ScanForPlayer();
            if (player != null) PreferPickerOnServer(player.transform);
        }

        if (IsOwner)
        {
            if (Time.time > nextPickupTime && localPreferredTarget != null)
            {
                var targetPosition = localPreferredTarget.position + pickupOffset;
                if (targetRigidbody == null) targetRigidbody = localPreferredTarget.GetComponent<Rigidbody2D>();

                var direction = (targetPosition - transform.position).normalized;
                var sqrDistance = (transform.position - targetPosition).sqrMagnitude;
                if (sqrDistance > 0.1f)
                {
                    // Lerp to the target for consistent movement
                    Vector3 targetVelocity = targetRigidbody != null ? targetRigidbody.linearVelocity : Vector2.zero;
                    transform.position = Vector3.Lerp(transform.position, targetPosition + targetVelocity * 0.1f, Time.deltaTime * pickupSpeed);
                    velocity = direction * pickupSpeed;
                }
                else
                {
                    if (localPreferredTarget.TryGetComponent(out PlayerInventory playerInventory)
                        && playerInventory.AddItem(CurrentItemProperty.Value))
                    {
                        // Item successfully picked up
                        DespawnRpc();
                    }
                    else
                    {
                        // Failed to pick up item, notify server
                        FailToPickupRpc(localPreferredTarget.gameObject);

                        // Use the velocity to continute the motion
                        itemRigidbody.linearVelocity = velocity;
                    }

                    // Reset the local preferred target
                    velocity = Vector2.zero;
                    targetRigidbody = null;
                    localPreferredTarget = null;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void FailToPickupRpc(NetworkObjectReference targetRef)
    {
        if (IsServer)
        {
            serverPreferredPicker = null;
            NetworkObject.ChangeOwnership(0);
            if (targetRef.TryGet(out var targetObj))
            {
                IgnorePickerOnServer(targetObj.transform);
            }
            else
            {
                Debug.LogError($"Failed to find target object {targetRef}", this);
            }
        }

        nextPickupTime = Time.time + pickupRecovery;
    }

    public void IgnorePickerOnServer(Transform ignore)
    {
        serverIgnorePicker = ignore;
        //if (showDebugs) 
        Debug.Log($"Ignoring picker {ignore.name}", this);
    }

    private GameObject ScanForPlayer()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, playerLayer);
        if (hits.Length == 0)
        {
            serverIgnorePicker = null;
            return null;
        }

        float closetDistance = float.MaxValue;
        int closestIndex = -1;
        bool foundIgnorePicker = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == serverIgnorePicker)
            {
                foundIgnorePicker = true;
                continue;
            }

            var distance = Vector2.Distance(hits[i].transform.position, transform.position);
            if (distance < closetDistance)
            {
                closetDistance = distance;
                closestIndex = i;
            }
        }

        if (!foundIgnorePicker)
        {
            serverIgnorePicker = null;
        }

        if (closestIndex == -1) return null;
        return hits[closestIndex].gameObject;
    }

    [Rpc(SendTo.Server)]
    private void DespawnRpc()
    {
        NetworkObjectPool.Main.Despawn(gameObject);
    }

    [Rpc(SendTo.Owner)]
    private void AddRandomForceRpc()
    {
        AddRandomForce();
    }

    private void AddRandomForce()
    {
        itemRigidbody.AddForce(Random.insideUnitCircle * Random.Range(5, 10), ForceMode2D.Impulse);
    }
}

/*using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemReplica : NetworkBehaviour, INetworkObjectPoolBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private ItemProperty mockProperty;
    [SerializeField]
    private float pickupRadius = 5f;
    [SerializeField]
    private float pickupSpeed = 20f;
    [SerializeField]
    private float pickupTimeout = 3f;
    [SerializeField]
    private float pickupRecovery = 3f;
    [SerializeField]
    private float pickupDelay = 1f;
    [SerializeField]
    private GameObject graphicObject;
    [SerializeField]
    private LayerMask playerLayer;

    [Header("Initial Force Settings")]
    private Vector2 velocity;
    [SerializeField]
    private float speed = 5f;
    [SerializeField]
    private float drag = 0.95f;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    private NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();
    private NetworkVariable<NetworkObjectReference> Owner = new NetworkVariable<NetworkObjectReference>();
    private Transform ignorePicker;
    private float nextPickupTime = 0f;
    private float nextScanTime = 0f;
    private bool canBePickedup = true;
    private bool pickuppreferred = false;
    private Vector3 pickupOffset = new Vector3(0, 0.75f); // Offset to apply when picking up

    private int spawnTracker = 0;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D itemRigidbody;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemRigidbody = GetComponent<Rigidbody2D>();
    }

    public void NetworkSpawn()
    {
        ignorePicker = null;
        nextPickupTime = Time.time + pickupDelay;
        canBePickedup = true;
        pickuppreferred = false;
        spawnTracker++;


        velocity = Random.insideUnitCircle.normalized * speed;
    }

    public void NetworkDespawn()
    {
        if (IsServer)
        {
            NetworkObject.ChangeOwnership(0);
            Owner.Value = default;
        }

        graphicObject.transform.localPosition = Vector3.zero;
    }

    public override void OnNetworkSpawn()
    {
        Property.OnValueChanged += HandlePropertyChanged;
        HandlePropertyChanged(null, Property.Value);
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;
    }

    private void HandlePropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        if (newValue == null) return;
        spriteRenderer.sprite = newValue.IconSprite;
    }

    public void SetProperty(ItemProperty property)
    {
        Property.Value = property;
    }

    private float nextPreferSendTime = 0f;
    public void PreferPickerOnServer(Transform preferred, Vector2 spawnPosition)
    {
        //PreferPickerRpc(preferred.gameObject, spawnPosition);
        // TODO: ask for confirmation on the client first before sending info to all clients
        // TODO: UI restacking
        if (Time.time < nextPreferSendTime) return;
        nextPreferSendTime = Time.time + 2f; // Prevent spamming

        if (preferred.TryGetComponent<NetworkObject>(out var networkObject))
            PreferPickerAssignmentRpc(networkObject, spawnPosition, RpcTarget.Single(networkObject.OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void PreferPickerAssignmentRpc(NetworkObjectReference preferredPref, Vector2 spawnPosition, RpcParams param)
    {
        if (preferredPref.TryGet(out var preferredObj) &&
            preferredObj.TryGetComponent<PlayerInventory>(out var inventory))
        {
            if (inventory.CanAddItem(Property.Value))
            {
                PreferPickerRpc(preferredObj, spawnPosition);
            }
            else
            {
                IgnorePickerRpc(preferredObj.gameObject);
            }
        }
    }

    private NetworkObject localPreferredTarget;
    [Rpc(SendTo.Everyone)]
    private void PreferPickerRpc(NetworkObjectReference preferredPref, Vector2 spawnPosition)
    {
        if (showDebugs) Debug.Log($"Prefer picker requested by {NetworkManager.Singleton.LocalClientId} for {Property.Value.ItemName}", this);
        if (!preferredPref.TryGet(out var preferredObj)) return;
        localPreferredTarget = preferredObj;
        graphicObject.SetActive(true);
        graphicObject.transform.position = spawnPosition;
    }

    [Rpc(SendTo.Server)]
    private void IgnorePickerRpc(NetworkObjectReference ignoreRef)
    {
        if (ignoreRef.TryGet(out var ignoreObj))
        {
            IgnorePickerOnServer(ignoreObj.transform);
        }
        else
        {
            Debug.LogError($"Failed to find ignore object {ignoreRef}", this);
        }
    }

    public void IgnorePickerOnServer(Transform ignore)
    {
        if (showDebugs) Debug.Log($"Ignoring picker {ignore.name}", this);
        ignorePicker = ignore;
    }

    private void Update()
    {
        if (Time.time > nextPickupTime)
        {
            if (localPreferredTarget != null)
            {
                var destination = localPreferredTarget.transform.position + pickupOffset;
                graphicObject.transform.position = Vector3.Lerp(graphicObject.transform.position, destination, Time.deltaTime * pickupSpeed);
                if (Vector3.SqrMagnitude(graphicObject.transform.position - destination) < 0.1f)
                {
                    if (localPreferredTarget.IsLocalPlayer) //preferredTarget.IsOwner && preferredTarget.IsPlayerObject
                    {
                        if (localPreferredTarget.TryGetComponent<PlayerInventory>(out var inventory))
                        {
                            if (inventory.AddItem(Property.Value))
                            {
                                DespawnRpc();
                            }
                            else
                            {
                                // This should not happened since we check if the player can add the item before sending the prefer picker request
                                Debug.LogError($"Failed to add item {Property.Value.ItemName} to inventory of {localPreferredTarget.name}", this);
                                //FailToPickupRpc();
                            }
                        }
                        else
                        {
                            Debug.LogError($"preferred target {localPreferredTarget.name} does not have PlayerInventory component.");
                        }
                    }
                    else
                    {
                        if (showDebugs) Debug.LogError($"preferred target {localPreferredTarget.name} is not a local player.");
                    }

                    graphicObject.SetActive(false);
                    localPreferredTarget = null;
                }
            }
            else if (IsServer)
            {
                if (showDebugs) Debug.Log($"Scanning for player to pickup {Property.Value.ItemName}", this);
                var player = ScanForPlayer();
                if (player) PreferPickerOnServer(player.transform, graphicObject.transform.position);
            }
        }
        else
        {
            //Debug.Log("Simulating drags");
            // Apply drag
            velocity *= drag;

            // Simulate random force to make the item move around
            graphicObject.transform.position += (Vector3)(velocity * Time.deltaTime);
        }
    }

    private GameObject ScanForPlayer()
    {
        var hits = Physics2D.OverlapCircleAll(graphicObject.transform.position, pickupRadius, playerLayer);
        if (hits.Length == 0)
        {
            ignorePicker = null;
            return null;
        }

        float closetDistance = float.MaxValue;
        int closestIndex = -1;
        bool foundIgnorePicker = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == ignorePicker)
            {
                foundIgnorePicker = true;
                continue;
            }

            var distance = Vector2.Distance(hits[i].transform.position, transform.position);
            if (distance < closetDistance)
            {
                closetDistance = distance;
                closestIndex = i;
            }
        }

        if (!foundIgnorePicker)
        {
            if (showDebugs) Debug.Log("No ignore picker found, resetting ignorePicker", this);
            ignorePicker = null;
        }

        if (closestIndex == -1) return null;
        return hits[closestIndex].gameObject;
    }

    [Rpc(SendTo.Server)]
    private void DespawnRpc()
    {
        if (showDebugs) Debug.Log($"Despawning {Property.Value.ItemName}", this);
        NetworkObjectPool.Main.Despawn(gameObject);
        localPreferredTarget = null;
        ignorePicker = null;
    }

    [Rpc(SendTo.Server)]
    private void FailToPickupRpc()
    {
        if (showDebugs) Debug.LogError($"{localPreferredTarget.name} failed to pickup {Property.Value.ItemName}", this);
        IgnorePickerOnServer(localPreferredTarget.transform);
        localPreferredTarget = null;
    }

    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        Property.Value = mockProperty;
    }
}*/

/*using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemReplica : NetworkBehaviour, INetworkObjectPoolBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private ItemProperty mockProperty;
    [SerializeField]
    private float pickupRadius = 5f;
    [SerializeField]
    private float pickupSpeed = 20f;
    [SerializeField]
    private float pickupTimeout = 3f;
    [SerializeField]
    private float pickupRecovery = 3f;
    [SerializeField]
    private float pickupDelay = 1f;
    [SerializeField]
    private LayerMask playerLayer;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    private NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();
    private NetworkVariable<NetworkObjectReference> Owner = new NetworkVariable<NetworkObjectReference>();
    private Transform ignorePicker;
    private float nextPickupTime = 0f;
    private float nextScanTime = 0f;
    private bool canBePickedup = true;
    private bool pickuppreferred = false;
    private Vector3 pickupOffset = new Vector3(0, 0.75f); // Offset to apply when picking up
    private Coroutine pickupCoroutine;
    private Coroutine ignoreCoroutine;
    private Coroutine preferCoroutine;
    private Coroutine ownershipCoroutine;

    private int spawnTracker = 0;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D itemRigidbody;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemRigidbody = GetComponent<Rigidbody2D>();
    }


    public void NetworkSpawn()
    {
        ignorePicker = null;
        nextPickupTime = Time.time + pickupDelay;
        canBePickedup = true;
        pickuppreferred = false;
        spawnTracker++;
    }

    public void NetworkDespawn()
    {
        if (IsServer)
        {
            NetworkObject.ChangeOwnership(0);
            Owner.Value = default;
        }

        StopAllCoroutines();
    }

    public override void OnNetworkSpawn()
    {
        Property.OnValueChanged += HandlePropertyChanged;
        HandlePropertyChanged(null, Property.Value);
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (!IsOwner) return;
        if (showDebugs) Debug.Log($"Ownership changed from {previous} to {current}", this);

        if (ownershipCoroutine != null) StopCoroutine(ownershipCoroutine);
        ownershipCoroutine = StartCoroutine(OwnershipConfirmationCoroutine(current));
    }

    private IEnumerator OwnershipConfirmationCoroutine(ulong ownerId)
    {
        if (showDebugs) Debug.Log($"Confirming ownership for client {ownerId}", this);
        var nextExitTime = Time.time + pickupTimeout;

        NetworkObject ownerNetObj;
        while (!Owner.Value.TryGet(out ownerNetObj))
        {
            if (Time.time > nextExitTime) yield break;
            yield return null;
        }

        if (ownerNetObj == NetworkObject) Debug.LogError($"Ownership confirmation failed for {ownerNetObj.name} - object is self, spawned: {spawnTracker}", this);

        if (ownerId == ownerNetObj.OwnerClientId)
        {
            if (showDebugs) Debug.Log($"Ownership confirmed for {ownerNetObj.name}", this);
            if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
            pickupCoroutine = StartCoroutine(PickupCoroutine(ownerNetObj.transform, pickupTimeout));
        }
        else
        {
            if (Time.time > nextExitTime && ownerId != 0)
            {
                Debug.LogError($"Ownership confirmation timed out for {ownerNetObj.name}");
            }
        }
    }

    private void HandlePropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        if (newValue == null) return;
        spriteRenderer.sprite = newValue.IconSprite;
    }

    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        Property.Value = mockProperty;
    }

    public void SetProperty(ItemProperty property)
    {
        Property.Value = property;
        AddRandomForceRpcRpc();
    }

    private void Update()
    {
        if (IsServer && canBePickedup && !pickuppreferred && Time.time > nextPickupTime && Time.time > nextScanTime)
        {
            nextScanTime = Time.time + 0.1f;
            var player = ScanForPlayer();
            if (player != null) PickupOnServer(player.transform);
        }
    }

    public void PreferPickerOnServer(Transform preferred)
    {
        canBePickedup = false;
        pickuppreferred = true;
        if (preferCoroutine != null) StopCoroutine(preferCoroutine);
        preferCoroutine = StartCoroutine(PreferCoroutine(preferred));
    }

    private IEnumerator PreferCoroutine(Transform preferred)
    {
        yield return new WaitUntil(() => Time.time > nextPickupTime);
        PickupOnServer(preferred);
    }

    private void PickupOnServer(Transform picker)
    {
        if (showDebugs) Debug.Log($"Pickup requested by {picker}", this);
        Owner.Value = picker.gameObject;
        var pickerOwnerId = picker.GetComponent<NetworkObject>().OwnerClientId;
        if (pickerOwnerId == OwnerClientId)
        {
            if (showDebugs) Debug.Log($"{picker} has same owner", this);
            if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
            pickupCoroutine = StartCoroutine(PickupCoroutine(picker, pickupTimeout));
        }
        else
        {
            if (showDebugs) Debug.Log($"Picked up by {picker}", this);
            NetworkObject.ChangeOwnership(pickerOwnerId);
        }

        canBePickedup = false;
    }

    private IEnumerator PickupCoroutine(Transform picker, float duration)
    {
        if (showDebugs) Debug.Log($"Picked up by {picker}", picker);

        var pickerPos = picker.position + pickupOffset;
        var endTime = Time.time + duration;
        var sqrDistance = (transform.position - pickerPos).sqrMagnitude;
        while (sqrDistance > 0.01f)
        {
            if (Time.time > endTime) yield break;

            var direction = (pickerPos - transform.position).normalized;
            itemRigidbody.linearVelocity = direction * pickupSpeed;

            sqrDistance = (transform.position - pickerPos).sqrMagnitude;
            if (sqrDistance < 0.04f) break;

            yield return new WaitForFixedUpdate();
        }

        var inventory = picker.GetComponent<PlayerInventory>();
        if (inventory.AddItem(Property.Value))
        {
            DespawnRpc();
        }
        else
        {
            FailToPickupRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void FailToPickupRpc()
    {
        if (Owner.Value.TryGet(out var ownerNetObj))
        {
            IgnorePickerOnServer(ownerNetObj.transform);
        }

        Owner.Value = default;
        NetworkObject.ChangeOwnership(0);
        canBePickedup = true;
        nextPickupTime = Time.time + pickupRecovery;
    }

    public void IgnorePickerOnServer(Transform ignore)
    {
        ignorePicker = ignore;
        *//*if (ignoreCoroutine != null) StopCoroutine(ignoreCoroutine);
        ignoreCoroutine = StartCoroutine(IgnorePickerCoroutine());*//*
    }

    private IEnumerator IgnorePickerCoroutine()
    {
        yield return new WaitForSeconds(pickupRecovery);
        ignorePicker = null;
    }

    private GameObject ScanForPlayer()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, playerLayer);
        if (hits.Length == 0)
        {
            ignorePicker = null;
            return null;
        }

        float closetDistance = float.MaxValue;
        int closestIndex = -1;
        bool foundIgnorePicker = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == ignorePicker)
            {
                foundIgnorePicker = true;
                continue;
            }

            var distance = Vector2.Distance(hits[i].transform.position, transform.position);
            if (distance < closetDistance)
            {
                closetDistance = distance;
                closestIndex = i;
            }
        }

        if (!foundIgnorePicker)
        {
            ignorePicker = null;
        }

        if (closestIndex == -1) return null;
        return hits[closestIndex].gameObject;
    }

    [Rpc(SendTo.Server)]
    private void DespawnRpc()
    {
        NetworkObjectPool.Main.Despawn(gameObject);
    }

    [Rpc(SendTo.Owner)]
    private void AddRandomForceRpcRpc()
    {
        AddRandomForce();
    }

    private void AddRandomForce()
    {
        itemRigidbody.AddForce(Random.insideUnitCircle * Random.Range(5, 10), ForceMode2D.Impulse);
    }
}*/