using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    public static ShopManager Main { get; private set; }

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

    [Header("Shop Settings")]
    [SerializeField]
    private ShopInventory[] shopInventoryArray;

    [Header("Shop Debugs")]
    [SerializeField]
    private bool showDebugs;

    private Dictionary<ShopInventory, int> globalShopInventoryCurrentTier = new Dictionary<ShopInventory, int>();
    private Dictionary<ShopInventory, int> localShopInventoryCurrentTier = new Dictionary<ShopInventory, int>();
    private Dictionary<ShopInventory, bool> shopInventoryRecentUpgraded = new Dictionary<ShopInventory, bool>();

    public Action<ShopInventory> OnNewUpgradeAvailable;
    public Action<ShopInventory> OnShopUpgraded;

    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();
        WalletManager.Main.OnGlobalWalletChanged += HandleGlobalWalletChanged;
        HandleGlobalWalletChanged(WalletManager.Main.GlobalWalletValue);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        WalletManager.Main.OnGlobalWalletChanged -= HandleGlobalWalletChanged;
    }

    private void HandleGlobalWalletChanged(ulong obj)
    {
        var playerCount = (uint)NetworkManager.Singleton.ConnectedClients.Count;

        foreach (var shopInventory in shopInventoryArray)
        {
            var shopTierLength = shopInventory.Tiers.Length;
            if (shopTierLength > 1)
            {
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

                globalShopInventoryCurrentTier[shopInventory] = globalTier;

                // Check if the tier has changed
                var localTier = GetCurrentLocalTier(shopInventory);
                if (globalTier > localTier)
                {
                    // Notify that a new upgrade is available
                    OnNewUpgradeAvailable?.Invoke(shopInventory);
                    if (showDebugs) Debug.Log($"ShopManager: New upgrade available for {shopInventory.name} to tier {globalTier}.");
                }
            }
        }
    }

    public int GetCurrentGlobalTier(ShopInventory shopInventory)
    {
        if (globalShopInventoryCurrentTier.TryGetValue(shopInventory, out int currentTier))
        {
            return currentTier;
        }
        else
        {
            // Code should no reach here
            if (showDebugs) Debug.LogWarning($"ShopManager: No current tier found for {shopInventory.name}. Returning 0.");
            return 0; // Default to 0 if not found
        }
    }

    public int GetCurrentLocalTier(ShopInventory shopInventory)
    {
        if (localShopInventoryCurrentTier.TryGetValue(shopInventory, out int currentTier))
        {
            return currentTier;
        }
        else
        {
            localShopInventoryCurrentTier[shopInventory] = 0; // Initialize to 0 if not found
            return 0; // Default to 0 if not found
        }
    }

    public void IncreaseLocalTier(ShopInventory shopInventory)
    {
        if (localShopInventoryCurrentTier.ContainsKey(shopInventory))
        {
            if (localShopInventoryCurrentTier[shopInventory] < shopInventory.Tiers.Length - 1)
            {
                localShopInventoryCurrentTier[shopInventory]++;
                shopInventoryRecentUpgraded[shopInventory] = true;
                OnShopUpgraded?.Invoke(shopInventory);
                if (showDebugs) Debug.Log($"ShopManager: Local tier for {shopInventory.name} increased to {localShopInventoryCurrentTier[shopInventory]}.");
            }
            else
            {
                if (showDebugs) Debug.LogWarning($"ShopManager: Cannot increase tier for {shopInventory.name}. Already at max tier.");
            }
        }
        else
        {
            localShopInventoryCurrentTier[shopInventory] = 1;
            shopInventoryRecentUpgraded[shopInventory] = true;
            OnShopUpgraded?.Invoke(shopInventory);
            if (showDebugs) Debug.Log($"ShopManager: Local tier for {shopInventory.name} initialized to 1.");
        }
    }

    public bool IsShopRecentlyUpgraded(ShopInventory shopInventory)
    {
        if (shopInventoryRecentUpgraded.TryGetValue(shopInventory, out bool recentlyUpgraded))
        {
            // Reset the flag after checking
            shopInventoryRecentUpgraded[shopInventory] = false;
            return recentlyUpgraded;
        }
        else
        {
            // If not found, assume it hasn't been upgraded recently
            return false;
        }
    }


    public bool IsUpgradeAvailable(ShopInventory shopInventory)
    {
        return GetCurrentGlobalTier(shopInventory) > GetCurrentLocalTier(shopInventory);
    }
}
