using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    [Header("Settings")]
    [SerializeField]
    private bool isInteractable = true;
    public bool IsInteractable => isInteractable;
    [SerializeField]
    private Image slotImage;
    [SerializeField]
    private InventoryItemUI itemUI;
    [SerializeField]
    private TMP_Text itemIndex;
    [SerializeField]
    private TMP_Text itemAmount;
    [SerializeField]
    private Sprite selectedSprite;
    [SerializeField]
    private Sprite unselectedSprite;

    private int index;
    private PlayerInventory playerInventory;

    public void Initialize(int index, PlayerInventory playerInventory)
    {
        this.index = index;
        this.playerInventory = playerInventory;

        itemIndex.text = index.ToString();
        itemUI.Initialize(index, isInteractable);

        // Hide item index if it's greater than 9 (part of the inventory)
        if (index > 9) itemIndex.gameObject.SetActive(false);

        // Hide item image and amount if it's not the Hand slot
        if (index != 0) UpdateSlot(null, 0);
    }

    public void UpdateSlot(Sprite sprite, int amount)
    {
        if (index != 0)
            itemUI.UpdateImage(sprite);
        itemAmount.text = amount == 0 ? "" : amount.ToString();
    }

    public void SelectSlot(bool value)
    {
        if (value)
        {
            slotImage.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            slotImage.sprite = selectedSprite;
        }
        else
        {
            slotImage.transform.localScale = Vector3.one;
            slotImage.sprite = unselectedSprite;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!isInteractable) return;

        if (eventData.pointerDrag != null)
        {
            InventoryItemUI item = eventData.pointerDrag.GetComponent<InventoryItemUI>();
            if (item != null && item.IsInteractable)
            {
                playerInventory.SwapItem(item.Index, index);
            }
        }
    }
}
