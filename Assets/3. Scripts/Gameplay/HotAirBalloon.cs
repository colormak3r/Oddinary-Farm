using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class HotAirBalloon : Structure, IInteractable
{
    [Header("Hot Air Balloon Settings")]
    // Specify when the hot air baloon can fly
    [SerializeField]
    private int takeOffDate = 8;
    [SerializeField]
    private int takeOffHour = 0;

    [SerializeField]
    private UpgradeStages upgradeStages;        // The amount of upgrades the hot air baloon may have
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Vector3 playerOffset;       // Where the player will stand relative to the hot air baloon

    // Networked variables
    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    private NetworkVariable<bool> IsTakenOff = new NetworkVariable<bool>();
    private NetworkVariable<NetworkObjectReference> CurrentOwner = new NetworkVariable<NetworkObjectReference>();

    private FixedJoint2D fixedJoint;

    public int CurrentStageValue => CurrentStage.Value;
    private Vector2 playerPosition;

    public override void OnNetworkSpawn()
    {
        fixedJoint = GetComponent<FixedJoint2D>();
        
        // Subscribe event and init. start state
        CurrentStage.OnValueChanged += HandleCurrentStageChanged;
        HandleCurrentStageChanged(0, CurrentStage.Value);       // int

        // Subscribe event and init. starting owner to the person who built structure
        CurrentOwner.OnValueChanged += HandleCurrentOwnerChanged;
        HandleCurrentOwnerChanged(default, CurrentOwner.Value);     // Network Object

        IsTakenOff.OnValueChanged += HandleIsTakenOff;
        HandleIsTakenOff(false, IsTakenOff.Value);      // bool

        if (IsOwner)
        {
            TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
        }
        HandleOnHourChanged(TimeManager.Main.CurrentHour);
    }
    
    // Unsubscribe from events
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
        if (!newValue) 
            return;

        // Lock baloon rotation
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

        // Change sorting order to front
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
        if (IsTakenOff.Value) 
            return;

        // QUESTION: Attempt auto take off? or trigger "can be taken off"?
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

    // Update stage sprite
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

            // Just placed, is it take off time?
            if (TimeManager.Main.CurrentDate > takeOffDate || (TimeManager.Main.CurrentDate == takeOffDate && TimeManager.Main.CurrentHour >= takeOffHour))
            {
                IsTakenOff.Value = true;
            }
        }

        // Assign the fixed joint to a client
        if (newValue.TryGet(out var localnetworkObject))
        {
            var playerStatus = localnetworkObject.GetComponent<PlayerStatus>();             // QUESTION: Is this ever used?
            fixedJoint.connectedBody = localnetworkObject.GetComponent<Rigidbody2D>();
        }
        else
        {
            fixedJoint.connectedBody = null;
        }
    }

    public void Interact(Transform source)
    {
        if (IsTakenOff.Value) 
            return;

        if (CurrentStage.Value < upgradeStages.GetStageCount() - 1)     // If the current stage is not the final stage
            UpgradeUI.Main.Initialize(source.GetComponent<PlayerInventory>(), upgradeStages, CurrentStageValue, UpgradeBalloon);
        else
        {
            if (CurrentOwner.Value.TryGet(out var networkObject))
            {
                if (networkObject == source.GetComponent<NetworkObject>())      // QUESTION: If the object is a newtworked object?
                {
                    SetOwnerRpc(default);

                    source.transform.position = playerPosition;
                    source.GetComponent<HotAirBalloonController>().SetControl(false);       // Player still has control
                    source.transform.SetParent(null);       // QUESTION: Unparent player? Why?
                }
            }
            else
            {
                SetOwnerRpc(source.gameObject);     // Set the woner to the gameobject
                playerPosition = source.position;

                source.GetComponent<HotAirBalloonController>().SetControl(true);        // Hot air balloon has control
                source.transform.position = transform.position + playerOffset;      // Place player in hot air baloon
                source.transform.SetParent(transform);                          // Parent player to hot air baloon object
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

    // Increase balloon stage
    [Rpc(SendTo.Server)]
    private void UpgradeBalloonRpc()
    {
        if (CurrentStage.Value < upgradeStages.GetStageCount() - 1)
            CurrentStage.Value++;
    }

    [ContextMenu("Take Off")]
    private void TakeOff()
    {
        if (!IsServer) 
            return;

        IsTakenOff.Value = true;
    }
}
