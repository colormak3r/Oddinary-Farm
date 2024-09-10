using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
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
    private NetworkVariable<FixedString128Bytes> Property = new NetworkVariable<FixedString128Bytes>();
    private ItemProperty currentProperty;

    [SerializeField]
    private Transform currentPicker;
    private float nextPickupStop;

    [SerializeField]
    private NetworkVariable<bool> CanBePickedUp = new NetworkVariable<bool>();

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rbody;
    private Collider2D collider2D;
    private float pickupRangeSqr;
    private Vector3 dummyVelocity;

    public bool CanBePickedUpValue => CanBePickedUp.Value;
    public ItemProperty CurrentProperty => currentProperty;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rbody = GetComponent<Rigidbody2D>();
        collider2D = GetComponentInChildren<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer) CanBePickedUp.Value = false;
        
        HandlePropertyChanged(Property.Value, Property.Value);
        Property.OnValueChanged += HandlePropertyChanged;     
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;
    }

    private void HandlePropertyChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        HandlePropertyChanged(current.ToString());
    }

    private void HandlePropertyChanged(string item)
    {
        currentProperty = (ItemProperty)AssetManager.Main.GetAssetByName(item);
        if (currentProperty == null) currentProperty = AssetManager.Main.UnidentifiedItemProperty;

        spriteRenderer.sprite = currentProperty.Sprite;
    }

    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        Property.Value = mockProperty.name;
    }

    public void Initialize(ItemProperty property)
    {
        Property.Value = property.name;
        StartCoroutine(PickupRecovery());
    }    

    public void PickUpItem(Transform newPicker)
    {
        currentPicker = newPicker;
        nextPickupStop = Time.time + pickupDuration;
        CanBePickedUp.Value = false;
    }

    private void FixedUpdate()
    {
        if(!IsServer || currentPicker == null) return;

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
