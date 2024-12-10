using System;
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
    private float pickupSpeed = 800f;
    [SerializeField]
    private float pickupDuration = 3f;
    [SerializeField]
    private float pickupRecovery = 1f;
    [SerializeField]
    private Vector3 targetOffset;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();
    private ItemProperty currentProperty;
    public ItemProperty CurrentProperty => currentProperty;

    [SerializeField]
    private Transform currentPicker;
    public Transform CurrentPicker => currentPicker;

    [SerializeField]
    private Transform ignorePicker;

    [SerializeField]
    private NetworkVariable<NetworkObjectReference> Owner = new NetworkVariable<NetworkObjectReference>();
    public NetworkObject OwnerValue => Owner.Value;

    private float nextPickupStop;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D itemRigidbody;
    private Collider2D itemCollider;

    private float pickupRangeSqr;
    private Vector3 dummyVelocity;
    private Coroutine pickupCoroutine;
    private Coroutine recoveryCoroutine;
    private Coroutine ignoreCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemRigidbody = GetComponent<Rigidbody2D>();
        itemCollider = GetComponentInChildren<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        HandlePropertyChanged(null, Property.Value);
        Property.OnValueChanged += HandlePropertyChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;
    }

    private void HandlePropertyChanged(ItemProperty previous, ItemProperty current)
    {
        HandlePropertyChanged(current);
    }

    private void HandlePropertyChanged(ItemProperty property)
    {
        currentProperty = property;
        if (currentProperty == null) return;

        spriteRenderer.sprite = currentProperty.Sprite;
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
        if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
        recoveryCoroutine = StartCoroutine(PickupRecoveryCoroutine());
    }

    private IEnumerator PickupRecoveryCoroutine()
    {
        yield return new WaitForSeconds(pickupRecovery);
    }

    public void PickUpItemOnServer(Transform picker)
    {
        if (picker == ignorePicker) return;
        if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
        currentPicker = picker;
        Owner.Value = picker.gameObject;
        pickupCoroutine = StartCoroutine(PickupCoroutine(picker, pickupDuration));
    }

    private IEnumerator PickupCoroutine(Transform picker, float duration)
    {
        yield return recoveryCoroutine;

        var pickerPos = currentPicker.position + targetOffset;
        var endTime = Time.time + duration;
        while ((transform.position - pickerPos).sqrMagnitude > 0.001f)
        {
            if (Time.time > endTime) yield break;
            pickerPos = currentPicker.position + targetOffset;
            FlyToward(pickerPos);
            yield return new WaitForFixedUpdate();
        }

        if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
        recoveryCoroutine = StartCoroutine(PickupRecoveryCoroutine());

        yield return recoveryCoroutine;

        currentPicker = null;
        Owner.Value = default;

        if (ignoreCoroutine != null) StopCoroutine(ignoreCoroutine);
        ignoreCoroutine = StartCoroutine(IgnoreCoroutine(picker));

        yield return ignoreCoroutine;
    }

    public void IgnorePicker(Transform picker)
    {
        if (ignoreCoroutine != null) StopCoroutine(ignoreCoroutine);
        ignoreCoroutine = StartCoroutine(IgnoreCoroutine(picker));
    }

    private IEnumerator IgnoreCoroutine(Transform picker)
    {
        ignorePicker = picker;
        yield return new WaitForSeconds(pickupDuration);
        ignorePicker = null;
    }

    private void FlyToward(Vector3 position)
    {
        var direction = (position - transform.position).normalized;
        itemRigidbody.velocity = direction * pickupSpeed;
    }

    public void AddRandomForce()
    {
        itemRigidbody.AddForce(Random.insideUnitCircle * Random.Range(0, 10), ForceMode2D.Impulse);
    }

    public void Destroy()
    {
        DestroyServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void DestroyServerRpc()
    {
        Destroy(gameObject);
    }
}
