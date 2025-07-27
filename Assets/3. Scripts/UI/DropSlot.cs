using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropSlot : MonoBehaviour, IDropHandler
{
    private PlayerInventory playerInventory;

    public void Initialize(PlayerInventory playerInventory)
    {
        this.playerInventory = playerInventory;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            InventoryItemUI item = eventData.pointerDrag.GetComponent<InventoryItemUI>();
            if (item != null && item.IsInteractable)
            {
                playerInventory.DropStack(item.Index);
            }
        }

        //gameObject.SetActive(false);
    }
}
