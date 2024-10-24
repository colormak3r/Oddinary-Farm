using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VendingMachine : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField]
    private ShopInventory shopInventory;

    private ItemSpawner itemSpawner;

    private void Awake()
    {
        itemSpawner = GetComponent<ItemSpawner>();
    }

    public void Interact(Transform source)
    {
        if (ShopUI.Main.IsShowing)
            ShopUI.Main.CloseShop();
        else
            ShopUI.Main.OpenShop(source.GetComponent<PlayerInventory>(), shopInventory, itemSpawner);
    }
}
