using Unity.Netcode;
using ColorMak3r.Utility;
using System;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Events;
using System.Collections.Generic;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class ItemStack
{
    public ItemProperty Property;
    public uint Count;
    public bool IsStackFull => Count >= Property.MaxStack;
    public bool IsStackEmpty => Property == null || Count <= 0;

    public ItemStack(ItemProperty property = null, uint count = 0)
    {
        Property = property;
        Count = count;
    }

    public void EmptyStack()
    {
        Property = null;
        Count = 0;
    }
}

[System.Serializable]
public struct ItemRefElement : INetworkSerializable, IEquatable<ItemRefElement>
{
    public int Index;
    public NetworkBehaviourReference ItemRef;

    public ItemRefElement(int index, NetworkBehaviourReference itemRef)
    {
        Index = index;
        ItemRef = itemRef;
    }

    public bool Equals(ItemRefElement other)
    {
        return Index == other.Index && ItemRef.Equals(other.ItemRef);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out Index);
            reader.ReadValueSafe(out ItemRef);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(Index);
            writer.WriteValueSafe(ItemRef);
        }
    }
}

// Must include this line [GenerateSerializationForType(typeof(byte))] somewhere in the project
// See: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/2920#issuecomment-2173886545
[GenerateSerializationForType(typeof(byte))]
[RequireComponent(typeof(ItemSpawner))]
public class PlayerInventory : NetworkBehaviour
{
    private static int MAX_INVENTORY_SLOTS = 20;

    [Header("Settings")]
    [SerializeField]
    private float inventoryRadius = 1f;
    /*[SerializeField]
    private int inventorySlot = 10;*/
    [SerializeField]
    private LayerMask itemLayer;
    [SerializeField]
    private Transform inventoryTransform;
    [SerializeField]
    private HandProperty handProperty;
    [SerializeField]
    private SpriteRenderer itemRenderer;
    [SerializeField]
    private ItemStack[] defaultInventory;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebug;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private int currentHotbarIndex;
    [SerializeField]
    private ItemStack[] inventory;
    [SerializeField]
    private Item[] itemRefs;

    [SerializeField]
    private NetworkVariable<NetworkBehaviourReference> CurrentItem =
        new NetworkVariable<NetworkBehaviourReference>(default, default, NetworkVariableWritePermission.Owner);
    [SerializeField]
    private Item currentItem;

    [SerializeField]
    private NetworkVariable<ulong> Coins = new NetworkVariable<ulong>(10, default, NetworkVariableWritePermission.Owner);
    [HideInInspector]
    public UnityEvent<ulong> OnCoinsValueChanged;

    private ItemSpawner itemSpawner;

    public Item CurrentItemValue => currentItem;
    public int CurrentHotbarIndex => currentHotbarIndex;
    public ulong CoinsValue => Coins.Value;
    public ItemStack[] Inventory => inventory;

    private void Awake()
    {
        itemSpawner = GetComponent<ItemSpawner>();

        inventory = new ItemStack[MAX_INVENTORY_SLOTS];
        itemRefs = new Item[MAX_INVENTORY_SLOTS];
        for (int i = 0; i < MAX_INVENTORY_SLOTS; i++)
        {
            inventory[i] = new ItemStack();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            inventory[0].Property = handProperty;
            CreateItemRefServerRpc(0, handProperty);
        }

        if (IsOwner)
        {
            foreach (var itemStack in defaultInventory)
            {
                AddItemOnClient(itemStack.Property, itemStack.Count);
            }
        }

        CurrentItem.OnValueChanged += HandleCurrentItemChanged;
        Coins.OnValueChanged += HandleCoinValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentItem.OnValueChanged -= HandleCurrentItemChanged;
        Coins.OnValueChanged -= HandleCoinValueChanged;
    }

    private void HandleCurrentItemChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
    {
        if (newValue.TryGet(out Item item))
        {
            currentItem = item;
            itemRenderer.sprite = item.PropertyValue.Sprite;
        }
    }

    private void HandleCoinValueChanged(ulong previousValue, ulong newValue)
    {
        OnCoinsValueChanged?.Invoke(newValue);
    }


    private void Update()
    {
        // Run on client only
        if (!IsOwner) return;

        // Automatically try to pick up Items in the close proximity
        var hits = Physics2D.OverlapCircleAll(transform.PositionHalfUp(), inventoryRadius, itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out ItemReplica itemReplica) && itemReplica.OwnerValue == NetworkObject)
                {
                    // Add an item on client side
                    if (AddItemOnClient(itemReplica.CurrentProperty))
                    {
                        itemReplica.gameObject.SetActive(false);
                        itemReplica.DestroyRpc();   // Todo: Recycle using network object pooling
                    }
                }
            }
        }
    }

    public bool AddItemOnClient(ItemProperty property, uint amount = 1)
    {
        if (!IsOwner) return false;

        if (property is CurrencyProperty) 
        {
            var value = ((CurrencyProperty)property).Value * amount;
            AddCoinsOnClient(value);
            return true;
        }

        // Find the index of the property
        var found = FindPartialStackIndex(inventory, property, out int index);
        if (found)
        {
            var itemStack = inventory[index];
            itemStack.Property = property;
            CreateItemRefServerRpc(index, property);

            var stackProperty = itemStack.Property;
            if (itemStack.Count + amount <= stackProperty.MaxStack)
            {
                inventory[index].Count += amount;
            }
            else
            {
                // Recursively add the overflow item
                return AddItemOnClient(property, itemStack.Count + amount - stackProperty.MaxStack);
            }

            if (showDebug) Debug.Log($"Added {property.name} to index {index}. New count = {inventory[index].Count}");
            return true;
        }
        else
        {
            if (showDebug) Debug.Log("Inventory full. Cannot add " + property.name);
            return false;
        }
    }

    /*public bool CanConsumeItemOnClient(int index, uint amount = 1)
    {
        if (!IsOwner) return false;
        if (amount <= 0) return false;
        if (index < 0 || index >= inventory.Length) return false;

        var itemStack = inventory[index];
        var itemProperty = itemStack.Property;

        uint total = 0;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Property == itemProperty)
            {
                total += inventory[i].Count;
                if (total >= amount)
                    return true;
            }
        }
        return false;
    }*/

    public bool ConsumeItemOnClient(ItemProperty property, uint amount = 1)
    {
        if (!IsOwner) return false;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Property == property)
            {
                return ConsumeItemOnClient(i, amount);
            }
        }
        return false;
    }

    public bool ConsumeItemOnClient(int index, uint amount = 1)
    {
        if (!IsOwner) return false;
        if (index < 0 || index >= inventory.Length) return false;

        var itemStack = inventory[index];
        var itemProperty = itemStack.Property;

        if (itemStack.Count - amount > 0)
        {
            itemStack.Count -= amount;
            return true;
        }
        else
        {
            var amountNeeded = amount - itemStack.Count;

            // Remove the current item stack
            itemStack.EmptyStack();
            if (index == currentHotbarIndex) CurrentItem.Value = itemRefs[0];
            RemoveItemRefServerRpc(index);

            // Further recursively consume item if needed
            if (amountNeeded > 0)
            {
                if (FindNearestStackIndex(inventory, itemProperty, index, out var nextIndex))
                    return ConsumeItemOnClient(nextIndex, amountNeeded);
                else
                    return false;
            }
            else
            {
                return true;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RemoveItemRefServerRpc(int index)
    {
        if (showDebug) Debug.Log($"Removing Item Ref #{index} on server");
        Destroy(itemRefs[index].gameObject);
    }

    [Rpc(SendTo.Server)]
    private void CreateItemRefServerRpc(int index, ItemProperty property)
    {
        var itemRef = itemRefs[index];
        if (itemRef != null)
        {
            if (itemRef.PropertyValue != null && itemRef.PropertyValue == property)
            {
                // Same property, new property does not need to be created
            }
            else
            {
                Destroy(itemRef.gameObject);
                CreateItemOnServer(index, property);
            }
        }
        else
        {
            CreateItemOnServer(index, property);
        }
    }

    private void CreateItemOnServer(int index, ItemProperty property)
    {
        if (!IsServer) return;

        // Create a networked Item at this position
        var item = Instantiate(property.Prefab, inventoryTransform);

        var networkObject = item.GetComponent<NetworkObject>();
        networkObject.Spawn();
        networkObject.TrySetParent(inventoryTransform, false);

        var itemRef = item.GetComponent<Item>();
        itemRef.PropertyValue = property;
        itemRefs[index] = itemRef;

        UpdateItemRefOwnerRpc(index, itemRef);
    }

    [Rpc(SendTo.Owner)]
    private void UpdateItemRefOwnerRpc(int index, NetworkBehaviourReference itemRef)
    {
        if (itemRef.TryGet(out Item item))
        {
            itemRefs[index] = item;
        }
    }

    /// <summary>
    /// Finds the index of a suitable stack in the inventory based on the specified item property.
    /// The function first looks for an existing stack with the same property that is not full.
    /// If no such stack is found, it searches for the first empty slot in the inventory.
    /// </summary>
    /// <param name="inventory">An array of ItemStack objects representing the player's inventory.</param>
    /// <param name="property">The property of the item being searched for, used to find a matching stack.</param>
    /// <param name="index">An output parameter that returns the index of the suitable stack or empty slot found.</param>
    /// <returns>
    /// Returns true if a suitable stack (either an existing stack that is not full or an empty slot) is found.
    /// Returns false if no such stack or empty slot is found.
    /// </returns>
    /// <remarks>
    /// The function uses two passes:
    /// 1. It first checks for a non-full stack with the same property.
    /// 2. If no stack is found, it checks for an empty slot (where property is null).
    /// Both loops start at index 1, skipping item at 0 which will always be the hand.
    /// </remarks>
    private bool FindPartialStackIndex(ItemStack[] inventory, ItemProperty property, out int index)
    {
        index = -1;

        // First loop to check if there is any stack with the property that is not full
        if (property.IsStackable)
        {
            for (int i = 1; i < inventory.Length; i++)
            {
                if (inventory[i].Property == property && !inventory[i].IsStackFull)
                {
                    index = i;
                    return true;
                }
            }
        }

        // Second loop to check for the first available slot
        for (int i = 1; i < inventory.Length; i++)
        {
            if (inventory[i].Property == null)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    private bool FindNearestStackIndex(ItemStack[] inventory, ItemProperty property, int initialIndex, out int index)
    {
        index = -1;

        for (int offset = 0; offset < inventory.Length; offset++)
        {
            int indexToCheck;

            // Check positive and negative offsets
            if (offset % 2 == 0)
                indexToCheck = initialIndex + (offset / 2);
            else
                indexToCheck = initialIndex - ((offset + 1) / 2);

            // Ensure the index is within bounds
            if (indexToCheck >= 0 && indexToCheck < inventory.Length)
            {
                if (inventory[indexToCheck].Property == property)
                {
                    index = indexToCheck;
                    return true;
                }
            }
        }

        return false;
    }

    public void ChangeHotBarIndex(int index)
    {
        currentHotbarIndex = index;
        if (itemRefs[index] != null)
        {
            CurrentItem.Value = itemRefs[index];
        }
        else
        {
            CurrentItem.Value = itemRefs[0];
        }
    }

    public void AddCoinsOnClient(uint value)
    {
        Coins.Value += value;
    }

    public void ConsumeCoinsOnClient(ulong value)
    {
        if (Coins.Value - value < 0) return;
        Coins.Value -= value;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (currentItem == null) return;
        /*Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentItem.PropertyValue.);*/
    }
}