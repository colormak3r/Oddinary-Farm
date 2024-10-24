using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Image displayImage;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text priceText;
    [SerializeField]
    private Button button;

    private ShopEntry shopEntry;

    public void SetShopEntry(ShopEntry shopEntry, ShopUI shopUI)
    {
        this.shopEntry = shopEntry;

        displayImage.sprite = shopEntry.Item.Sprite;
        nameText.text = shopEntry.Item.Name;
        priceText.text = "$" + shopEntry.Price;

        button.onClick.AddListener(() => shopUI.HandleOnButtonClick(shopEntry));
    }

    public void Remove()
    {
        button.onClick.RemoveAllListeners();
        Destroy(gameObject);
    }
}
