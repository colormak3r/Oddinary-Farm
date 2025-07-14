/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/12/2025 (Khoa)
 * Notes:           <write here>
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Image displayImage;
    [SerializeField]
    private TMP_Text priceText;
    [SerializeField]
    private TMP_Text amountText;
    [SerializeField]
    private Button shopButton;
    [SerializeField]
    private Button quickActionButton;
    [SerializeField]
    private TMP_Text quickActionText;

    private ItemProperty itemProperty;

    public void SetShopEntry(ItemProperty itemProperty, ShopUI shopUI, ShopMode shopMode, float multiplier, int index, uint amount)
    {
        this.itemProperty = itemProperty;

        displayImage.sprite = itemProperty.IconSprite;
        var value = (uint)Mathf.Max(Mathf.CeilToInt(itemProperty.Price * multiplier), 1);
        priceText.text = (shopMode == ShopMode.Buy ? "<color=#ae2334>" : " <color=#239063>")
            + (shopMode == ShopMode.Buy ? "-" : "+") + "$" + value;
        quickActionText.text = (shopMode == ShopMode.Buy ? "Quick Buy" : "Quick Sell");
        amountText.text = amount == 0 ? "" : amount.ToString();

        shopButton.onClick.AddListener(() => shopUI.HandleShopButtonClicked(itemProperty, this, index));
        quickActionButton.onClick.AddListener(() => shopUI.HandleQuickActionClicked(itemProperty, this, index));
    }

    public void UpdateEntry(uint amount)
    {
        amountText.text = amount == 0 ? "" : amount.ToString();
    }

    public void Remove()
    {
        quickActionButton.onClick.RemoveAllListeners();
        Destroy(gameObject);
    }
}
