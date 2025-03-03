using Unity.Netcode;
using ColorMak3r.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.Collections.Generic;

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
public class PlayerInventory : NetworkBehaviour, IControllable
{
    private static int MAX_INVENTORY_SLOTS = 30;

    [Header("Inventory Settings")]
    [SerializeField]
    private float inventoryRadius = 1f;
    [SerializeField]
    private Vector3 inventoryOffset = new Vector3(0, 0.75f);
    [SerializeField]
    private LayerMask itemLayer;
    [SerializeField]
    private HandProperty handProperty;
    [SerializeField]
    private ItemStack[] defaultInventory;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebug;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private bool showItemDebug;
    [SerializeField]
    private bool showItemGizmos;
    [SerializeField]
    private int currentHotbarIndex;
    [SerializeField]
    private ItemStack[] inventory;
    [SerializeField]
    private Item[] itemRefs;

    private bool isControllable = true;

    [SerializeField]
    private NetworkVariable<NetworkBehaviourReference> CurrentItem =
        new NetworkVariable<NetworkBehaviourReference>(default, default, NetworkVariableWritePermission.Owner);
    [SerializeField]
    private Item currentItemOnLocal;

    [SerializeField]
    private NetworkVariable<ulong> Wallet = new NetworkVariable<ulong>(10, default, NetworkVariableWritePermission.Owner);
    [HideInInspector]
    public UnityEvent<ulong> OnCoinsValueChanged;

    public Action<Item> OnCurrentItemChanged;

    private InventoryUI inventoryUI;

    public int CurrentHotbarIndex => currentHotbarIndex;
    public ulong WalletValue => Wallet.Value;
    public ItemStack[] Inventory => inventory;

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
        if (IsOwner)
        {
            // Initialize the inventory UI
            inventoryUI = InventoryUI.Main;
            inventoryUI.Initialize(this);

            //Add the hand to the inventory
            inventory[0].Property = handProperty;
            inventory[0].Count = 1;
            CreateItemRefServerRpc(0, handProperty);

            // Add the default items to the inventory
            foreach (var itemStack in defaultInventory)
            {
                AddItemOnClient(itemStack.Property, itemStack.Count, false);
            }

            // Set the current item to the Hand
            ChangeHotBarIndex(0);

            // Update the wallet
            inventoryUI.UpdateWallet(WalletValue);
        }

        CurrentItem.OnValueChanged += HandleCurrentItemChanged;
        Wallet.OnValueChanged += HandleCoinValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentItem.OnValueChanged -= HandleCurrentItemChanged;
        Wallet.OnValueChanged -= HandleCoinValueChanged;

        if (IsServer)
        {
            // Clean up when a client despawns
            foreach (var item in itemRefs)
            {
                if (item != null)
                {
                    var netObj = item.GetComponent<NetworkObject>();
                    if (netObj.IsSpawned) netObj.Despawn();
                }
            }
        }
    }

    private void HandleCurrentItemChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
    {
        if (newValue.TryGet(out Item item))
        {
            if (item == null) Debug.Log("Item is null");
            if (item.PropertyValue == null) Debug.Log("Item property is null");

            OnCurrentItemChanged?.Invoke(item);

#if UNITY_EDITOR
            if (showItemGizmos || showItemDebug)
            {
                var items = GetComponentsInChildren<Item>();
                foreach (var i in items)
                {
                    i.SetGizmosVisibility(false);
                    i.SetDebugVisibility(false);
                }

                item.SetGizmosVisibility(showItemGizmos);
                item.SetDebugVisibility(showItemDebug);
            }
#endif
        }
    }

    private void HandleCoinValueChanged(ulong previousValue, ulong newValue)
    {
        OnCoinsValueChanged?.Invoke(newValue);
    }

    public bool AddItemOnClient(ItemProperty property, uint amount = 1, bool playSound = true)
    {
        if (!IsOwner) return false;

        if (showDebug) Debug.Log($"Adding {amount}x {property.Name} on client");

        if (property is CurrencyProperty)
        {
            var currency = property as CurrencyProperty;
            var value = currency.Value * amount;
            AddCoinsOnClient(value);

            //Play the pickup sound
            if (playSound) AudioManager.Main.PlayOneShot(property.PickupSound);

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
                inventoryUI.UpdateSlot(index, itemStack.Property.Sprite, (int)itemStack.Count);

                // Update the current item if the player is holding the item
                if (index == currentHotbarIndex) ChangeHotBarIndex(index);

                //Play the pickup sound
                if (playSound) AudioManager.Main.PlayOneShot(property.PickupSound);
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

    /*public bool ConsumeItemOnClient(ItemProperty property, uint amount = 1)
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
    }*/

    public bool CanConsumeItemOnClient(int index, uint amount = 1)
    {
        if (!IsOwner) return false;
        if (index < 0 || index >= inventory.Length) return false;
        if (amount == 0) return true;

        var itemStack = inventory[index];
        var itemProperty = itemStack.Property;

        if (itemStack.Count - amount >= 0)
        {
            return true;
        }
        else
        {
            var amountNeeded = amount - itemStack.Count;
            for (int i = 0; i < inventory.Length; i++)
            {
                if (i == index) continue;
                if (inventory[i].Property == itemProperty)
                {
                    if (inventory[i].Count >= amountNeeded)
                    {
                        return true;
                    }
                    else
                    {
                        amountNeeded -= inventory[i].Count;
                    }
                }
            }

            return false;
        }
    }

    public void ConsumeItemOnClient(int index, uint amount = 1)
    {
        if (!IsOwner) return;
        if (index < 0 || index >= inventory.Length) return;

        var itemStack = inventory[index];
        var itemProperty = itemStack.Property;

        if (itemStack.Count - amount > 0)
        {
            itemStack.Count -= amount;
            inventoryUI.UpdateSlot(index, itemStack.Property.Sprite, (int)itemStack.Count);
        }
        else
        {
            // Remove the current item stack
            itemStack.EmptyStack();

            // Update the UI
            inventoryUI.UpdateSlot(index, null, 0);

            // If the player is holding the item, switch to the default hand
            if (index == currentHotbarIndex) CurrentItem.Value = itemRefs[0];

            // Remove the item reference on the server
            RemoveItemRefServerRpc(index);

            // TODO: Fulfill the remaining amount by consuming from other stacks

            // Wrong Implementation
            /*var amountNeeded = amount - itemStack.Count;

            // Remove the current item stack
            itemStack.EmptyStack();

            // Update the UI
            inventoryUI.UpdateSlot(index, null, 0);

            // If the player is holding the item, switch to the default hand
            if (index == currentHotbarIndex) CurrentItem.Value = itemRefs[0];

            // Remove the item reference on the server
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
            }*/
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
        var itemObject = Instantiate(property.Prefab, transform);
        itemObject.transform.localPosition = inventoryOffset;

        var networkObject = itemObject.GetComponent<NetworkObject>();
        networkObject.Spawn();
        networkObject.TrySetParent(transform);

        var item = itemObject.GetComponent<Item>();
        item.PropertyValue = property;
        itemRefs[index] = item;

        UpdateItemRefOwnerRpc(index, item);
    }

    [Rpc(SendTo.Owner)]
    private void UpdateItemRefOwnerRpc(int index, NetworkBehaviourReference itemRef)
    {
        if (itemRef.TryGet(out Item item))
        {
            itemRefs[index] = item;
            item.transform.localPosition = inventoryOffset;
        }
    }

    public void SwapItems(int index1, int index2)
    {
        if (index1 < 0 || index1 >= inventory.Length || index2 < 0 || index2 >= inventory.Length) return;

        var stack = inventory[index1];
        inventory[index1] = inventory[index2];
        inventory[index2] = stack;

        var itemRef = itemRefs[index1];
        itemRefs[index1] = itemRefs[index2];
        itemRefs[index2] = itemRef;

        if (!inventory[index1].IsStackEmpty)
            inventoryUI.UpdateSlot(index1, inventory[index1].Property.Sprite, (int)inventory[index1].Count);
        else
            inventoryUI.UpdateSlot(index1, null, 0);

        if (!inventory[index2].IsStackEmpty)
            inventoryUI.UpdateSlot(index2, inventory[index2].Property.Sprite, (int)inventory[index2].Count);
        else
            inventoryUI.UpdateSlot(index2, null, 0);

        if (index1 == currentHotbarIndex || index2 == currentHotbarIndex)
        {
            ChangeHotBarIndex(currentHotbarIndex);
        }
    }

    public void DropItem(int index)
    {
        if (inventory[index].IsStackEmpty || index == 0) return;

        var property = inventory[index].Property;
        ConsumeItemOnClient(index);
        AssetManager.Main.SpawnItem(property, transform.position, default, gameObject);
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

    public void ChangeHotBarIndex(int index)
    {
        currentHotbarIndex = index;
        if (itemRefs[index] != null)
        {
            currentItemOnLocal = itemRefs[index];
            CurrentItem.Value = itemRefs[index];

            var selectSound = itemRefs[index].PropertyValue.SelectSound;
            AudioManager.Main.PlayOneShot(selectSound);
        }
        else
        {
            currentItemOnLocal = itemRefs[0];
            CurrentItem.Value = itemRefs[0];
        }

        inventoryUI.SelectSlot(index);
    }

    public void AddCoinsOnClient(uint value)
    {
        Wallet.Value += value;
        inventoryUI.UpdateWallet(Wallet.Value);
        if (showDebug) Debug.Log($"Added {value} coins to inventory. Total coins = {Wallet.Value}");
    }

    public void ConsumeCoinsOnClient(ulong value)
    {
        if (value > Wallet.Value)
        {
            if (showDebug) Debug.Log($"Not enough coins to consume {value}. Total coins = {Wallet.Value}");
            return;
        }

        Wallet.Value -= value;
        inventoryUI.UpdateWallet(Wallet.Value);
        if (showDebug) Debug.Log($"Consumed {value} coins from inventory. Total coins = {Wallet.Value}");
    }

    public void SetControllable(bool value)
    {
        isControllable = value;
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.blue;
        var inventoryPos = transform.position + inventoryOffset;
        Gizmos.DrawWireSphere(inventoryPos, inventoryRadius);
        Handles.Label(inventoryPos.Add(inventoryRadius), "Inventory\nRadius");
    }

#endif

}