using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ShopMode
{
    Buy,
    Sell
}

public class ShopUI : UIBehaviour
{
    public static ShopUI Main;

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
    private GameObject shopButtonPrefab;
    [SerializeField]
    private Transform contentContainer;
    [SerializeField]
    private TMP_Text shopNameText;
    [SerializeField]
    private TMP_Text coinText;
    [SerializeField]
    private Image buyImage;
    [SerializeField]
    private Image sellImage;
    [SerializeField]
    private Sprite selectedSprite;
    [SerializeField]
    private Sprite unselectedSprite;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebug;
    [SerializeField]
    private ShopMode shopMode = ShopMode.Buy;

    private PlayerInventory playerInventory;
    private ShopInventory shopInventory;
    private ItemSpawner shopSpawner;

    public void ShopModeBuy()
    {
        shopMode = ShopMode.Buy;
        buyImage.sprite = selectedSprite;
        sellImage.sprite = unselectedSprite;

        foreach (Transform child in contentContainer)
        {
            child.GetComponent<ShopButton>().Remove();
        }

        foreach (var entry in shopInventory.ItemProperties)
        {
            var shopButton = Instantiate(shopButtonPrefab, contentContainer);
            shopButton.GetComponent<ShopButton>().SetShopEntry(entry, this, shopMode, 1f, 0, 0);
        }
    }

    public void ShopModeSell()
    {
        shopMode = ShopMode.Sell;
        buyImage.sprite = unselectedSprite;
        sellImage.sprite = selectedSprite;

        foreach (Transform child in contentContainer)
        {
            child.GetComponent<ShopButton>().Remove();
        }

        var index = 0;
        foreach (var entry in playerInventory.Inventory)
        {
            if (!entry.IsStackEmpty)
            {
                var shopButton = Instantiate(shopButtonPrefab, contentContainer);
                shopButton.GetComponent<ShopButton>().SetShopEntry(entry.Property, this, shopMode, shopInventory.PenaltyMultiplier, index, entry.Count);
            }
            index++;
        }
    }

    public void OpenShop(PlayerInventory playerInventory, ShopInventory shopInventory, ItemSpawner shopSpawner)
    {
        if (IsAnimating) return;
        if (IsShowing) return;

        this.shopInventory = shopInventory;
        shopNameText.text = shopInventory.ShopName;

        this.shopSpawner = shopSpawner;

        this.playerInventory = playerInventory;
        HandleCoinValueChanged(playerInventory.CoinsValue);
        playerInventory.OnCoinsValueChanged.AddListener(HandleCoinValueChanged);

        InventoryUI.Main.CloseInventory();

        if (shopMode == ShopMode.Buy)
        {
            ShopModeBuy();
        }
        else
        {
            ShopModeSell();
        }

        StartCoroutine(ShowCoroutine());
    }

    public void CloseShop()
    {
        if (IsAnimating) return;
        if (!IsShowing) return;

        playerInventory.OnCoinsValueChanged.RemoveListener(HandleCoinValueChanged);
        playerInventory = null;
        shopSpawner = null;
        shopInventory = null;

        StartCoroutine(CloseShopCoroutine());
    }

    private IEnumerator CloseShopCoroutine()
    {
        foreach (Transform child in contentContainer)
        {
            child.GetComponent<ShopButton>().Remove();
        }
        yield return null;
        yield return UnShowCoroutine();
    }

    public void HandleOnButtonClick(ItemProperty itemProperty, ShopButton button, int index)
    {
        if (shopMode == ShopMode.Buy)
        {
            var price = itemProperty.Price;
            if (playerInventory.CoinsValue < price)
            {
                if (showDebug) Debug.Log($"Cannot afford {itemProperty.Name}");
            }
            else
            {
                playerInventory.ConsumeCoinsOnClient(itemProperty.Price);
                if (!playerInventory.AddItemOnClient(itemProperty))
                {
                    shopSpawner.Spawn(itemProperty, shopSpawner.transform.position - new Vector3(0, 1), 0.5f, false);
                }

                if (showDebug) Debug.Log($"Bought 1x{itemProperty.Name} for {itemProperty.Price}");
            }
        }
        else
        {
            playerInventory.ConsumeItemOnClient(index);
            var value = (uint)Mathf.CeilToInt((float)itemProperty.Price * shopInventory.PenaltyMultiplier);
            playerInventory.AddCoinsOnClient(value);

            button.UpdateEntry(playerInventory.Inventory[index].Count);

            if (playerInventory.Inventory[index].IsStackEmpty)
                button.Remove();
        }
    }

    private void HandleCoinValueChanged(ulong value)
    {
        coinText.text = "$" + value;
    }
}
