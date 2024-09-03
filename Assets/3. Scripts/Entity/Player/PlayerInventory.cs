using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

[System.Serializable]
public struct ItemPropertyNameCount: IEquatable<ItemPropertyNameCount>, INetworkSerializable
{
    public FixedString128Bytes propertyName;
    public int count;

    public ItemPropertyNameCount(string propertyName, int count = 1)
    {
        this.propertyName = propertyName;
        this.count = count;
    }

    public bool Equals(ItemPropertyNameCount other)
    {
        return propertyName == other.propertyName && count == other.count;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref propertyName);
        serializer.SerializeValue(ref count);
    }
}

public class PlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float inventoryRadius = 1f;
    [SerializeField]
    private int inventorySlot = 20;
    [SerializeField]
    private LayerMask itemLayer;

    [Header("Debugs")]
    [SerializeField]
    NetworkList<ItemPropertyNameCount> Inventory;

    private void Awake()
    {
        Inventory = new NetworkList<ItemPropertyNameCount>();
    }

    private void Update()
    {
        if (!IsServer) return;

        var hits = Physics2D.OverlapCircleAll(transform.position, inventoryRadius, itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out Item item))
                {
                    Add(item.CurrentProperty);

                    // Todo: Recycle using network object pooling
                    Destroy(item.gameObject);
                }
            }
        }
    }

    private void Add(ItemProperty property, int count = 1)
    {
        Inventory.Add(new ItemPropertyNameCount(property.name));
    }



    public void Remove(ItemProperty property, int count = 1)
    {

    }

   
}
