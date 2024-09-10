using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using ColorMak3r.Utility;

[System.Serializable]
public struct ItemStack : IEquatable<ItemStack>, INetworkSerializable
{
    private FixedString128Bytes propertyName;
    private int count;

    public ItemProperty Property => (ItemProperty)AssetManager.Main.GetAssetByName(propertyName.ToString());
    public int Count => count;  

    public ItemStack(string propertyName, int count = 1)
    {
        this.propertyName = propertyName;
        this.count = count;
    }

    public bool Equals(ItemStack other)
    {
        return propertyName == other.propertyName && count == other.count;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref propertyName);
        serializer.SerializeValue(ref count);
    }

    public void InreaseCount(int amount = 1)
    {
        count += amount;
    }

    public void DecreaseCount(int amount = 1)
    {
        count -= amount;
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
    [SerializeField]
    private GameObject itemPrefab;

    [Header("Debugs")]
    [SerializeField]
    private NetworkList<ItemStack> Inventory;
    [SerializeField]
    private NetworkVariable<int> CurrentHotbarPosition;

    public int CurrentHotbarPositionValue => CurrentHotbarPosition.Value;

    private void Awake()
    {
        Inventory = new NetworkList<ItemStack>();
    }

    private void Update()
    {
        if (!IsServer) return;

        var hits = Physics2D.OverlapCircleAll(transform.PositionHalfUp(), inventoryRadius, itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out Item item))
                {
                    AddItem(item.CurrentProperty);

                    // Todo: Recycle using network object pooling
                    Destroy(item.gameObject);
                }
            }
        }
    }

    private void AddItem(ItemProperty property, int count = 1)
    {
        Inventory.Add(new ItemStack(property.name));
    }


    public void RemoveItem(ItemProperty property, int count = 1)
    {

    }

    public void DropItem(int itemPosition, Vector2 dropPosition)
    {
        DropItemRpc(itemPosition, dropPosition);
    }

    [Rpc(SendTo.Server)]
    private void DropItemRpc(int itemPosition, Vector2 dropPosition)
    {
        if (Inventory.Count == 0) return;
        var itemStack = Inventory[itemPosition];
        if (itemStack.Count <= 0) return;

        if (itemStack.Count == 1)
        {
            Inventory.RemoveAt(itemPosition);
        }
        else //itemStack.Count > 1
        {
            Inventory[itemPosition].DecreaseCount();
        }
        
        var item = Instantiate(itemPrefab, dropPosition, Quaternion.identity);        
        item.GetComponent<Item>().Initialize(itemStack.Property);
        item.GetComponent<NetworkObject>().Spawn();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.PositionHalfUp(), inventoryRadius);
    }
}
