using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryUI : UIBehaviour
{
    public static InventoryUI Main;

    private static Vector3 ONE_POINT_ONE_VECTOR = new Vector3(1.1f, 1.1f, 1.1f);

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
    private UIBehaviour inventoryBehaviour;
    [SerializeField]
    private TMP_Text walletText;
    [SerializeField]
    private CanvasRenderer walletUI;
    [SerializeField]
    private InventorySlotUI[] inventorySlots;

    private int selectedIndex;
    private Coroutine popCoroutine;

    public void Initialize(PlayerInventory playerInventory)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            inventorySlots[i].Initialize(i, playerInventory);
        }
    }

    public void UpdateSlot(int index, Sprite sprite, int amount)
    {
        inventorySlots[index].UpdateSlot(sprite, amount);
    }

    public void UpdateWallet(ulong amount)
    {
        if (amount > 1000000)
        {
            walletText.text = (amount / 1000000).ToString() + "M";

        }
        else
        {
            walletText.text = amount.ToString();
        }

        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(walletUI.UIPopCoroutine(Vector3.one, ONE_POINT_ONE_VECTOR, 0.1f));
    }

    public void SelectSlot(int index)
    {
        inventorySlots[selectedIndex].SelectSlot(false);
        selectedIndex = index;
        inventorySlots[selectedIndex].SelectSlot(true);
    }

    public void ToggleInventory()
    {
        inventoryBehaviour.ToggleShow();
    }

    public void CloseInventory()
    {
        StartCoroutine(inventoryBehaviour.HideCoroutine());
    }
}
