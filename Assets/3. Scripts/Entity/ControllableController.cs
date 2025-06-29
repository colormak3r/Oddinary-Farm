using System;
using Unity.Netcode;
using UnityEngine;

public class ControllableController : NetworkBehaviour
{
    private IControllable[] controllables;

    private bool isShopUIVisible;
    private bool isOptionsUIVisible;
    private bool isAudioUIVisible;
    private bool isAppearanceUIVisible;
    private bool isGameplayUIVisible;
    private bool isUpgradeUIVisible;
    private bool isTutorialUIVisible;

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
            //AppearanceUI.Main.OnVisibilityChanged.AddListener(OnAppearanceUIVisibilityChanged);
            UpgradeUI.Main.OnVisibilityChanged.AddListener(OnUpgradeUIVisibilityChanged);
            TutorialUI.Main.OnVisibilityChanged.AddListener(OnTutorialUIVisibilityChanged);
        }
    }


    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            ShopUI.Main.OnVisibilityChanged.RemoveListener(OnShopUIVisibilityChanged);
            OptionsUI.Main.OnVisibilityChanged.RemoveListener(OnOptionsUIVisibilityChanged);
            AudioUI.Main.OnVisibilityChanged.RemoveListener(OnAudioUIVisibilityChanged);
            //AppearanceUI.Main.OnVisibilityChanged.RemoveListener(OnAppearanceUIVisibilityChanged);
            UpgradeUI.Main.OnVisibilityChanged.RemoveListener(OnUpgradeUIVisibilityChanged);
            TutorialUI.Main.OnVisibilityChanged.RemoveListener(OnTutorialUIVisibilityChanged);
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

    private void OnTutorialUIVisibilityChanged(bool isShowing)
    {
        isTutorialUIVisible = isShowing;
        Evaluate();
    }

    private void Evaluate()
    {
        bool controllable = !isShopUIVisible && !isOptionsUIVisible
            && !isAudioUIVisible && !isAppearanceUIVisible && !isUpgradeUIVisible
            && !isTutorialUIVisible;

        foreach (var controllableItem in controllables)
        {
            controllableItem.SetControllable(controllable);
        }
    }

    public void SetControl(bool value)
    {
        foreach (var controllable in controllables)
        {
            controllable.SetControllable(value);
        }
    }
}
