using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
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

    [Header("Shop UI Settings")]
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

    [Header("Shop UI Debugs")]
    [SerializeField]
    private bool showDebug;
    [SerializeField]
    private ShopMode shopMode = ShopMode.Buy;

    private PlayerInventory playerInventory;
    private ShopInventory shopInventory;
    private Transform shopTransform;

    public void ShopModeBuy()
    {
        shopMode = ShopMode.Buy;
        buyImage.sprite = selectedSprite;
        sellImage.sprite = unselectedSprite;
        var playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        foreach (Transform child in contentContainer)
        {
            child.GetComponent<ShopButton>().Remove();
        }

        foreach (var entry in shopInventory.ItemProperties)
        {
            var shopButton = Instantiate(shopButtonPrefab, contentContainer);
            shopButton.GetComponent<ShopButton>().SetShopEntry(entry, this, shopMode, playerCount, 0, 0);
        }
    }

    public void ShopModeSell()
    {
        shopMode = ShopMode.Sell;
        buyImage.sprite = unselectedSprite;
        sellImage.sprite = selectedSprite;
        var playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        foreach (Transform child in contentContainer)
        {
            child.GetComponent<ShopButton>().Remove();
        }

        var index = 0;
        foreach (var slot in playerInventory.Inventory)
        {
            if (!slot.IsEmpty && slot.Property.IsSellable)
            {
                var shopButton = Instantiate(shopButtonPrefab, contentContainer);
                shopButton.GetComponent<ShopButton>().SetShopEntry(slot.Property, this, shopMode, playerCount * shopInventory.PenaltyMultiplier, index, slot.Count);
            }
            index++;
        }
    }

    public void OpenShop(PlayerInventory playerInventory, ShopInventory shopInventory, Transform shopTransform)
    {
        if (IsAnimating) return;
        if (IsShowing) return;

        this.shopInventory = shopInventory;
        shopNameText.text = shopInventory.ShopName;

        this.shopTransform = shopTransform;

        this.playerInventory = playerInventory;

        HandleCoinValueChanged(WalletManager.Main.LocalWallet);
        playerInventory.OnCoinsValueChanged.AddListener(HandleCoinValueChanged);

        InventoryUI.Main.CloseInventory();

        // Always default to buy mode
        ShopModeBuy();
        /*if (shopMode == ShopMode.Buy)
        {
            ShopModeBuy();
        }
        else
        {
            ShopModeSell();
        }*/

        StartCoroutine(ShowCoroutine());
    }

    public void CloseShop()
    {
        if (IsAnimating) return;
        if (!IsShowing) return;

        playerInventory.OnCoinsValueChanged.RemoveListener(HandleCoinValueChanged);
        playerInventory = null;
        shopInventory = null;
        shopTransform = null;

        StartCoroutine(CloseShopCoroutine());
    }

    private IEnumerator CloseShopCoroutine()
    {
        foreach (Transform child in contentContainer)
        {
            child.GetComponent<ShopButton>().Remove();
        }
        yield return null;
        yield return HideCoroutine();
    }

    public void HandleOnButtonClick(ItemProperty itemProperty, ShopButton button, int index)
    {
        var playerCount = (uint)NetworkManager.Singleton.ConnectedClients.Count;
        var price = itemProperty.Price * playerCount;
        if (shopMode == ShopMode.Buy)
        {
            if (WalletManager.Main.LocalWallet < price)
            {
                if (showDebug) Debug.Log($"Cannot afford {itemProperty.Name}");
            }
            else
            {
                playerInventory.ConsumeCoinsOnClient(price);
                if (!playerInventory.AddItem(itemProperty))
                {
                    AssetManager.Main.SpawnItem(itemProperty, shopTransform.transform.position - new Vector3(0, 1), default, default, 0.5f, false);
                }

                if (showDebug) Debug.Log($"Bought 1x{itemProperty.Name} for {price}");
            }
        }
        else
        {
            playerInventory.ConsumeItemOnClient(index);
            var value = (uint)Mathf.CeilToInt(price * shopInventory.PenaltyMultiplier);
            playerInventory.AddCoinsOnClient(value);

            button.UpdateEntry(playerInventory.Inventory[index].Count);

            if (playerInventory.Inventory[index].IsEmpty)
                button.Remove();
        }
    }

    private void HandleCoinValueChanged(ulong value)
    {
        coinText.text = "$" + value;
    }
}
