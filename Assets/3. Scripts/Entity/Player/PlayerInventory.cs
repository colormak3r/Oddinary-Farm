using Unity.Netcode;
using ColorMak3r.Utility;
using System;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

[System.Serializable]
public class ItemStack
{
    public ItemProperty Property;
    public int Count;
    public bool IsStackFull => Count >= Property.MaxStack;
    public bool IsStackEmpty => Property == null || Count <= 0;

    public ItemStack(ItemProperty property = null, int count = 0)
    {
        Property = property;
        Count = count;
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
public class PlayerInventory : NetworkBehaviour
{
    private static int MAX_INVENTORY_SLOTS = 40;

    [Header("Settings")]
    [SerializeField]
    private float inventoryRadius = 1f;
    [SerializeField]
    private int inventorySlot = 20;
    [SerializeField]
    private LayerMask itemLayer;
    [SerializeField]
    private Transform inventoryTransform;
    [SerializeField]
    private HandProperty handProperty;
    [SerializeField]
    private SpriteRenderer itemRenderer;

    [Header("Debugs")]
    [SerializeField]
    private bool debug;
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

    public Item CurrentItemValue => currentItem;
    public int CurrentHotbarIndex => currentHotbarIndex;

    private void Awake()
    {
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
            UpdateItemRefRpc(0, handProperty);
        }

        CurrentItem.OnValueChanged += HandleCurrentItemChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentItem.OnValueChanged -= HandleCurrentItemChanged;
    }

    private void HandleCurrentItemChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
    {
        if (newValue.TryGet(out Item item))
        {
            currentItem = item;
            itemRenderer.sprite = item.PropertyValue.Sprite;
        }
    }

    private void Update()
    {
        // Run on client only
        if (!IsClient) return;

        // Automatically try to pick up Items in the close proximity
        var hits = Physics2D.OverlapCircleAll(transform.PositionHalfUp(), inventoryRadius, itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out ItemReplica itemReplica) && itemReplica.OwnerValue == NetworkObject)
                {
                    // Todo: Ask to add item;

                    // Add an item on client side
                    AddItemOnClient(itemReplica.CurrentProperty);

                    // Todo: Recycle using network object pooling
                    Destroy(itemReplica.gameObject);
                }
            }
        }
    }

    private bool AddItemOnClient(ItemProperty property, int amount = 1)
    {
        if (!IsClient) return false;

        // Find the index of the property
        var found = FindIncompleteStackIndex(inventory, property, out int index);
        if (found)
        {
            var itemStack = inventory[index];
            itemStack.Property = property;
            UpdateItemRefRpc(index, property);

            var stackProperty = itemStack.Property;
            if (itemStack.Count + amount < stackProperty.MaxStack)
            {
                inventory[index].Count += amount;
            }
            else
            {
                // Recursively add the overflow item
                AddItemOnClient(property, itemStack.Count + amount - stackProperty.MaxStack);
            }

            if (debug) Debug.Log($"Added {property.name} to index {index}. New count = {inventory[index].Count}");
            return true;
        }
        else
        {
            if (debug) Debug.Log("Inventory full. Cannot add " + property.name);
            //Todo: A server RPC to spawn the leftover item
            return false;
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateItemRefRpc(int index, ItemProperty property)
    {
        if (itemRefs[index] != null)
        {

        }
        else
        {
            // Create a networked Item at this position
            var item = Instantiate(property.Prefab, inventoryTransform);

            var networkObject = item.GetComponent<NetworkObject>();
            networkObject.Spawn();
            networkObject.TrySetParent(inventoryTransform, false);

            var itemRef = item.GetComponent<Item>();
            itemRef.PropertyValue = property;

            UpdateItemRefRpc(index, itemRef);
        }
    }

    [Rpc(SendTo.Owner)]
    private void UpdateItemRefRpc(int index, NetworkBehaviourReference itemRef)
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
    private bool FindIncompleteStackIndex(ItemStack[] inventory, ItemProperty property, out int index)
    {
        index = -1;

        // First loop to check if there is any stack with the property that is not full
        for (int i = 1; i < inventory.Length; i++)
        {
            if (inventory[i].Property == property && !inventory[i].IsStackFull)
            {
                index = i;
                return true;
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.PositionHalfUp(), inventoryRadius);
    }
}