using System;
using Unity.Netcode;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class HotAirBalloon : Structure, IInteractable
{
    [Header("Hot Air Balloon Settings")]
    [SerializeField]
    private UpgradeStages upgradeStages;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Vector3 playerOffset;

    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    private NetworkVariable<NetworkObjectReference> CurrentOwner = new NetworkVariable<NetworkObjectReference>();

    public int CurrentStageValue => CurrentStage.Value;
    private Vector2 playerPosition;

    public override void OnNetworkSpawn()
    {
        CurrentStage.OnValueChanged += HandleCurrentStageChanged;
        CurrentOwner.OnValueChanged += HandleCurrentOwnerChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentStage.OnValueChanged -= HandleCurrentStageChanged;
        CurrentOwner.OnValueChanged -= HandleCurrentOwnerChanged;

        if (IsServer)
        {
            HandleOnPlayerDieOnServer();
        }
    }

    private void HandleCurrentStageChanged(int previousValue, int newValue)
    {
        spriteRenderer.sprite = upgradeStages.GetStage(newValue).sprite;
    }

    private void HandleCurrentOwnerChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {

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

                    source.transform.position = playerPosition;
                    source.GetComponent<HotAirBalloonController>().SetControl(false);
                }
            }
            else
            {
                SetOwnerRpc(source.gameObject);
                playerPosition = source.position;

                source.GetComponent<HotAirBalloonController>().SetControl(true);
                source.transform.position = transform.position + playerOffset;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SetOwnerRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out var networkObject))
        {
            var playerStatus = networkObject.GetComponent<PlayerStatus>();
            playerStatus.OnDeathOnServer.AddListener(HandleOnPlayerDieOnServer);
            CurrentOwner.Value = networkObjectReference;
        }
        else
        {
            HandleOnPlayerDieOnServer();
        }
    }

    private void HandleOnPlayerDieOnServer()
    {
        PlayerStatus playerStatus = null;
        if (CurrentOwner.Value.TryGet(out var networkObject))
        {
            var playerGO = networkObject.gameObject;
            playerGO.GetComponent<HotAirBalloonController>().SetControl(false);
            playerStatus = networkObject.GetComponent<PlayerStatus>();
        }

        if (playerStatus)
            playerStatus.OnDeathOnServer.AddListener(HandleOnPlayerDieOnServer);


        CurrentOwner.Value = default;
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
