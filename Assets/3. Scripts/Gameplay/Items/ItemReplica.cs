using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemReplica : NetworkBehaviour
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

    /*[SerializeField]
    private Transform currentPicker;
    public Transform CurrentPicker => currentPicker;*/

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
    private Coroutine pickupTimeoutCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemRigidbody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
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

        spriteRenderer.sprite = currentProperty.Sprite;

        if (IsOwner)
        {
            AddRandomForce();
        }
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

        Owner.Value = pickerNetObj;
        CanBePickup.Value = false;
        NetworkObject.ChangeOwnership(pickerNetObj.OwnerClientId);

        if (pickupTimeoutCoroutine != null) StopCoroutine(pickupTimeoutCoroutine);
        pickupTimeoutCoroutine = StartCoroutine(PickupTimeoutCoroutine(pickerNetObj));
    }

    public void PickupItemOnServer(NetworkObject preferNetObj)
    {
        Owner.Value = preferNetObj;
        CanBePickup.Value = false;
        NetworkObject.ChangeOwnership(preferNetObj.OwnerClientId);

        if (pickupTimeoutCoroutine != null) StopCoroutine(pickupTimeoutCoroutine);
        pickupTimeoutCoroutine = StartCoroutine(PickupTimeoutCoroutine(preferNetObj));
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
            itemRigidbody.velocity = direction * pickupSpeed;

            sqrDistance = (transform.position - pickerPos).sqrMagnitude;
            yield return new WaitForFixedUpdate();
        }

        picker = null;
    }

    private IEnumerator PickupTimeoutCoroutine(NetworkObject pickerNetObj)
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

    private void AddRandomForce()
    {
        itemRigidbody.AddForce(Random.insideUnitCircle * Random.Range(5, 10), ForceMode2D.Impulse);
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

    public void RequestDestroy()
    {
        // Object is destroyed before being requested to be destroyed somehow
        // Must check if object is spawned before sending RPC
        if (IsSpawned) DestroyServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void DestroyServerRpc()
    {
        NetworkObject.Despawn(true);
    }
}
