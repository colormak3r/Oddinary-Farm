using System;
using Unity.Netcode;
using UnityEngine;

public class ControllableController : NetworkBehaviour
{
    private IControllable[] controllables;      // List of objects that are able to be controlled

    // UI toggles
    private bool isShopUIVisible;
    private bool isOptionsUIVisible;
    private bool isAudioUIVisible;
    private bool isAppearanceUIVisible;
    private bool isGameplayUIVisible;
    private bool isUpgradeUIVisible;

    private void Awake()
    {
        // Find every child object that is a controllable object
        controllables = GetComponentsInChildren<IControllable>();
    }

    public override void OnNetworkSpawn()
    {
        // NOTE: Consider using early return for better readability
        // if (!IsOwner)
        //    return;
        
        if (IsOwner)
        {
            // Subscribe to events on spawn
            ShopUI.Main.OnVisibilityChanged.AddListener(OnShopUIVisibilityChanged);
            OptionsUI.Main.OnVisibilityChanged.AddListener(OnOptionsUIVisibilityChanged);
            AudioUI.Main.OnVisibilityChanged.AddListener(OnAudioUIVisibilityChanged);
            AppearanceUI.Main.OnVisibilityChanged.AddListener(OnAppearanceUIVisibilityChanged);
            UpgradeUI.Main.OnVisibilityChanged.AddListener(OnUpgradeUIVisibilityChanged);
        }
    }


    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            // Unsubscribe to events on despawn
            ShopUI.Main.OnVisibilityChanged.RemoveListener(OnShopUIVisibilityChanged);
            OptionsUI.Main.OnVisibilityChanged.RemoveListener(OnOptionsUIVisibilityChanged);
            AudioUI.Main.OnVisibilityChanged.RemoveListener(OnAudioUIVisibilityChanged);
            AppearanceUI.Main.OnVisibilityChanged.RemoveListener(OnAppearanceUIVisibilityChanged);
            UpgradeUI.Main.OnVisibilityChanged.RemoveListener(OnUpgradeUIVisibilityChanged);
        }
    }

    // Set UI Visiblility
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

    private void OnAppearanceUIVisibilityChanged(bool isShowing)
    {
        isAppearanceUIVisible = isShowing;
        Evaluate();
    }

    private void OnUpgradeUIVisibilityChanged(bool isShowing)
    {
        isUpgradeUIVisible = isShowing;
        Evaluate();
    }

    private void Evaluate()
    {
        // Only allow control if ALL listed UI panels are not visable
        bool controllable = !isShopUIVisible && !isOptionsUIVisible
            && !isAudioUIVisible && !isAppearanceUIVisible && !isUpgradeUIVisible;

        foreach (var controllableItem in controllables)
        {
            controllableItem.SetControllable(controllable);
        }
    }

    // Toggle Control of all UI elements at once
    public void SetControl(bool value)
    {
        foreach (var controllable in controllables)
        {
            controllable.SetControllable(value);
        }
    }
}
