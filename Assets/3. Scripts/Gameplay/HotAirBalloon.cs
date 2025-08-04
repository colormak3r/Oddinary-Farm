/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   08/03/2025 (Khoa)
 * Notes:           <write here>
*/

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

    [Header("Hot Air Balloon Debugs")]
    [SerializeField]
    private bool showDebugs;

    /// <summary>
    /// Tracks the current upgrade stage of the hot air balloon.
    /// upgradeStages.GetStageCount() - 1 indicates the maximum stage.
    /// The value starts at 0 and goes up to upgradeStages.GetStageCount() - 1.
    /// </summary>
    [SerializeField]
    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    public int CurrentStageValue => CurrentStage.Value;
    public bool IsFullyUpgraded => CurrentStage.Value >= upgradeStages.GetStageCount() - 1;

    /// <summary>
    /// Checks if the hot air balloon has taken off.
    /// </summary>
    [SerializeField]
    private NetworkVariable<bool> HasTakenOff = new NetworkVariable<bool>();

    public bool IsHoldInteractable => false;

    private MountInteraction mountInteraction;
    private HotAirBalloonMountController mountController;

    public override void OnNetworkSpawn()
    {
        mountController = GetComponent<HotAirBalloonMountController>();

        // Keep track of when the player has mounted the hot air balloon
        // Try to take off when the player mounts the balloon
        mountInteraction = GetComponent<MountInteraction>();
        mountInteraction.OnMountOnServer += CheckTakeOffOnMount;

        CurrentStage.OnValueChanged += HandleCurrentStageChanged;
        HandleCurrentStageChanged(0, CurrentStage.Value);           // Initialize current stage

        HasTakenOff.OnValueChanged += HandleHasTakenOffChanged;
        HandleHasTakenOffChanged(false, HasTakenOff.Value);         // Initialize taken off state

        if (IsServer)
        {
            // See when to take off
            TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
            HandleOnHourChanged(TimeManager.Main.CurrentHour);      // See if player can take off 
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentStage.OnValueChanged -= HandleCurrentStageChanged;
        HasTakenOff.OnValueChanged -= HandleHasTakenOffChanged;
        mountInteraction.OnMountOnServer -= CheckTakeOffOnMount;

        if (IsServer)
        {
            TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
        }
    }

    private void HandleOnHourChanged(int currentHour)
    {
        if (showDebugs) Debug.Log($"Handling Hour Change: Date = {TimeManager.Main.CurrentDate}, Hour = {currentHour}");

        if (HasTakenOff.Value) return;

        if (CheckTakeOff(currentHour))
        {
            if (showDebugs) Debug.Log("Player can now take off");
            // Take off if a player is already mounted
            // This is checked every hour but in reality it will be called very quickly after the flood
            if (mountInteraction.HasOwner) TakeOffOnServer();
        }
        else
        {
            if (showDebugs) Debug.Log("Player can not take off yet");
        }
    }

    private void HandleCurrentStageChanged(int previousValue, int newValue)
    {
        spriteRenderer.sprite = upgradeStages.GetStage(newValue).sprite;
    }

    private void HandleHasTakenOffChanged(bool previousValue, bool newValue)
    {
        // Run on both server and client
        if (newValue)
        {
            Debug.Log("Hot Air Balloon has taken off.");

            mountInteraction.SetCanMount(false);    // Player cannot dismount from balloon

            // Disable the collider of the hot air balloon
            var colliders = GetComponentsInChildren<Collider2D>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            GetComponent<SelectorModifier>().SetCanBeSelected(false);
            GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

            // Change sorting order of the hot air balloon
            // This will also affect the player since the player is a child of the hot air balloon
            var sortingGroup = GetComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "UI";
            sortingGroup.sortingOrder = 99;
        }
        else
        {
            //Reverse take off here in the future if needed
        }
    }

    #region Take Off

    /// <summary>
    /// Used to take off the hot air balloon on Clients. 
    /// For debugging purposes, this can be called from the Editor context menu.
    /// </summary>
    [ContextMenu("Take Off")]
    private void TakeOff()
    {
        if (HasTakenOff.Value) return;
        TakeOffRpc();
    }

    /// <summary>
    /// Method for OnMountOnServer callback.
    /// When a player mounts the hot air balloon, this method checks if the player can take off on the server.
    /// </summary>
    /// <param name="source"></param>
    private void CheckTakeOffOnMount(Transform source)
    {
        if (HasTakenOff.Value || !CheckTakeOff(TimeManager.Main.CurrentHour)) return;
        TakeOffRpc();
    }

    /// <summary>
    /// Used to take off the hot air balloon.
    /// Works on both server and client.
    /// </summary>
    [Rpc(SendTo.Server)]
    private void TakeOffRpc()
    {
        TakeOffOnServer();
    }

    /// <summary>
    /// Changes the state of the hot air balloon to taken off on the server.
    /// </summary>
    /// <param name="source"></param>
    private void TakeOffOnServer()
    {
        if (HasTakenOff.Value || !IsServer) return;
        HasTakenOff.Value = true;
        mountController.TakeOffOnServer();
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
        if (!IsFullyUpgraded)   // Player needs to upgrade balloon
        {
            UpgradeUI.Main.Initialize(source.GetComponent<PlayerInventory>(), upgradeStages, CurrentStageValue, UpgradeBalloon);
        }
        else                    // Player can mount balloon now
        {
            // No mounting logic here, mount interaction will handle it
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
        if (!IsFullyUpgraded)
        {
            if (showDebugs) Debug.Log("Upgrading Hot Air Balloon...");
            CurrentStage.Value++;

            // Check if the balloon is fully upgraded immidiately after upgrading
            if (IsFullyUpgraded)
            {
                if (showDebugs) Debug.Log("Hot Air Balloon is fully upgraded. Player can now mount the balloon.");
                mountInteraction.SetCanMount(true);
            }
        }
    }
    #endregion

    public void InteractionStart(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void InteractionEnd(Transform source)
    {
        throw new System.NotImplementedException();
    }
}
