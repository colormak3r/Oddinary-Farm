using System;
using Unity.Netcode;
using UnityEngine;

public class ControllableController : NetworkBehaviour
{
    private IControllable[] controllables;

    private bool isShopUIVisible;
    private bool isOptionsUIVisible;
    private bool isAudioUIVisible;

    private void Awake()
    {
        controllables = GetComponentsInChildren<IControllable>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            ShopUI.Main.OnVisibilityChanged.AddListener(OnShopUIVisibilityChanged);
            OptionsUI.Main.OnVisibilityChanged.AddListener(OnOptionsUIVisibilityChanged);
            AudioUI.Main.OnVisibilityChanged.AddListener(OnAudioUIVisibilityChanged);
        }
    }


    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            ShopUI.Main.OnVisibilityChanged.RemoveListener(OnShopUIVisibilityChanged);
            OptionsUI.Main.OnVisibilityChanged.RemoveListener(OnOptionsUIVisibilityChanged);
            AudioUI.Main.OnVisibilityChanged.RemoveListener(OnAudioUIVisibilityChanged);
        }
    }

    private void OnShopUIVisibilityChanged(bool isShowing)
    {
        isShopUIVisible = isShowing;
        Evaluate();
    }

    private void OnOptionsUIVisibilityChanged(bool isShowing)
    {
        isOptionsUIVisible = isShowing;
        Evaluate();
    }

    private void OnAudioUIVisibilityChanged(bool isShowing)
    {
        isAudioUIVisible = isShowing;
        Evaluate();
    }

    private void Evaluate()
    {
        bool controllable = !isShopUIVisible  && !isOptionsUIVisible && !isAudioUIVisible;

        foreach (var controllableItem in controllables)
        {
            controllableItem.SetControllable(controllable);
        }
    }

}
