/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/12/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using ColorMak3r.Utility;
using System.Collections.Generic;

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

        foreach (var item in hideInBuild)
        {
            item.SetActive(Application.isEditor);
        }

        foreach (var item in visitedItemPreset)
        {
            visitedItemProperties.Add(item);
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
    private CanvasRenderer coinUI;
    [SerializeField]
    private GameObject itemDropPrefab;
    [SerializeField]
    private ItemProperty[] visitedItemPreset;

    [Header("Item Info Settings")]
    [SerializeField]
    private Image itemImage;
    [SerializeField]
    private TMP_Text itemNameText;
    [SerializeField]
    private TMP_Text itemDescriptionText;
    [SerializeField]
    private GameObject buyStackButton;
    [SerializeField]
    private TMP_Text stackActionText;
    [SerializeField]
    private GameObject buyMultiplierButton;
    [SerializeField]
    private TMP_Text multiplierActionText;
    [SerializeField]
    private TMP_Text ownedText;
    [SerializeField]
    private GameObject itemPanel;
    [SerializeField]
    private GameObject displayPanel;

    [Header("Upgrade Settings")]
    [SerializeField]
    private GameObject upgradePanel;
    [SerializeField]
    private GameObject upgradeButton;
    [SerializeField]
    private TMP_Text upgradeButtonText;
    [SerializeField]
    private TMP_Text upgradeText;
    [SerializeField]
    private TMP_Text tierText;

    [Header("Buy/Sell Settings")]
    [SerializeField]
    private Image buyImage;
    [SerializeField]
    private Image sellImage;
    [SerializeField]
    private Sprite selectedSprite;
    [SerializeField]
    private Sprite unselectedSprite;

    [Header("Shop Audio")]
    [SerializeField]
    private AudioClip openShopSound;
    [SerializeField]
    private AudioClip buySound;
    [SerializeField]
    private AudioClip sellSound;
    [SerializeField]
    private AudioClip errorSound;

    [Header("Shop UI Debugs")]
    [SerializeField]
    private bool showDebug;
    [SerializeField]
    private ShopMode shopMode = ShopMode.Buy;
    [SerializeField]
    private GameObject[] hideInBuild;

    private ShopButton currentShopButton;
    [SerializeField]
    private ItemProperty currentItemProperty;
    [SerializeField]
    private int currentItemIndex = -1;
    [SerializeField]
    private int currentMultiplier = 5;

    private PlayerInventory playerInventory;
    private ShopInventory shopInventory;
    private Transform shopTransform;
    private Coroutine dropItemCoroutine;
    private Dictionary<ShopInventory, int> shopInventoryToCurrentTier = new Dictionary<ShopInventory, int>();
    private Dictionary<ShopInventory, int> shopTierDictionary_cached = new Dictionary<ShopInventory, int>();
    private HashSet<ItemProperty> visitedItemProperties = new HashSet<ItemProperty>();
    private List<ItemProperty> itemsToSpawn = new List<ItemProperty>();

    private bool recentlyOpened;

    public void OpenShop(PlayerInventory playerInventory, ShopInventory shopInventory, Transform shopTransform)
    {
        if (IsAnimating) return;
        if (IsShowing) return;

        // Set references
        this.shopInventory = shopInventory;
        this.shopTransform = shopTransform;
        this.playerInventory = playerInventory;
        shopNameText.text = shopInventory.ShopName;
        recentlyOpened = true;

        UpdateUpgradePanel();

        // Subscribe to wallet changes
        WalletManager.Main.OnLocalWalletChanged += HandleLocalWalletChanged;
        WalletManager.Main.OnGlobalWalletChanged += HandleGlobalWalletChanged;
        HandleLocalWalletChanged(WalletManager.Main.LocalWalletValue);
        HandleGlobalWalletChanged(WalletManager.Main.GlobalWalletValue);

        // Close any open inventory tab
        InventoryUI.Main.CloseTabInventory();

        // Switch UI input map to UI
        InputManager.Main.SwitchMap(InputMap.UI);

        // Always default to buy mode
        ShopModeBuy();

        AudioManager.Main.PlayClickSound();
        StartCoroutine(ShowCoroutine());
    }

    public void ShopModeBuy()
    {
        // Stop eye candy coroutines if they are running
        StopEyeCandyCoroutines();

        // Clear the selected item property when switching to buy mode
        ClearItemReference();
        itemPanel.SetActive(false);
        displayPanel.SetActive(true);

        // Buy mode initialization & UI update
        shopMode = ShopMode.Buy;
        buyImage.sprite = selectedSprite;
        sellImage.sprite = unselectedSprite;
        var playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        AudioManager.Main.PlayOneShot(openShopSound);

        ClearChild();

        // Determine if shop tier has changed
        if (!shopInventoryToCurrentTier.TryGetValue(shopInventory, out int currentTier))
        {
            shopInventoryToCurrentTier[shopInventory] = 0;
            currentTier = 0;
        }
        if (!shopTierDictionary_cached.TryGetValue(shopInventory, out int currentTier_cached))
        {
            shopTierDictionary_cached[shopInventory] = 0;
            currentTier_cached = 0;
        }

        // Old items spawn in bulk
        for (int i = 0; i <= currentTier_cached; i++)
        {
            if (shopInventory.Tiers.Length > i)
            {
                foreach (var entry in shopInventory.Tiers[i].itemProperties)
                {
                    var shopButton = Instantiate(shopButtonPrefab, contentContainer);
                    var isNew = !visitedItemProperties.Contains(entry);
                    shopButton.GetComponent<ShopButton>().SetShopEntry(entry, this, shopMode, playerCount, -1, 0, isNew);
                }
            }
        }

        // Debug.Log($"Shop Mode Buy: {shopInventory.ShopName} - Current Tier: {currentTier} (Cached: {currentTier_cached})");
        // New items spawn in coroutine
        if (currentTier != currentTier_cached)
        {
            if (shopInventory.Tiers.Length > currentTier)
            {
                itemsToSpawn.Clear();
                foreach (var entry in shopInventory.Tiers[currentTier].itemProperties)
                    itemsToSpawn.Add(entry);

                if (shopButtonBuyCoroutine != null) StopCoroutine(shopButtonBuyCoroutine);
                shopButtonBuyCoroutine = StartCoroutine(ShopButtonBuyCoroutine(playerCount));

                // Update cache
                shopTierDictionary_cached[shopInventory] = currentTier;
            }
        }
    }

    private Coroutine shopButtonBuyCoroutine;
    private IEnumerator ShopButtonBuyCoroutine(int playerCount)
    {
        foreach (var entry in itemsToSpawn)
        {
            var shopButton = Instantiate(shopButtonPrefab, contentContainer);
            shopButton.GetComponent<ShopButton>().SetShopEntry(entry, this, shopMode, playerCount, -1, 0, true);
            AudioManager.Main.PlaySoundIncreasePitch(buySound);
            yield return shopButton.transform.UIPopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.1f);
            yield return new WaitForSeconds(0.1f); // Slight delay between button instantiation
        }
        AudioManager.Main.ResetPitch();
    }

    public void ShopModeSell()
    {
        // Stop eye candy coroutines if they are running
        StopEyeCandyCoroutines();

        // Clear the selected item property when switching to sell mode
        ClearItemReference();
        itemPanel.SetActive(false);
        displayPanel.SetActive(true);

        // Sell mode initialization & UI update
        shopMode = ShopMode.Sell;
        buyImage.sprite = unselectedSprite;
        sellImage.sprite = selectedSprite;
        var playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        AudioManager.Main.PlayOneShot(openShopSound);

        ClearChild();

        var index = 0;
        foreach (var slot in playerInventory.Inventory)
        {
            if (!slot.IsEmpty && slot.Property.IsSellable)
            {
                var shopButton = Instantiate(shopButtonPrefab, contentContainer);
                shopButton.GetComponent<ShopButton>().SetShopEntry(slot.Property, this, shopMode, playerCount * shopInventory.PenaltyMultiplier, index, slot.Count, false);
                shopButton.transform.UIPopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.1f);
            }
            index++;
        }
    }

    public void OnUpgradeClicked()
    {
        var currentTier = shopInventoryToCurrentTier[shopInventory];
        var upgradeCost = (uint)shopInventory.Tiers[currentTier + 1].upgradeCost;
        if (WalletManager.Main.LocalWalletValue < upgradeCost)
        {
            // Not enough coins to upgrade
            if (showDebug) Debug.Log($"Cannot afford upgrade {shopInventory.ShopName}: ${upgradeCost}");
            StartCoroutine(coinText.transform.UIPopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.1f));
            AudioManager.Main.PlayOneShot(errorSound);
        }
        else
        {
            if (shopInventoryToCurrentTier[shopInventory] < shopInventory.Tiers.Length - 1)
                shopInventoryToCurrentTier[shopInventory]++;

            StartCoroutine(tierText.transform.UIPopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.1f));

            // Upgrade the shop inventory
            playerInventory.ConsumeCoinsOnClient(upgradeCost);

            UpdateUpgradePanel();

            if (shopMode == ShopMode.Buy)
                ShopModeBuy();
            else
                ShopModeSell();
        }
    }

    private void UpdateUpgradePanel()
    {
        if (!shopInventory) return;

        var shopTierLength = shopInventory.Tiers.Length;
        if (shopTierLength > 1)
        {
            upgradePanel.SetActive(true);
            var playerCount = (uint)NetworkManager.Singleton.ConnectedClients.Count;

            // Find the current tier based on the global wallet value
            int globalTier = 0;
            for (int i = 0; i < shopTierLength; i++)
            {
                var netIncomeNeeded = shopInventory.Tiers[i].netIncome * playerCount;
                if (WalletManager.Main.GlobalWalletValue >= netIncomeNeeded)
                    globalTier = i;
                else
                    break;
            }

            // Get the current tier, default to 0 if entry not exist
            if (!shopInventoryToCurrentTier.TryGetValue(shopInventory, out int currentTier))
            {
                shopInventoryToCurrentTier[shopInventory] = 0;
                currentTier = 0;
            }

            // Update UI
            tierText.text = $"Tier {currentTier}";
            if (currentTier < globalTier)
            {
                // The player can upgrade
                upgradeButton.SetActive(true);
                upgradeButtonText.text = $"<u>UPGRADE</u>\n${shopInventory.Tiers[currentTier + 1].upgradeCost}";
                StartCoroutine(WaitShopLoad());
                upgradeText.gameObject.SetActive(false);
            }
            else
            {
                if (currentTier + 1 < shopTierLength)
                {
                    // Show how much until next tier
                    upgradeButton.SetActive(false);
                    upgradeText.gameObject.SetActive(true);
                    var netIncomeNeeded = shopInventory.Tiers[currentTier + 1].netIncome * playerCount;
                    upgradeText.text = $"Earn ${netIncomeNeeded - WalletManager.Main.GlobalWalletValue} more\nto unlock next Tier";
                }
                else
                {
                    // Max Tier
                    tierText.text = "Max Tier";
                    upgradeButton.SetActive(false);
                    upgradeText.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // Some shop doesn't have upgrade tiers, disable upgrade panel
            upgradePanel.SetActive(false);
        }
    }

    public void CloseShop()
    {
        if (IsAnimating) return;
        if (!IsShowing) return;

        // Unsubscribe from wallet changes
        WalletManager.Main.OnLocalWalletChanged -= HandleLocalWalletChanged;

        // Clear references
        playerInventory = null;
        shopInventory = null;
        shopTransform = null;

        // Switch UI input map back to gameplay
        InputManager.Main.SwitchMap(InputMap.Gameplay);

        // Stop eye candy coroutines if they are running
        StopEyeCandyCoroutines();

        AudioManager.Main.PlayClickSound();
        StartCoroutine(CloseShopCoroutine());
    }

    public void HandleShopButtonClicked(ItemProperty itemProperty, ShopButton button, int index)
    {
        var playerCount = (uint)NetworkManager.Singleton.ConnectedClients.Count;

        itemPanel.SetActive(true);
        displayPanel.SetActive(false);

        // Update UI
        itemImage.sprite = itemProperty.IconSprite;
        itemNameText.text = itemProperty.Name;
        itemDescriptionText.text = itemProperty.Description;
        ownedText.text = $"Owned: {playerInventory.GetItemCount(itemProperty)}";

        // Set the selected item property
        currentItemProperty = itemProperty;
        currentShopButton = button;
        currentItemIndex = index;

        // Set default multiplier to 5
        currentMultiplier = 5;

        // Play Audio
        AudioManager.Main.PlayClickSound();

        if (shopMode == ShopMode.Buy)
        {
            stackActionText.text = $"Buy A Stack\n{itemProperty.MaxStack} for ${itemProperty.MaxStack * itemProperty.Price * playerCount}";
            multiplierActionText.text = $"Buy 5 for ${5 * itemProperty.Price * playerCount}";
            visitedItemProperties.Add(itemProperty);
            button.UpdateIsNew(false);
        }
        else
        {
            var itemCount = playerInventory.Inventory[index].Count;
            stackActionText.text = $"Sell All\n{itemCount} for ${itemCount * itemProperty.Price * playerCount}";
            multiplierActionText.text = $"Sell {currentMultiplier} for ${currentMultiplier * itemProperty.Price * playerCount}";
        }
    }

    public void HandleQuickActionClicked(ItemProperty itemProperty, ShopButton button, int index)
    {
        HandleShopButtonClicked(itemProperty, button, index);
        if (shopMode == ShopMode.Buy)
        {
            BuyItem(itemProperty, 1, button);
        }
        else
        {
            SellItem(itemProperty, 1, button, index);
        }
    }

    private void BuyItem(ItemProperty itemProperty, int count, ShopButton button)
    {
        var playerCount = (uint)NetworkManager.Singleton.ConnectedClients.Count;
        var price = itemProperty.Price * playerCount * (uint)count;

        if (WalletManager.Main.LocalWalletValue < price)
        {
            // Not enough coins to buy the item
            if (showDebug) Debug.Log($"Cannot afford x{count} {itemProperty.Name}");
            AudioManager.Main.PlayOneShot(errorSound);
        }
        else
        {
            // Charge the player
            playerInventory.ConsumeCoinsOnClient(price);

            // Add the item to the player's inventory, if not successful, spawn item on the ground
            for (int i = 0; i < count; i++)
            {
                if (!playerInventory.AddItem(itemProperty, false))
                {
                    AssetManager.Main.SpawnItem(itemProperty, shopTransform.transform.position - new Vector3(0, 1));
                }
            }

            // VFX
            if (dropItemCoroutine != null) StopCoroutine(dropItemCoroutine);
            dropItemCoroutine = StartCoroutine(DropItemCoroutine(button.transform.position, itemProperty.IconSprite, count));

            // Update UI
            ownedText.text = $"Owned: {playerInventory.GetItemCount(itemProperty)}";
            visitedItemProperties.Add(itemProperty);
            button.UpdateIsNew(false);

            if (showDebug) Debug.Log($"Bought 1x{itemProperty.Name} for {price}");
        }
    }

    private void SellItem(ItemProperty itemProperty, int count, ShopButton button, int index)
    {
        if (playerInventory.Inventory[index].Count == 0 || playerInventory.Inventory[index].Count < count)
        {
            // Not enough items to sell
            if (showDebug) Debug.Log($"Cannot sell x{count} {itemProperty.Name}");
            AudioManager.Main.PlayOneShot(errorSound);
            return;
        }

        var playerCount = (uint)NetworkManager.Singleton.ConnectedClients.Count;
        var price = itemProperty.Price * playerCount;

        for (int i = 0; i < count; i++)
        {
            playerInventory.ConsumeItemOnClient(index);
            var value = (uint)Mathf.CeilToInt(price * shopInventory.PenaltyMultiplier);
            playerInventory.AddCoinsOnClient(value);
        }

        // Update UI
        button.UpdateEntry(playerInventory.Inventory[index].Count);
        ownedText.text = $"Owned: {playerInventory.GetItemCount(itemProperty)}";

        // VFX
        if (dropItemCoroutine != null) StopCoroutine(dropItemCoroutine);
        dropItemCoroutine = StartCoroutine(DropItemCoroutine(button.transform.position, itemProperty.IconSprite, count));

        if (playerInventory.Inventory[index].IsEmpty)
        {
            button.Remove();

            if (showDebug) Debug.Log($"No more {itemProperty.Name} to sell");

            // If no more items of this type, clear the selected item
            ClearItemReference();
            itemPanel.SetActive(false);
            displayPanel.SetActive(true);
        }
        else
        {
            var itemCount = (int)playerInventory.Inventory[index].Count;
            stackActionText.text = $"Sell All\n{itemCount} for ${itemCount * itemProperty.Price * playerCount}";
            ClampMultiplier();
            multiplierActionText.text = $"Sell {currentMultiplier} for ${currentMultiplier * itemProperty.Price * playerCount}";
        }
    }

    public void OnStackActionClicked()
    {
        if (currentItemProperty == null)
        {
            if (showDebug) Debug.Log("No item selected for buying a stack.");
            return;
        }

        if (shopMode == ShopMode.Buy)
        {
            BuyItem(currentItemProperty, (int)currentItemProperty.MaxStack, currentShopButton);
        }
        else
        {
            SellItem(currentItemProperty, (int)playerInventory.Inventory[currentItemIndex].Count, currentShopButton, currentItemIndex);
        }
    }

    public void OnMultiplierActionClicked()
    {
        if (currentItemProperty == null)
        {
            if (showDebug) Debug.Log("No item selected");
            return;
        }

        if (shopMode == ShopMode.Buy)
        {
            BuyItem(currentItemProperty, currentMultiplier, currentShopButton);
        }
        else
        {
            SellItem(currentItemProperty, currentMultiplier, currentShopButton, currentItemIndex);
        }
    }

    #region Utility

    private void ClearItemReference()
    {
        currentItemProperty = null;
        currentShopButton = null;
        currentItemIndex = -1;
    }

    private IEnumerator DropItemCoroutine(Vector2 position, Sprite sprite, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (shopMode == ShopMode.Buy)
                AudioManager.Main.PlaySoundIncreasePitch(buySound);
            else
                AudioManager.Main.PlaySoundIncreasePitch(sellSound);

            var itemDrop = Instantiate(itemDropPrefab, position, Quaternion.identity, contentContainer);
            itemDrop.GetComponent<Image>().sprite = sprite;
            yield return new WaitForSeconds(0.1f); // Slight delay between drops
        }

        AudioManager.Main.ResetPitch();
    }

    private Coroutine popCoroutine;
    private void HandleLocalWalletChanged(ulong value)
    {
        coinText.text = "$" + value;

        if (recentlyOpened)
        {
            recentlyOpened = false;
        }
        else
        {
            if (popCoroutine != null) StopCoroutine(popCoroutine);
            popCoroutine = StartCoroutine(coinUI.UIPopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.1f));
        }
    }

    private void HandleGlobalWalletChanged(ulong newValue)
    {
        UpdateUpgradePanel();
    }

    private IEnumerator CloseShopCoroutine()
    {
        ClearChild();
        yield return null;
        yield return HideCoroutine();
    }

    private void ClearChild()
    {
        foreach (Transform child in contentContainer)
        {
            if (child.TryGetComponent<ShopButton>(out var button))
                button.Remove();
            else
                Destroy(child.gameObject);
        }
    }

    public void Add100()
    {
        playerInventory.AddCoinsOnClient(100);
    }

    public void Add1000()
    {
        playerInventory.AddCoinsOnClient(1000);
    }

    public void Add10000()
    {
        playerInventory.AddCoinsOnClient(10000);
    }

    private void StopEyeCandyCoroutines()
    {
        // Stop eye candy coroutines if they are running
        if (dropItemCoroutine != null) StopCoroutine(dropItemCoroutine);
        if (shopButtonBuyCoroutine != null) StopCoroutine(shopButtonBuyCoroutine);

        AudioManager.Main.ResetPitch();
    }

    private IEnumerator WaitShopLoad()
    {
        yield return new WaitUntil(() => !IsAnimating);
        yield return upgradeButton.transform.UIPopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.1f);
    }

    #endregion Utility

    #region Multiplier
    public void OnNeg5Clicked()
    {
        UpdateMultiplier(-5);
    }

    public void OnNeg1Clicked()
    {
        UpdateMultiplier(-1);
    }

    public void OnPos1Clicked()
    {
        UpdateMultiplier(1);
    }

    public void OnPos5Clicked()
    {
        UpdateMultiplier(5);
    }

    private void UpdateMultiplier(int count)
    {
        if (currentItemProperty == null) return;

        currentMultiplier += count;
        ClampMultiplier();

        var playerCount = (uint)NetworkManager.Singleton.ConnectedClients.Count;
        multiplierActionText.text = $"{(shopMode == ShopMode.Buy ? "Buy" : "Sell")} {currentMultiplier} for ${currentMultiplier * currentItemProperty.Price * playerCount}";
    }

    private void ClampMultiplier()
    {
        // Ensure the multiplier does not go below 1       
        if (currentMultiplier < 1)
        {
            currentMultiplier = 1;
        }

        // Ensure the multiplier does not exceed the item count in the player's inventory
        if (currentItemIndex <= 0)
        {
            if (currentItemProperty == null) return;
            var itemCount = (int)currentItemProperty.MaxStack;
            if (currentMultiplier > itemCount)
            {
                currentMultiplier = itemCount;
            }
        }
        else
        {
            var itemCount = (int)playerInventory.Inventory[currentItemIndex].Count;
            if (currentMultiplier > itemCount)
            {
                currentMultiplier = itemCount;
            }
        }
    }
    #endregion
}
