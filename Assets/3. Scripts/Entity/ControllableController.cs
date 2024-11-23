using System;
using Unity.Netcode;
using UnityEngine;

public class ControllableController : NetworkBehaviour
{
    private IControllable[] controllables;

    private bool isShopVisible;

    private void Awake()
    {
        controllables = GetComponentsInChildren<IControllable>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            ShopUI.Main.OnVisibilityChanged.AddListener(OnShopVisibilityChanged);
        }
    }


    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            ShopUI.Main.OnVisibilityChanged.RemoveListener(OnShopVisibilityChanged);
        }
    }

    private void OnShopVisibilityChanged(bool isShowing)
    {
        isShopVisible = isShowing;
        Evaluate();
    }

    private void Evaluate()
    {
        bool controllable = !isShopVisible;

        foreach (var controllableItem in controllables)
        {
            controllableItem.SetControllable(controllable);
        }
    }

}
