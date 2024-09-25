using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ItemReplica : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private ItemProperty mockProperty;
    [SerializeField]
    private float pickupSpeed = 300f;
    [SerializeField]
    private float pickupDuration = 3f;
    [SerializeField]
    private float pickupRecovery = 3f;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();
    private ItemProperty currentProperty;

    private Transform currentPicker;
    private float nextPickupStop;

    [SerializeField]
    private NetworkVariable<bool> CanBePickedUp = new NetworkVariable<bool>();
    [SerializeField]
    private NetworkVariable<NetworkObjectReference> OwnerReference = new NetworkVariable<NetworkObjectReference>();

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rbody;
    private Collider2D c2D;
    private float pickupRangeSqr;
    private Vector3 dummyVelocity;

    public bool CanBePickedUpValue => CanBePickedUp.Value;
    public NetworkObject OwnerValue => OwnerReference.Value;
    public ItemProperty CurrentProperty => currentProperty;
    public Transform CurrentPicker => currentPicker;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rbody = GetComponent<Rigidbody2D>();
        c2D = GetComponentInChildren<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) CanBePickedUp.Value = false;

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
        //AssetManager.Main.GetScriptableObjectByName<ItemProperty>(property);
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

    public void Initialize(ItemProperty property)
    {
        Property.Value = property;
        StartCoroutine(PickupRecovery());
    }

    public void PickUpItem(Transform newPicker, NetworkObject networkObject)
    {
        currentPicker = newPicker;        
        nextPickupStop = Time.time + pickupDuration;
        CanBePickedUp.Value = false;
        OwnerReference.Value = networkObject;
    }

    private void FixedUpdate()
    {
        if (!IsServer || currentPicker == null) return;

        if (Time.time < nextPickupStop)
        {
            FlyToward(currentPicker.transform.position);
        }
        else
        {
            currentPicker = null;
            CanBePickedUp.Value = false;
            StartCoroutine(PickupRecovery());
        }
    }

    private void FlyToward(Vector3 position)
    {
        var targetVelocity = (position - transform.position) * pickupSpeed * Time.deltaTime;
        rbody.velocity = Vector3.SmoothDamp(rbody.velocity, targetVelocity, ref dummyVelocity, 0.01f);
    }

    private IEnumerator PickupRecovery()
    {
        //collider2D.enabled = value;
        yield return new WaitForSeconds(pickupRecovery);
        CanBePickedUp.Value = true;
    }
}
