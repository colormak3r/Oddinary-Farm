using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemReplicaa : NetworkBehaviour, INetworkObjectPoolBehaviour
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
    private bool pickupPrefered = false;
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
        pickupPrefered = false;
        spawnTracker++;

        AddRandomForceRpc();
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
        AddRandomForceRpc();
    }

    private void Update()
    {
        if (IsServer && canBePickedup && !pickupPrefered && Time.time > nextPickupTime && Time.time > nextScanTime)
        {
            nextScanTime = Time.time + 0.1f;
            var player = ScanForPlayer();
            if (player != null) PickupOnServer(player.transform);
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
        /*if (ignoreCoroutine != null) StopCoroutine(ignoreCoroutine);
        ignoreCoroutine = StartCoroutine(IgnorePickerCoroutine()); */
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
    private void AddRandomForceRpc()
    {
        AddRandomForce();
    }

    private void AddRandomForce()
    {
        itemRigidbody.AddForce(Random.insideUnitCircle * Random.Range(5, 10), ForceMode2D.Impulse);
    }
}