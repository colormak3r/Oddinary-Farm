/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/22/2025 (Khoa)
 * Notes:           <write here>
*/

using Unity.Netcode;
using System;
using UnityEngine;
using NUnit.Framework;
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
public struct InventorySlot
{
    public Item Item;
    public uint Count;

    public ItemProperty Property => Item?.BaseProperty;
    public bool IsFull => Item != null && Count >= Item.BaseProperty.MaxStack;
    public bool IsEmpty => Item == null || Count == 0;

    public InventorySlot(Item item, uint count = 1)
    {
        Item = item;
        Count = count;
    }
}

// Must include this line [GenerateSerializationForType(typeof(byte))] somewhere in the project
// See: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/2920#issuecomment-2173886545
// [GenerateSerializationForType(typeof(byte))]
public class PlayerInventory : NetworkBehaviour, IControllable
{
    private static int MAX_INVENTORY_SLOTS = 30;

    [Header("Inventory Settings")]
    [SerializeField]
    private HandProperty handProperty;
    [SerializeField]
    private Transform inventoryTransform;
    [SerializeField]
    private ItemStack[] defaultInventory;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebug;
    [SerializeField]
    private bool isControllable = true;
    [SerializeField]
    private InventorySlot[] inventory = new InventorySlot[MAX_INVENTORY_SLOTS];
    public InventorySlot[] Inventory => inventory;

    private int currentHotbarIndex;
    public int CurrentHotbarIndex => currentHotbarIndex;


    private NetworkVariable<ItemProperty> CurrentItemProperty = new NetworkVariable<ItemProperty>(default, default, NetworkVariableWritePermission.Owner);
    public Action<ItemProperty> OnCurrentItemPropertyChanged;
    private Item currentItem;
    public Action<Item> OnCurrentItemChanged;

    private InventoryUI inventoryUI;
    private ItemContextUI itemContextUI;
    private AudioElement audioElement;
    private RangeIndicator rangeIndicator;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Initialize the inventory UI
            inventoryUI = InventoryUI.Main;
            inventoryUI.Initialize(this);
            audioElement = GetComponent<AudioElement>();
            rangeIndicator = GetComponent<RangeIndicator>();

            // Initialize context UI
            itemContextUI = ItemContextUI.Main;

            //Add the hand to the inventory
            AddItem(handProperty, false);

            // Add the default items to the inventory
            foreach (var itemStack in defaultInventory)
            {
                for (int i = 0; i < itemStack.Count; i++)
                    AddItem(itemStack.Property, false);
            }

            // Set the current item to the Hand
            ChangeHotBarIndex(0);

            WalletManager.Main.OnLocalWalletChanged += HandleLocalWalletChanged;
            HandleLocalWalletChanged(WalletManager.Main.LocalWalletValue);
        }

        CurrentItemProperty.OnValueChanged += HandleCurrentItemChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentItemProperty.OnValueChanged -= HandleCurrentItemChanged;

        if (IsServer)
        {
            // Clean up when a client despawns
            foreach (var slot in inventory)
            {
                if (slot.Item != null)
                {
                    var netObj = slot.Item.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned) netObj.Despawn();
                }
            }
        }

        if (IsOwner)
        {
            WalletManager.Main.OnLocalWalletChanged -= HandleLocalWalletChanged;
        }
    }

    private void HandleLocalWalletChanged(ulong newValue)
    {
        if (showDebug) Debug.Log($"Local wallet updated: {newValue}");
        inventoryUI.UpdateWallet(newValue);
    }

    private void HandleCurrentItemChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        OnCurrentItemPropertyChanged?.Invoke(newValue);
    }

    public bool CanAddItem(ItemProperty property)
    {
        if (property is CurrencyProperty)
        {
            return true;    // Currency can always be added
        }
        else if (property.MaxStack > 1 && FindPartialSlot(property, out var partialIndex))
        {
            return true;    // Found a stack that is not full
        }
        else if (FindEmptySlot(out var emptyIndex))
        {
            return true;    // Found an empty slot
        }
        else
        {
            return false;   // No space in inventory
        }
    }

    public bool AddItem(ItemProperty property, bool playSound = true)
    {
        if (showDebug) Debug.Log($"Adding {property.ItemName} to inventory on client {OwnerClientId}");

        // Convert carrot to golden carrot
        if (isGoldenCarrot && property == AssetManager.Main.CarrotProperty)
        {
            property = AssetManager.Main.GoldenCarrotProperty;
            if (showDebug) Debug.Log("Converted carrot to golden carrot for inventory addition.");
        }

        if (property is CurrencyProperty)
        {
            var currency = property as CurrencyProperty;
            var value = currency.Value;
            AddCoinsOnClient(value);

            if (playSound) audioElement.PlayOneShot(property.PickupSound);

            // Update stat
            StatisticsManager.Main.UpdateStat(StatisticType.ItemsCollected, property);

            return true;
        }
        else if (property.MaxStack > 1 && FindPartialSlot(property, out var partialIndex))
        {
            inventory[partialIndex].Count++;

            inventoryUI.UpdateSlot(partialIndex, property.IconSprite, (int)inventory[partialIndex].Count);

            if (playSound) audioElement.PlayOneShot(property.PickupSound);

            if (partialIndex == currentHotbarIndex)
            {
                ChangeHotBarIndex(partialIndex);
            }

            // Update stat
            StatisticsManager.Main.UpdateStat(StatisticType.ItemsCollected, property);

            return true;
        }
        else if (FindEmptySlot(out var emptyIndex))
        {
            // Create and initialize the item
            var obj = Instantiate(property.Prefab, inventoryTransform);
            var item = obj.GetComponent<Item>();
            item.Initialize(property);

            // Add the item to the inventory
            inventory[emptyIndex] = new InventorySlot(item);

            // Update the UI
            inventoryUI.UpdateSlot(emptyIndex, property.IconSprite, 1);

            if (playSound) audioElement.PlayOneShot(property.PickupSound);

            if (emptyIndex == currentHotbarIndex)
            {
                ChangeHotBarIndex(emptyIndex);
            }

            // Update stat
            StatisticsManager.Main.UpdateStat(StatisticType.ItemsCollected, property);

            return true;
        }

        return false;
    }

    public bool CanConsumeItemOnClient(int index)
    {
        return !inventory[index].IsEmpty;
    }

    public void ConsumeItemOnClient(int index)
    {
        inventory[index].Count--;
        if (inventory[index].Count == 0)
        {
            ClearInventorySlot(index);
        }
        else
        {
            inventoryUI.UpdateSlot(index, inventory[index].Property.IconSprite, (int)inventory[index].Count);
        }
    }

    private void ClearInventorySlot(int index)
    {
        if (inventory[index].Item != null && inventory[index].Item.gameObject != null)
            Destroy(inventory[index].Item.gameObject);
        inventory[index].Item = null;
        inventoryUI.UpdateSlot(index, null, 0);

        // Update the visual on player's hand if the current item is being cleared
        if (index == currentHotbarIndex) ChangeHotBarIndex(currentHotbarIndex);
    }

    public void DropItem(int index)
    {
        if (inventory[index].IsEmpty || index == 0) return;

        var property = inventory[index].Property;
        ConsumeItemOnClient(index);
        AssetManager.Main.SpawnItem(property, transform.position, default, gameObject);
    }

    public void SwapItem(int index1, int index2)
    {
        if (index1 == index2) return;   // same slot? nothing to do

        // 1. Try to merge if they hold the same item and neither is empty
        if (!inventory[index1].IsEmpty &&
        !inventory[index2].IsEmpty &&
        inventory[index1].Property == inventory[index2].Property)
        {
            var maxStack = inventory[index1].Property.MaxStack;
            if (inventory[index1].Count + inventory[index2].Count > maxStack)
            {
                var toMove = maxStack - inventory[index1].Count;
                inventory[index1].Count = maxStack;
                inventory[index2].Count -= toMove;
            }
            else
            {
                inventory[index1].Count += inventory[index2].Count;
                inventory[index2].Count = 0;
            }
        }
        else
        {
            // 2. Swap the items in the inventory
            var temp = inventory[index1];
            inventory[index1] = inventory[index2];
            inventory[index2] = temp;
        }

        if (!inventory[index1].IsEmpty)
            inventoryUI.UpdateSlot(index1, inventory[index1].Property.IconSprite, (int)inventory[index1].Count);
        else
            ClearInventorySlot(index1);

        if (!inventory[index2].IsEmpty)
            inventoryUI.UpdateSlot(index2, inventory[index2].Property.IconSprite, (int)inventory[index2].Count);
        else
            ClearInventorySlot(index2);

        ChangeHotBarIndex(currentHotbarIndex);
    }

    public void DropStack(int index)
    {
        var count = (int)inventory[index].Count;
        var property = inventory[index].Property;
        ClearInventorySlot(index);
        AssetManager.Main.SpawnItem(property, transform.position, default, gameObject, default, default, count);
    }

    #region Search Algorithms
    private bool FindPartialSlot(ItemProperty property, out int index)
    {
        index = -1;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Property == property && inventory[i].Count < property.MaxStack)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    public bool FindSlot(ItemProperty property, out int index)
    {
        index = -1;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Property == property)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    private bool FindEmptySlot(out int index)
    {
        index = -1;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Property == null)
            {
                index = i;
                return true;
            }
        }

        return false;
    }
    #endregion

    #region Currency
    public void AddCoinsOnClient(uint value)
    {
        WalletManager.Main.AddToWallet(value);
        if (showDebug) Debug.Log($"Added {value} coins to inventory. Total coins = {WalletManager.Main.LocalWalletValue}");
    }

    private ulong personalCoinsSpent = 0;
    public void ConsumeCoinsOnClient(uint value)
    {
        if (value > WalletManager.Main.LocalWalletValue)
        {
            if (showDebug) Debug.Log($"Not enough coins to consume {value}. Total coins = {WalletManager.Main.LocalWalletValue}");
            return;
        }

        personalCoinsSpent += value;

        WalletManager.Main.RemoveFromWallet(value);
        StatisticsManager.Main.UpdateStat(StatisticType.PersonalCoinsSpent, personalCoinsSpent);
        if (showDebug) Debug.Log($"Consumed {value} coins from inventory. Total coins = {WalletManager.Main.LocalWalletValue}");
    }
    #endregion

    public void ChangeHotBarIndex(int index)
    {
        currentHotbarIndex = index;
        var currentSlot = inventory[currentHotbarIndex];
        if (currentSlot.Item == null)
        {
            // If the selected slot is empty, switch to the hand
            currentSlot = inventory[0];
        }

        // Update the current item reference
        CurrentItemProperty.Value = currentSlot.Property;
        currentItem = currentSlot.Item;
        OnCurrentItemChanged?.Invoke(currentItem);

        // Play the select sound
        if (!currentSlot.IsEmpty)
            AudioManager.Main.PlayOneShot(currentSlot.Property.SelectSound);

        // UI update
        inventoryUI.SelectSlot(index);
        rangeIndicator.Show(currentSlot.Property.Range);
        itemContextUI.SetItemContext(currentSlot.Property.ItemContext);
    }

    #region Golden Carrot

    private void ConvertCarrotToGolden()
    {
        var carrotProperty = AssetManager.Main.CarrotProperty;
        var goldenCarrotProperty = AssetManager.Main.GoldenCarrotProperty;

        List<int> convertedIndices = new List<int>();
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Property == carrotProperty && inventory[i].Count > 0)
                convertedIndices.Add(i);
        }

        foreach (var index in convertedIndices)
        {
            var count = inventory[index].Count;

            // Clear the carrot
            ClearInventorySlot(index);
            if (showDebug) Debug.Log($"Converted carrot at index {index} to golden carrot. Count: {count}");

            for (int i = 0; i < count; i++)
            {
                // Add the golden carrot
                if (AddItem(goldenCarrotProperty, false))
                {
                    if (showDebug) Debug.Log($"Converted carrot at index {index} to golden carrot.");
                }
                else
                {
                    // This should rarely happen, but if we can't add the golden carrot, we log a warning and spawn it in the world
                    Debug.LogWarning($"Failed to convert carrot at index {index} to golden carrot. No space in inventory.");
                    AssetManager.Main.SpawnItem(goldenCarrotProperty, transform.position, default, gameObject);
                }
            }
        }
    }

    private bool isGoldenCarrot;
    public void SetGoldenCarrot(bool value)
    {
        isGoldenCarrot = value;

        if (isGoldenCarrot)
        {
            // Convert all carrots to golden carrots
            ConvertCarrotToGolden();
        }
        else
        {
            // If we are not golden carrot, we can do something else if needed
            // For now, just log it
            if (showDebug) Debug.Log("Switched back from Golden Carrot to regular Carrot.");
        }
    }

    #endregion

    #region Utility

    public uint GetItemCount(ItemProperty itemProperty)
    {
        uint count = 0;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Property == itemProperty)
            {
                count += inventory[i].Count;
            }
        }
        return count;
    }

    public void SetControllable(bool value)
    {
        isControllable = value;
    }

    #endregion
}