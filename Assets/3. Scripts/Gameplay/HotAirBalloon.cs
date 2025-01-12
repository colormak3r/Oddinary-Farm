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

    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    public int CurrentStageValue => CurrentStage.Value;

    public override void OnNetworkSpawn()
    {
        CurrentStage.OnValueChanged += HandleCurrentStageChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentStage.OnValueChanged -= HandleCurrentStageChanged;
    }

    private void HandleCurrentStageChanged(int previousValue, int newValue)
    {
        spriteRenderer.sprite = upgradeStages.GetStage(newValue).sprite;
    }


    public void Interact(Transform source)
    {
        if (CurrentStage.Value < upgradeStages.GetStageCount() - 1)
            UpgradeUI.Main.Initialize(source.GetComponent<PlayerInventory>(), upgradeStages, CurrentStageValue, UpgradeBalloon);
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
