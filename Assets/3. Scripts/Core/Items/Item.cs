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

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<FixedString128Bytes> Property;
    private ItemProperty currentProperty;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rbody;
    private float pickupRangeSqr;
    private Vector3 dummyVelocity;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rbody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        HandleCurrentItemChangedInternal(Property.Value.ToString());
        Property.OnValueChanged += HandleCurrentItemChanged;     
    }

    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        Property.Value = mockProperty.name;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandleCurrentItemChanged;
    }

    private void HandleCurrentItemChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        HandleCurrentItemChangedInternal(current.ToString());
    }

    private void HandleCurrentItemChangedInternal(string item)
    {
        currentProperty = (ItemProperty)AssetManager.Main.GetAssetByName(item);
        if (currentProperty == null) currentProperty = AssetManager.Main.UnidentifiedProperty;

        spriteRenderer.sprite = currentProperty.Sprite;
    }
}
