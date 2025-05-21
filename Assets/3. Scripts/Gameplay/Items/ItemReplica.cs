using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;  // Use Unity Engine's Random not System.Collection's Random

public class ItemReplica : NetworkBehaviour, INetworkObjectPoolBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private ItemProperty mockProperty;      // QUESTION: Why mock again?
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

    private NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();       // Networked item data
    private NetworkVariable<NetworkObjectReference> Owner = new NetworkVariable<NetworkObjectReference>();
    private Transform ignorePicker;
    private float nextPickupTime = 0f;
    private float nextScanTime = 0f;
    private bool canBePickedup = true;
    private bool pickupPrefered = false;
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


    public void NetworkSpawn()      // Item spawned in world
    {
        ignorePicker = null;
        nextPickupTime = Time.time + pickupDelay;
        canBePickedup = true;
        pickupPrefered = false;
        spawnTracker++;                 // QUESTION: Is this for object pooling?
    }

    public void NetworkDespawn()        // Item despawned
    {
        if (IsServer)
        {
            NetworkObject.ChangeOwnership(0);       // Assign ownership to host
            Owner.Value = default;          // No player owner assigned
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

    // NOTE: Consider changing args to "prevOwner" and "newOwner"; it is a bit confusing at first glance
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (!IsOwner) return;       // If client is already owner then no need to transfer ownership
        if (showDebugs) Debug.Log($"Ownership changed from {previous} to {current}", this);

        if (ownershipCoroutine != null) 
            StopCoroutine(ownershipCoroutine);

        ownershipCoroutine = StartCoroutine(OwnershipConfirmationCoroutine(current));
    }

    private IEnumerator OwnershipConfirmationCoroutine(ulong ownerId)
    {
        if (showDebugs) Debug.Log($"Confirming ownership for client {ownerId}", this);
        var nextExitTime = Time.time + pickupTimeout;

        NetworkObject ownerNetObj;
        while (!Owner.Value.TryGet(out ownerNetObj))        // Stop coroutine if running for too long
        {
            if (Time.time > nextExitTime)       // If timeout -> break loop
                yield break;

            yield return null;
        }

        if (ownerNetObj == NetworkObject) Debug.LogError($"Ownership confirmation failed for {ownerNetObj.name} - object is self, spawned: {spawnTracker}", this);

        if (ownerId == ownerNetObj.OwnerClientId)       // Ownership confirmed
        {
            if (showDebugs) Debug.Log($"Ownership confirmed for {ownerNetObj.name}", this);
            if (pickupCoroutine != null) 
                StopCoroutine(pickupCoroutine);

            pickupCoroutine = StartCoroutine(PickupCoroutine(ownerNetObj.transform, pickupTimeout));
        }
        else                        // Timeout error
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
        AddRandomForceRpcRpc();     // QUESTION: Why add force here?
    }

    private void Update()
    {
        if (IsServer && canBePickedup && !pickupPrefered && Time.time > nextPickupTime && Time.time > nextScanTime)
        {
            nextScanTime = Time.time + 0.1f;
            var player = ScanForPlayer();

            // If player in vacinity then start pickup coroutine
            if (player != null) 
                PickupOnServer(player.transform);
        }
    }

    public void PreferPickerOnServer(Transform prefered)
    {
        canBePickedup = false;
        pickupPrefered = true;
        if (preferCoroutine != null) StopCoroutine(preferCoroutine);
        preferCoroutine = StartCoroutine(PreferCoroutine(prefered));
    }

    private IEnumerator PreferCoroutine(Transform prefered)
    {
        yield return new WaitUntil(() => Time.time > nextPickupTime);
        PickupOnServer(prefered);
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

    // NOTE: Consider naming the first arg. "ownerTranform" or "playerTransform"; it is a bit confusing at first glance
    // NOTE: Consider naming the second arg. "timeout" or "timeoutDuration"; it is a bit confusing at first glance
    private IEnumerator PickupCoroutine(Transform picker, float duration)
    {
        if (showDebugs) Debug.Log($"Picked up by {picker}", picker);

        var pickerPos = picker.position;
        var endTime = Time.time + duration;
        var sqrDistance = (transform.position - pickerPos).sqrMagnitude;        // Squared distance for perfomant computation
        while (sqrDistance > 0.01f)     // Suck into player
        {
            if (Time.time > endTime) yield break;       // Timed out

            var direction = (pickerPos - transform.position).normalized;
            itemRigidbody.linearVelocity = direction * pickupSpeed;

            sqrDistance = (transform.position - pickerPos).sqrMagnitude;
            if (sqrDistance < 0.04f) break;

            yield return new WaitForFixedUpdate();
        }

        // Store in inventory
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
    private void FailToPickupRpc()      // Reset ownership to server if pickup failed
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

    // NOTE: Consider naming the arg. "ignoredOwnerTranform"; it is a bit confusing at first glance
    public void IgnorePickerOnServer(Transform ignore)
    {
        ignorePicker = ignore;
        if (ignoreCoroutine != null) 
            StopCoroutine(ignoreCoroutine);

        ignoreCoroutine = StartCoroutine(IgnorePickerCoroutine());
    }

    private IEnumerator IgnorePickerCoroutine()     // Delay before retrying pickup
    {
        yield return new WaitForSeconds(pickupRecovery);
        ignorePicker = null;
    }

    // Return closest player object that falls within the pickup radius
    private GameObject ScanForPlayer()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, playerLayer);       // Player in radius
        if (hits.Length == 0) // No players found in area 
            return null;

        // Find the closest player and 
        float closetDistance = float.MaxValue;      // QUESTION: Why not use Mathf.Infinity?
        int closestIndex = -1;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == ignorePicker)  // If player marked as ignore
                continue;

            var distance = Vector2.Distance(hits[i].transform.position, transform.position);
            if (distance < closetDistance)
            {
                closetDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex == -1) 
            return null;

        return hits[closestIndex].gameObject;
    }

    [Rpc(SendTo.Server)]
    private void DespawnRpc()       // Remove item from item pool
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
}


/*using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemReplica : NetworkBehaviour, INetworkObjectPoolBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private ItemProperty mockProperty;
    [SerializeField]
    private float pickupSpeed = 10f;
    [SerializeField]
    private float pickupDuration = 3f;
    [SerializeField]
    private float pickupRecovery = 1f;
    [SerializeField]
    private Vector3 targetOffset = new Vector3(0, 0.25f);

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();
    private ItemProperty currentProperty;
    public ItemProperty CurrentProperty => currentProperty;


    [SerializeField]
    private Transform ignorePicker;

    [SerializeField]
    private NetworkVariable<NetworkObjectReference> Owner = new NetworkVariable<NetworkObjectReference>();
    public NetworkObject OwnerValue => Owner.Value;

    [SerializeField]
    private NetworkVariable<bool> CanBePickup = new NetworkVariable<bool>(false);
    public bool CanBePickupValue => CanBePickup.Value;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D itemRigidbody;

    private Coroutine spawnCoroutine;
    private Coroutine pickupCoroutine;
    private Coroutine recoveryCoroutine;
    private Coroutine ignoreCoroutine;
    private Coroutine timeoutCoroutine;

    private bool isWokenUp = false;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemRigidbody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        AddRandomForce();
        HandlePropertyChanged(null, Property.Value);
        Property.OnValueChanged += HandlePropertyChanged;
        Owner.OnValueChanged += HandleOwnerChanged;

        spawnCoroutine = StartCoroutine(SpawnCoroutine());
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;
        Owner.OnValueChanged -= HandleOwnerChanged;
    }

    private void HandleOwnerChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {
        if (!newValue.TryGet(out var pickerNetObj)) return;

        if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
        if (pickerNetObj.OwnerClientId == NetworkManager.LocalClientId)
        {
            if (pickerNetObj.transform == transform) return;
            pickupCoroutine = StartCoroutine(PickupCoroutine(pickerNetObj.transform, pickupDuration));
            if (showDebugs) Debug.Log($"Picked up by {pickerNetObj.transform}");
        }
    }

    private void HandlePropertyChanged(ItemProperty previous, ItemProperty current)
    {
        currentProperty = current;
        if (currentProperty == null) return;

        spriteRenderer.sprite = currentProperty.IconSprite;

        if (IsOwner) AddRandomForce();
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
    }

    private IEnumerator SpawnCoroutine()
    {
        yield return new WaitForSeconds(pickupRecovery);
        yield return new WaitUntil(() => IsSpawned);
        isWokenUp = true;

        if (IsServer)
        {
            if (!Owner.Value.TryGet(out var networkObject))
            {
                // If no owner, can be picked up
                CanBePickup.Value = true;
            }
        }
    }

    public void Pickup(Transform picker)
    {
        if (!CanBePickup.Value)
        {
            if (showDebugs) Debug.Log("Cannot be picked up.");
            return;
        }

        if (Owner.Value.TryGet(out var networkObject) && networkObject.transform == picker)
        {
            if (showDebugs) Debug.Log($"Already picked up by the same picker: {picker}");
            return;
        }

        if (!IsSpawned)
        {
            if (showDebugs) Debug.Log("Not spawned yet.");
            return;
        }

        PickupRpc(picker.gameObject);
    }

    [Rpc(SendTo.Server)]
    public void PickupRpc(NetworkObjectReference pickerRef)
    {
        Transform picker = null;
        if (pickerRef.TryGet(out var pickerNetObj))
        {
            picker = pickerNetObj.transform;
        }

        if (picker == null || picker == ignorePicker) return;

        // Check for owner and canbePickup again to prevent RPC duplication
        if (!CanBePickup.Value)
        {
            if (showDebugs) Debug.Log("Cannot be picked up.");
            return;
        }

        if (Owner.Value.TryGet(out var networkObject) && networkObject.transform == picker)
        {
            if (showDebugs) Debug.Log($"Already picked up by the same picker: {picker}");
            return;
        }

        if (showDebugs) Debug.Log($"Picked up by {picker}", picker);

        PickupItemOnServer(pickerNetObj);
    }

    public void PickupItemOnServer(NetworkObject pickerNetObj)
    {
        StartCoroutine(WaitForPickupCoroutine(pickerNetObj));
    }

    private IEnumerator WaitForPickupCoroutine(NetworkObject pickerNetObj)
    {
        Owner.Value = pickerNetObj;
        CanBePickup.Value = false;

        yield return new WaitUntil(() => isWokenUp);
        yield return new WaitUntil(() => IsSpawned);
        NetworkObject.ChangeOwnership(pickerNetObj.OwnerClientId);

        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(TimeoutCoroutine(pickerNetObj));
    }

    private IEnumerator TimeoutCoroutine(NetworkObject pickerNetObj)
    {
        yield return new WaitForSeconds(pickupDuration);

        Owner.Value = NetworkObject;
        NetworkObject.ChangeOwnership(0);

        if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
        recoveryCoroutine = StartCoroutine(PickupRecoveryCoroutine());

        yield return recoveryCoroutine;

        if (ignoreCoroutine != null) StopCoroutine(ignoreCoroutine);
        ignoreCoroutine = StartCoroutine(IgnorePickerCoroutine(pickerNetObj.transform));

        yield return ignoreCoroutine;
    }

    private IEnumerator PickupRecoveryCoroutine()
    {
        CanBePickup.Value = false;
        yield return new WaitForSeconds(pickupRecovery);
        CanBePickup.Value = true;
    }

    public void IgnorePickerOnServer(Transform picker)
    {
        if (ignoreCoroutine != null) StopCoroutine(ignoreCoroutine);
        ignoreCoroutine = StartCoroutine(IgnorePickerCoroutine(picker));
    }

    private IEnumerator IgnorePickerCoroutine(Transform picker)
    {
        ignorePicker = picker;
        yield return new WaitForSeconds(pickupDuration);
        ignorePicker = null;
    }

    private IEnumerator PickupCoroutine(Transform picker, float duration)
    {
        yield return spawnCoroutine;

        //var pickerRigidBody = picker.GetComponent<Rigidbody2D>();
        var pickerPos = picker.position + targetOffset;
        var endTime = Time.time + duration;
        var sqrDistance = (transform.position - pickerPos).sqrMagnitude;
        while (sqrDistance > 0.01f)
        {
            if (Time.time > endTime) yield break;
            //pickerPos = picker.position + (Vector3)pickerRigidBody.velocity * Time.fixedDeltaTime + targetOffset;
            pickerPos = picker.position + targetOffset;

            var direction = (pickerPos - transform.position).normalized;
            itemRigidbody.linearVelocity = direction * pickupSpeed;

            sqrDistance = (transform.position - pickerPos).sqrMagnitude;
            if (sqrDistance < 0.04f) break;

            yield return new WaitForFixedUpdate();
        }

        var inventory = picker.GetComponent<PlayerInventory>();
        if (inventory.AddItem(Property.Value))
        {
            gameObject.SetActive(false);
            DestroyServerRpc();
        }

        picker = null;
    }

    [Rpc(SendTo.Server)]
    private void DestroyServerRpc()
    {
        NetworkObjectPool.Main.Despawn(gameObject);
    }

    private void AddRandomForce()
    {
        itemRigidbody.AddForce(Random.insideUnitCircle * Random.Range(5, 10), ForceMode2D.Impulse);
    }
}
*/