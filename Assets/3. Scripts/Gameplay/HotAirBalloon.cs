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
    private Collider2D physicCollider;

    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    private NetworkVariable<bool> CanTakeOff = new NetworkVariable<bool>();

    public int CurrentStageValue => CurrentStage.Value;

    private MountInteraction mountInteraction;
    private MountController mountController;

    private bool isMounted;

    public override void OnNetworkSpawn()
    {
        mountInteraction = GetComponent<MountInteraction>();
        mountController = GetComponent<MountController>();

        if (mountInteraction == null || mountController == null)
        {
            Debug.LogError("HotAirBalloon Error: Missing MountInteraction or MountController scripts.");
            return;
        }

        // Set mount values to false on init
        mountInteraction.SetCanMount(false);
        mountInteraction.OnMount += TakeOff;
        isMounted = false;

        CurrentStage.OnValueChanged += HandleCurrentStageChanged;
        HandleCurrentStageChanged(0, CurrentStage.Value);

        if (IsOwner)
        {
            TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);        // See when to take off
        }
        HandleOnHourChanged(TimeManager.Main.CurrentHour);      // See if player can take off 
    }

    public override void OnNetworkDespawn()
    {
        if (mountInteraction == null || mountController == null)
        {
            Debug.LogError("HotAirBalloon Error: Missing MountInteraction or MountController scripts.");
            return;
        }

        CurrentStage.OnValueChanged -= HandleCurrentStageChanged;

        if (IsOwner)
        {
            TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
        }
    }

    private void TakeOff(Transform source)
    {
        if (!CanTakeOff.Value)      // Take off only if conditions are met
            return;

        if (source == null)
            return;

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
        {
            // TODO: Take Off From Mount Controller
            mountInteraction.SetCanMount(false);    // Player cannot dismount from balloon
        }
    }

    private void HandleOnHourChanged(int currentHour)
    {
        if (CanTakeOff.Value)
            return;

        if (currentHour == takeOffHour)
        {
            if (TimeManager.Main.CurrentDate == takeOffDate)
            {
                if (IsServer)
                {
                    CanTakeOff.Value = true;
                }
            }
        }
    }

    private void HandleCurrentStageChanged(int previousValue, int newValue)
    {
        spriteRenderer.sprite = upgradeStages.GetStage(newValue).sprite;
    }

    public void Interact(Transform source)
    {
        if (CurrentStage.Value < upgradeStages.GetStageCount() - 1)     // Player needs to upgrade balloon
            UpgradeUI.Main.Initialize(source.GetComponent<PlayerInventory>(), upgradeStages, CurrentStageValue, UpgradeBalloon);
        else                                              // Player can mount balloon now
        {
            isMounted = !isMounted;
            mountInteraction.SetCanMount(true);
            physicCollider.enabled = !isMounted;
            
            // Disable colliders/control of player
            if (source.TryGetComponent<PlayerMountHandler>(out var controller))
            {
                controller.SetControl(!isMounted);
            }

            mountInteraction.Interact(source);
        }
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
    private void TakeOffDebug()
    {
        if (!IsServer)
            return;

        TakeOff(null);
    }
}
