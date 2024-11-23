using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : UIBehaviour
{
    public static InventoryUI Main;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Settings")]
    [SerializeField]
    private InventorySlotUI[] inventorySlots;

    private int selectedIndex;

    private void Start()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            inventorySlots[i].Initialize(i);
        }
    }

    public void UpdateSlot(int index, Sprite sprite, int amount)
    {
        inventorySlots[index].UpdateSlot(sprite, amount);
    }

    public void SelectSlot(int index)
    {
        inventorySlots[selectedIndex].SelectSlot(false);
        selectedIndex = index;
        inventorySlots[selectedIndex].SelectSlot(true);
    }
}
