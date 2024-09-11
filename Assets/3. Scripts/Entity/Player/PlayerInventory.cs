using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using ColorMak3r.Utility;
using UnityEngine.Events;
using static UnityEditor.Progress;
using System.Linq;

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
    private GameObject itemPrefab;
    [SerializeField]
    private SpriteRenderer itemRenderer;

    [Header("Debugs")]
    [SerializeField]
    private int currentHotbarIndex;
    [SerializeField]
    private ItemStack[] inventory = new ItemStack[MAX_INVENTORY_SLOTS];
    [SerializeField]
    private NetworkVariable<FixedString128Bytes> CurrentItemName =
        new NetworkVariable<FixedString128Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
    private ItemProperty currentItem;

    [HideInInspector]
    public UnityEvent OnCurrentItemPropertyChanged;
    public int CurrentHotbarIndex => currentHotbarIndex;

    public override void OnNetworkSpawn()
    {
        HandleCurrentItemChanged(CurrentItemName.Value, CurrentItemName.Value);
        CurrentItemName.OnValueChanged += HandleCurrentItemChanged;
    }
    public override void OnNetworkDespawn()
    {
        CurrentItemName.OnValueChanged -= HandleCurrentItemChanged;
    }

    private void HandleCurrentItemChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        currentItem = (ItemProperty)AssetManager.Main.GetAssetByName(current.ToString());

        if (currentItem != null)
        {
            itemRenderer.sprite = currentItem.Sprite;
        }
        else
        {
            itemRenderer.sprite = null;
        }

        OnCurrentItemPropertyChanged?.Invoke();
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
                if (hit.TryGetComponent(out Item item) && item.CurrentPicker == transform)
                {
                    // Must use ClientRpc since this is on the server
                    AddItemRpc(item.CurrentProperty.name);

                    // Todo: Recycle using network object pooling
                    Destroy(item.gameObject);
                }
            }
        }
    }

    [Rpc(SendTo.Owner)]
    private void AddItemRpc(FixedString128Bytes itemPropertyName)
    {
        AddItem((ItemProperty)AssetManager.Main.GetAssetByName(itemPropertyName.ToString()));
    }

    private void AddItem(ItemProperty property, int amount = 1)
    {
        // Find the index of the property
        var found = GetStackIndex(inventory, property, out int index);
        if (found)
        {
            // Determine if the hotbar is empty and the player just pick up a new item
            var firstPickup = false;
            if (inventory[index].Property == null && index <= 9)
                firstPickup = true;

            inventory[index].Property = property;
            inventory[index].Count += amount;

            Debug.Log($"Added {property.name} to index {index}. New count = {inventory[index].Count}");

            if (firstPickup) ChangeHotBarIndex(index);            
        }
        else
        {
            Debug.Log("Inventory full. Cannot add " + property.name);
        }
    }


    public void RemoveItem(ItemProperty property, int count = 1)
    {

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
    private bool GetStackIndex(ItemStack[] inventory, ItemProperty property, out int index)
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


    public void DropItem(int itemPosition, Vector2 dropPosition)
    {
        var itemStack = inventory[itemPosition];
        if (itemStack.Property == null || itemStack.Count <= 0) return;

        itemStack.Count--;

        DropItemRpc(itemStack.Property.name, dropPosition);
    }

    [Rpc(SendTo.Server)]
    private void DropItemRpc(FixedString128Bytes itemPropertyName, Vector2 dropPosition)
    {
        var item = Instantiate(itemPrefab, dropPosition, Quaternion.identity);
        item.GetComponent<Item>().Initialize((ItemProperty)AssetManager.Main.GetAssetByName(itemPropertyName.ToString()));
        item.GetComponent<NetworkObject>().Spawn();
    }

    public void ChangeHotBarIndex(int index)
    {
        currentHotbarIndex = index;

        if (inventory[index].Property != null && inventory[index].Count > 0)
        {
            CurrentItemName.Value = inventory[index].Property.name;
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