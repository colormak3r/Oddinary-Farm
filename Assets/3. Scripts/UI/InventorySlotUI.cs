using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Image itemImage;
    [SerializeField]
    private Image slotImage;
    [SerializeField]
    private TMP_Text itemIndex;
    [SerializeField]
    private TMP_Text itemAmount;
    [SerializeField]
    private Sprite selectedSprite;
    [SerializeField]
    private Sprite unselectedSprite;

    public void Initialize(int index)
    {
        itemIndex.text = index.ToString();
        if (index > 9)
        {
            itemIndex.gameObject.SetActive(false);
        }

        if (index != 0)
            UpdateSlot(null, 0);
    }

    public void UpdateSlot(Sprite sprite, int amount)
    {
        itemImage.sprite = sprite;
        itemImage.enabled = sprite != null;

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
}
