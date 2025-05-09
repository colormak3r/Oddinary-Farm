using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class HotAirBalloon : Structure, IInteractable
{
    [Header("Hot Air Balloon Settings")]
    [SerializeField]
    private int takeOffDate = 8;
    [SerializeField]
    private int takeOffHour = 0;
    [SerializeField]
    private UpgradeStages upgradeStages;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Vector3 playerOffset;

    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    private NetworkVariable<bool> IsTakenOff = new NetworkVariable<bool>();
    private NetworkVariable<NetworkObjectReference> CurrentOwner = new NetworkVariable<NetworkObjectReference>();

    private FixedJoint2D fixedJoint;

    public int CurrentStageValue => CurrentStage.Value;
    private Vector2 playerPosition;

    public override void OnNetworkSpawn()
    {
        fixedJoint = GetComponent<FixedJoint2D>();

        CurrentStage.OnValueChanged += HandleCurrentStageChanged;
        HandleCurrentStageChanged(0, CurrentStage.Value);
        CurrentOwner.OnValueChanged += HandleCurrentOwnerChanged;
        HandleCurrentOwnerChanged(default, CurrentOwner.Value);
        IsTakenOff.OnValueChanged += HandleIsTakenOff;
        HandleIsTakenOff(false, IsTakenOff.Value);

        if (IsOwner)
        {
            TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
        }
        HandleOnHourChanged(TimeManager.Main.CurrentHour);
    }

    public override void OnNetworkDespawn()
    {
        CurrentStage.OnValueChanged -= HandleCurrentStageChanged;
        CurrentOwner.OnValueChanged -= HandleCurrentOwnerChanged;
        IsTakenOff.OnValueChanged -= HandleIsTakenOff;

        if (IsServer)
        {
            HandleOnPlayerDieOnServer();
        }

        if (IsOwner)
        {
            TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
        }
    }


    private void HandleIsTakenOff(bool previousValue, bool newValue)
    {
        if (!newValue) return;

        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

        // Change sorting order
        var sortingGroups = GetComponentsInChildren<SortingGroup>();
        foreach (var sortingGroup in sortingGroups)
        {
            sortingGroup.sortingLayerName = "UI";
            sortingGroup.sortingOrder = 99;
        }

        // Disable the collider of the hot air balloon
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Move the hot air balloon
        if (IsOwner)
            GetComponent<EntityMovement>().SetDirection(Vector2.up);
    }


    private void HandleOnHourChanged(int currentHour)
    {
        if (IsTakenOff.Value) return;

        if (currentHour == takeOffHour)
        {
            if (TimeManager.Main.CurrentDate == takeOffDate)
            {
                if (CurrentOwner.Value.TryGet(out var networkObject))
                {
                    if (IsServer)
                    {
                        IsTakenOff.Value = true;
                    }
                }
            }
        }
    }

    private void HandleCurrentStageChanged(int previousValue, int newValue)
    {
        spriteRenderer.sprite = upgradeStages.GetStage(newValue).sprite;
    }

    private void HandleCurrentOwnerChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {
        if (IsServer)
        {
            if (newValue.TryGet(out var networkObject))
            {
                NetworkObject.ChangeOwnership(networkObject.OwnerClientId);
            }
            else
            {
                NetworkObject.ChangeOwnership(default);
            }

            if (TimeManager.Main.CurrentDate > takeOffDate || (TimeManager.Main.CurrentDate == takeOffDate && TimeManager.Main.CurrentHour >= takeOffHour))
            {
                IsTakenOff.Value = true;
            }
        }

        if (newValue.TryGet(out var localnetworkObject))
        {
            var playerStatus = localnetworkObject.GetComponent<PlayerStatus>();
            fixedJoint.connectedBody = localnetworkObject.GetComponent<Rigidbody2D>();
        }
        else
        {
            fixedJoint.connectedBody = null;
        }
    }

    public void Interact(Transform source)
    {
        if (IsTakenOff.Value) return;

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
                    source.transform.SetParent(null);
                }
            }
            else
            {
                SetOwnerRpc(source.gameObject);
                playerPosition = source.position;

                source.GetComponent<HotAirBalloonController>().SetControl(true);
                source.transform.position = transform.position + playerOffset;
                source.transform.SetParent(transform);
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

    [ContextMenu("Take Off")]
    private void TakeOff()
    {
        if (!IsServer) return;
        IsTakenOff.Value = true;
    }
}
