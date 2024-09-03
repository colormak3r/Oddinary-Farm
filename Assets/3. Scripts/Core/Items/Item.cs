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
    private NetworkVariable<FixedString128Bytes> Property;
    private ItemProperty currentProperty;

    [SerializeField]
    private NetworkVariable<NetworkBehaviourReference> Picker;
    private Transform currentPicker;
    private float nextPickupStop;

    [SerializeField]
    private NetworkVariable<bool> CanBePickedUp = new NetworkVariable<bool>(true);

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rbody;
    private float pickupRangeSqr;
    private Vector3 dummyVelocity;

    public bool CanBePickedUpValue => CanBePickedUp.Value;
    public NetworkBehaviourReference PickerValue => Picker.Value;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rbody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        HandlePropertyChanged(Property.Value, Property.Value);
        HandlePickerChanged(Picker.Value, Picker.Value);
        Property.OnValueChanged += HandlePropertyChanged;     
        Picker.OnValueChanged += HandlePickerChanged;
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
        if (currentProperty == null) currentProperty = AssetManager.Main.UnidentifiedProperty;

        spriteRenderer.sprite = currentProperty.Sprite;
    }

    private void HandlePickerChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {   
        if(current.TryGet(out var networkBehaviour))
        {
            HandlePickerChanged(networkBehaviour.transform);            
        }        
    }

    private void HandlePickerChanged(Transform picker)
    {
        if (!IsServer) return;

        currentPicker = picker;
        nextPickupStop = Time.time + pickupDuration;
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
    }    

    [Rpc(SendTo.Server)]
    public void PickUpItemRpc(NetworkBehaviourReference newPicker)
    {
        Picker.Value = newPicker;
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
            Picker.Value = null;
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
        yield return new WaitForSeconds(pickupRecovery);
        CanBePickedUp.Value = true;
    }
}
