/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/12/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using UnityEngine;

public class VendingMachine : Structure, IInteractable
{
    [Header("Vending Machine Settings")]
    [SerializeField]
    private ShopInventory shopInventory;
    [SerializeField]
    private Color signColor;
    [SerializeField]
    private GameObject newTierAnimation;

    public bool IsHoldInteractable => false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ShopManager.Main.OnNewUpgradeAvailable += HandleNewUpgradeAvailable;
        ShopManager.Main.OnShopUpgraded += HandleShopUpgraded;
        newTierAnimation.GetComponent<SpriteRenderer>().color = signColor;
        newTierAnimation.SetActive(ShopManager.Main.IsUpgradeAvailable(shopInventory));
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        ShopManager.Main.OnNewUpgradeAvailable -= HandleNewUpgradeAvailable;
        ShopManager.Main.OnShopUpgraded -= HandleShopUpgraded;
    }

    private void HandleShopUpgraded(ShopInventory inventory)
    {
        if (inventory != shopInventory) return;
        newTierAnimation.SetActive(ShopManager.Main.IsUpgradeAvailable(shopInventory));
    }

    private void HandleNewUpgradeAvailable(ShopInventory inventory)
    {
        if (inventory != shopInventory) return;
        newTierAnimation.SetActive(true);
    }

    public void Interact(Transform source)
    {
        if (ShopUI.Main.IsShowing)
            ShopUI.Main.CloseShop();
        else
            ShopUI.Main.OpenShop(source.GetComponent<PlayerInventory>(), shopInventory, transform);
    }

    public void InteractionEnd(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void InteractionStart(Transform source)
    {
        throw new System.NotImplementedException();
    }
}
