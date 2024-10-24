using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    private PlayerInventory playerInventory;
    private ItemSpawner shopSpawner;

    public void OpenShop(PlayerInventory playerInventory, ShopInventory shopInventory, ItemSpawner shopSpawner)
    {
        if (IsAnimating) return;
        if (IsShowing) return;

        shopNameText.text = shopInventory.ShopName;
        this.shopSpawner = shopSpawner;

        this.playerInventory = playerInventory;
        HandleCoinValueChanged(playerInventory.CoinsValue);
        playerInventory.OnCoinsValueChanged.AddListener(HandleCoinValueChanged);

        foreach (var entry in shopInventory.ShopEntries)
        {
            var shopButton = Instantiate(shopButtonPrefab, contentContainer);
            shopButton.GetComponent<ShopButton>().SetShopEntry(entry, this);
        }

        StartCoroutine(ShowCoroutine());
    }

    public void CloseShop()
    {
        if (IsAnimating) return;
        if (!IsShowing) return;

        playerInventory.OnCoinsValueChanged.RemoveListener(HandleCoinValueChanged);
        playerInventory = null;

        this.shopSpawner = null;

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

    public void HandleOnButtonClick(ShopEntry shopEntry)
    {
        //Debug.Log($"Shop Entry {shopEntry.Item} Clicked");
        var price = shopEntry.Price;
        if (playerInventory.CoinsValue - price < 0)
        {
            Debug.Log($"Cannot afford {shopEntry.Item}");
            return;
        }
        playerInventory.ConsumeCoins(shopEntry.Price);
        if (!playerInventory.AddItemOnClient(shopEntry.Item))
        {
            shopSpawner.Spawn(shopEntry.Item, 0.5f, false, shopSpawner.transform.position - new Vector3(0, 1));
        }
        //Debug.Log($"Bought {shopEntry.Item}");
    }

    private void HandleCoinValueChanged(ulong value)
    {
        coinText.text = "$" + value;
    }
}
