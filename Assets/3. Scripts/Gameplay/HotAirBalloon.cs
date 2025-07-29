using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

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
    public bool IsHoldInteractable => false;
    private Vector2 playerPosition;
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
        mountInteraction.OnMountOnClient += TakeOff;        // Take off if a player mounts
        isMounted = false;

        mountController.CanMove = false;

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

    private void HandleOnHourChanged(int currentHour)
    {
        Debug.Log($"Handling Hour Change: Date = {TimeManager.Main.CurrentDate}, Hour = {currentHour}");

        if (CanTakeOff.Value)
            return;

        if (!IsServer)
            return;

        if (CheckTakeOff(currentHour))
        {
            Debug.Log("Player can now take off");

            CanTakeOff.Value = true;

            if (isMounted)          // Take off if a player is already mounted
                TakeOff(null);
        }
        else
        {
            Debug.Log("Player can not take off yet");
        }
    }

    private void HandleCurrentStageChanged(int previousValue, int newValue)
    {
        spriteRenderer.sprite = upgradeStages.GetStage(newValue).sprite;
    }

    #region Take Off
    private void TakeOff(Transform source)
    {
        Debug.Log("Attempting take off...");

        if (!CanTakeOff.Value)      // Take off only if conditions are met
            return;

        Debug.Log("Hot Air Balloon has taken off.");

        mountInteraction.SetCanMount(false);    // Player cannot dismount from balloon
        mountController.CanMove = true;         // Enable movement and start movement
        mountController.Move(Vector2.zero);

        GetComponent<Collider2D>().enabled = false;
        GetComponent<SelectorModifier>().SetCanBeSelected(false);
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

        // Change sorting order of the hot air balloon
        // This will also affect the player since the player is a child of the hot air balloon
        var sortingGroup = GetComponent<SortingGroup>();
        sortingGroup.sortingLayerName = "UI";
        sortingGroup.sortingOrder = 99;

        // Disable the collider of the hot air balloon
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private bool CheckTakeOff(int currentHour)
    {
        if (TimeManager.Main.CurrentDate > takeOffDate)
        {
            return true;
        }
        else if (TimeManager.Main.CurrentDate >= takeOffDate)
        {
            if (currentHour >= takeOffHour)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

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

    #region Upgrade Balloon
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
    #endregion

    [ContextMenu("Take Off")]
    private void TakeOffDebug()
    {
        if (!IsServer)
            return;

        CanTakeOff.Value = true;
        TakeOff(null);
    }

    public void InteractionStart(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void InteractionEnd(Transform source)
    {
        throw new System.NotImplementedException();
    }
}
