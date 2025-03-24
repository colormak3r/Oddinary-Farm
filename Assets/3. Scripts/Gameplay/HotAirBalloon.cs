using System;
using Unity.Netcode;
using UnityEngine;

public class HotAirBalloon : Structure, IInteractable
{
    [Header("Hot Air Balloon Settings")]
    [SerializeField]
    private UpgradeStages upgradeStages;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private SpriteRenderer headRenderer;
    [SerializeField]
    private SpriteRenderer faceRenderer;
    [SerializeField]
    private SpriteRenderer hatRenderer;

    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    private NetworkVariable<NetworkObjectReference> CurrentOwner = new NetworkVariable<NetworkObjectReference>();

    public int CurrentStageValue => CurrentStage.Value;

    public override void OnNetworkSpawn()
    {
        CurrentStage.OnValueChanged += HandleCurrentStageChanged;
        CurrentOwner.OnValueChanged += HandleCurrentOwnerChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentStage.OnValueChanged -= HandleCurrentStageChanged;
        CurrentOwner.OnValueChanged -= HandleCurrentOwnerChanged;
    }

    private void HandleCurrentStageChanged(int previousValue, int newValue)
    {
        spriteRenderer.sprite = upgradeStages.GetStage(newValue).sprite;
    }

    private void HandleCurrentOwnerChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {
        if (newValue.TryGet(out var newNetworkObject))
        {
            var appearance = newNetworkObject.GetComponent<PlayerAppearance>();
            headRenderer.sprite = appearance.CurrentHeadSprite;
            faceRenderer.sprite = appearance.CurrentFaceSprite;
            hatRenderer.sprite = appearance.CurrentHatSprite;
        }
        else
        {
            if (previousValue.TryGet(out var oldNetworkObject))
            {

            }

            headRenderer.sprite = null;
            faceRenderer.sprite = null;
            hatRenderer.sprite = null;
        }
    }

    public void Interact(Transform source)
    {
        if (CurrentStage.Value < upgradeStages.GetStageCount() - 1)
            UpgradeUI.Main.Initialize(source.GetComponent<PlayerInventory>(), upgradeStages, CurrentStageValue, UpgradeBalloon);
        else
        {
            if (CurrentOwner.Value.TryGet(out var networkObject))
            {
                if (networkObject == source.GetComponent<NetworkObject>())
                {
                    SetOwnerRpc(default);
                }
            }
            else
            {
                SetOwnerRpc(source.gameObject);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SetOwnerRpc(NetworkObjectReference networkObjectReference)
    {
        CurrentOwner.Value = networkObjectReference;
    }

    public void UpgradeBalloon()
    {
        UpgradeBalloonRpc();
    }

    [Rpc(SendTo.Server)]
    private void UpgradeBalloonRpc()
    {
        if (CurrentStage.Value < upgradeStages.GetStageCount() - 1)
            CurrentStage.Value++;
    }
}
