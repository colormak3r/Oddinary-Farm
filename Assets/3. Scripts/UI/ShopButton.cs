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
    private TMP_Text amountText;
    [SerializeField]
    private Button button;

    private ItemProperty itemProperty;

    public void SetShopEntry(ItemProperty itemProperty, ShopUI shopUI, ShopMode shopMode, int index, uint amount)
    {
        this.itemProperty = itemProperty;

        displayImage.sprite = itemProperty.Sprite;
        nameText.text = itemProperty.Name;
        priceText.text = (shopMode == ShopMode.Buy ? "<color=#a53030>" : " <color=#75a743>")
            + (shopMode == ShopMode.Buy ? "-" : "+") + "$" + itemProperty.Price;
        amountText.text = amount == 0 ? "" : "x" + amount;

        button.onClick.AddListener(() => shopUI.HandleOnButtonClick(itemProperty, this, index));
    }

    public void UpdateEntry(uint amount)
    {
        amountText.text = amount == 0 ? "" : "x" + amount;
    }

    public void Remove()
    {
        button.onClick.RemoveAllListeners();
        Destroy(gameObject);
    }
}
