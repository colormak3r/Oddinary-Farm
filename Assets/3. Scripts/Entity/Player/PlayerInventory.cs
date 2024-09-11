using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using ColorMak3r.Utility;
using UnityEngine.Events;

[System.Serializable]
public struct ItemStack : IEquatable<ItemStack>, INetworkSerializable
{
    [SerializeField]
    private FixedString128Bytes propertyName;
    [SerializeField]
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
    [SerializeField]
    private SpriteRenderer itemRenderer;

    [Header("Debugs")]
    [SerializeField]
    private int currentHotbarIndex;
    [SerializeField]
    private NetworkList<ItemStack> Inventory;
    [SerializeField]
    private NetworkVariable<ItemStack> CurrentItemStack = new NetworkVariable<ItemStack>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField]
    private ItemProperty currentItemProperty;

    [HideInInspector]
    public UnityEvent OnCurrentItemPropertyChanged;
    public int CurrentHotbarIndex => currentHotbarIndex;
    public ItemProperty CurrentItemProperty => currentItemProperty;

    private void Awake()
    {
        Inventory = new NetworkList<ItemStack>();
    }

    public override void OnNetworkSpawn()
    {
        HandleCurrentItemStackChanged(CurrentItemStack.Value, CurrentItemStack.Value);
        CurrentItemStack.OnValueChanged += HandleCurrentItemStackChanged;
    }



    public override void OnNetworkDespawn()
    {
        CurrentItemStack.OnValueChanged -= HandleCurrentItemStackChanged;
    }

    private void HandleCurrentItemStackChanged(ItemStack previous, ItemStack current)
    {
        if (current.Count == 0)
        {
            // Hand
        }
        else
        {
            currentItemProperty = CurrentItemStack.Value.Property;
            if (currentItemProperty == null)
            {
                //Hand
            }
            else
            {
                itemRenderer.sprite = currentItemProperty.Sprite;
                OnCurrentItemPropertyChanged?.Invoke();
            }
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // Automatically try to pick up Items in the close proximity
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

    public void ChangeHotBarIndex(int index)
    {
        currentHotbarIndex = index;

        if (Inventory.Count > index)
        {
            CurrentItemStack.Value = Inventory[index];
        }
        else
        {
            // Hand
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.PositionHalfUp(), inventoryRadius);
    }
}