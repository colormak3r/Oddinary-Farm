using ColorMak3r.Utility;
using Steamworks;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public enum PlayerCurse
{
    None,
    GoldenCarrot,
    GlassCannon,
    VacationMode,
}

public class PlayerStatus : EntityStatus
{
    [Header("Player Settings")]
    [SerializeField]
    private Collider2D playerHitbox;
    [SerializeField]
    private PlayerNameUI playerNameUI;

    [Header("Player Debugs")]
    [SerializeField]
    private PlayerCurse currentCurse = PlayerCurse.None;
    public PlayerCurse CurrentCurse => currentCurse;

    private NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, default, NetworkVariableWritePermission.Owner);
    public string PlayerNameValue => PlayerName.Value.ToString();
    public PlayerNameUI PlayerNameUI => playerNameUI;

    private IControllable[] controllables;
    private NetworkTransform networkTransform;
    private EntityMovement entityMovement;
    private PlayerController playerController;
    private PlayerInventory playerInventory;

    private ulong timeDied;
    private float timeSinceLastDeath;

    protected override void Awake()
    {
        base.Awake();
        controllables = GetComponentsInChildren<IControllable>();
        networkTransform = GetComponent<NetworkTransform>();
        entityMovement = GetComponent<EntityMovement>();
        playerController = GetComponent<PlayerController>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        gameObject.name = NetworkObject.OwnerClientId == 0 ? "Host" : $"Client {NetworkObject.OwnerClientId}";

        HandlePlayerNameChange(default, PlayerName.Value);
        PlayerName.OnValueChanged += HandlePlayerNameChange;

        if (IsOwner)
        {
            if (SteamClient.IsValid)
                PlayerName.Value = SteamClient.Name;

            timeSinceLastDeath = Time.time;

            PetManager.Main.InitializeOnLocalClient(gameObject);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerName.OnValueChanged -= HandlePlayerNameChange;
    }

    private void HandlePlayerNameChange(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        playerNameUI.SetPlayerName(newValue.ToString());
    }


    private Vector2 position_cached;
    private uint vacationPay = 1;
    private float nextVacationPayTime = 0f;
    private void Update()
    {
        if (IsOwner && currentCurse == PlayerCurse.VacationMode)
        {
            var snappedPosition = transform.position.SnapToGrid();
            if (snappedPosition != position_cached)
            {
                var snappedXLength = Mathf.Abs(snappedPosition.x - position_cached.x);
                var snappedYLength = Mathf.Abs(snappedPosition.y - position_cached.y);
                float maxSnappedLength = Mathf.Max(snappedXLength, snappedYLength);
                position_cached = snappedPosition;

                // If the snapped position is more than 1 tile away, deduct a penalty from the wallet
                // the penalty is 1% of the local wallet value per tile snapped
                var penalty = WalletManager.Main.LocalWalletValue / 100 * maxSnappedLength;
                WalletManager.Main.RemoveFromWallet((uint)penalty);
                vacationPay = 0;
            }
            else
            {
                // If the player is not moving, increment vacation pay every second
                if (Time.time > nextVacationPayTime)
                {
                    nextVacationPayTime = Time.time + 1f;
                    WalletManager.Main.AddToWallet(vacationPay);
                    vacationPay++;
                }
            }
        }
    }

    #region Entity Status
    protected override void OnEntityDamagedOnClient(uint damage, NetworkObjectReference attackerRef)
    {
        base.OnEntityDamagedOnClient(damage, attackerRef);

        // Update Stats
        if (attackerRef.TryGet(out NetworkObject networkObject))
        {
            StatisticsManager.Main.UpdateStat(StatisticType.DamageTaken, networkObject.gameObject.name, damage);
        }
        else
        {
            StatisticsManager.Main.UpdateStat(StatisticType.DamageTaken, "Unidentified", damage);
        }
    }

    protected override void OnEntityDeathOnServer()
    {
        // Override to prevent player from being destroyed
        playerHitbox.enabled = false;
    }

    protected override void OnEntityRespawnOnClient()
    {
        healthBarUI.SetValue(CurrentHealthValue, MaxHealth);
    }

    protected override IEnumerator DeathOnClientCoroutine()
    {
        // Record death data
        timeDied++;
        StatisticsManager.Main.UpdateStat(StatisticType.TimeDied, timeDied);
        StatisticsManager.Main.UpdateStat(StatisticType.TimeSinceLastDeath, (ulong)Mathf.RoundToInt(Time.time - timeSinceLastDeath));
        timeSinceLastDeath = Time.time;

        // Remove player curses
        SetCurse(PlayerCurse.None);

        // Spawn SFX & VFX
        SpawnDeathPrefab();

        // Disable all physics
        rbody.linearVelocity = Vector2.zero;
        foreach (var collider in colliders) collider.enabled = false;

        if (IsOwner)
        {
            // Detach camera to prevent it being shrink
            Camera.main.transform.parent = null;

            // Disable all controllables
            foreach (var controllable in controllables)
            {
                controllable.SetControllable(false);
            }

            // Transform pop out
            yield return transform.PopCoroutine(1, 0, 0.25f);
        }

        // Disable all renderers
        //foreach (var renderer in renderers) renderer.enabled = false;
        foreach (var light in lights) light.enabled = false;

        // Determain respawn position
        var respawnPos = GameManager.Main.SpawnPoint;
        //var deathPos = transform.position;

        // Prevent respawning in water or if the map is flooded
        // Remove temporarily for behavior change
        /*var int_respawnPos = ((Vector2)respawnPos).ToInt();
        if (WorldGenerator.Main.GetElevation(int_respawnPos.x, int_respawnPos.y) < FloodManager.Main.CurrentFloodLevelValue)
        {
            yield break;
        }*/

        // Black out and move player to respawn position
        if (IsOwner)
        {
            yield return CountdownUI.Main.CountdownRoutine(TimeManager.Main.IsDay ? 3f : 10f);
            yield return TransitionUI.Main.ShowCoroutine();

            transform.position = respawnPos;
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);

            yield return new WaitUntil(() => !WorldGenerator.Main.IsGenerating);
            WorldGenerator.Main.BuildWorld(transform.position);
            yield return new WaitUntil(() => !WorldGenerator.Main.IsGenerating);

            yield return TransitionUI.Main.HideCoroutine();
        }

        // Wait until player is at respawn position
        if (IsOwner) networkTransform.Teleport(respawnPos, Quaternion.identity, Vector3.one);
        yield return new WaitUntil(() => Vector3.Distance(transform.position, respawnPos) < 0.01f);

        foreach (var collider in colliders) collider.enabled = true;

        //foreach (var renderer in renderers) renderer.enabled = true;
        foreach (var light in lights) light.enabled = true;

        if (IsOwner)
        {
            // Transform pop in
            yield return transform.PopCoroutine(0, 1, 0.5f);

            Camera.main.transform.parent = transform;
            Camera.main.transform.localPosition = new Vector3(0, 0, -10);

            foreach (var controllable in controllables)
            {
                controllable.SetControllable(true);
            }
        }

        Respawn();
    }
    #endregion

    #region Player Curse

    [ContextMenu("Apply Curse")]
    private void ApplyCurse()
    {
        SetCurse(currentCurse, true);
    }

    public void SetCurse(PlayerCurse curse, bool debug = false)
    {
        if (currentCurse == curse && !debug) return;

        if (showDebugs) Debug.Log($"Setting curse: {curse}");

        // End the current curse if it exists
        EndStatusEffect();
        currentCurse = curse;

        switch (curse)
        {
            case PlayerCurse.GoldenCarrot:
                // Apply Golden Carrot effect
                playerInventory.SetGoldenCarrot(true);
                break;
            case PlayerCurse.GlassCannon:
                // Apply Glass Cannon effect
                // Player cannot be healed, speed is increased, and primary cooldown reduction is halved
                SetCanBeHealed(false);
                entityMovement.SetSpeedMultiplier(1.5f);
                playerController.SetPrimaryCdrModifier(0.5f);
                // Health is set to 3
                SetCurrentHealth(4);
                TakeDamage(1, DamageType.Slash, Hostility.Absolute, null);
                break;
            case PlayerCurse.VacationMode:
                // Apply Vacation Mode effect
                // Reset vacation pay to 1
                vacationPay = 1;
                break;
            default:
                // Remove all effects, do nothing
                break;
        }
    }

    private void EndStatusEffect()
    {
        switch (currentCurse)
        {
            case PlayerCurse.GoldenCarrot:
                // End Golden Carrot effect
                playerInventory.SetGoldenCarrot(false);
                break;
            case PlayerCurse.GlassCannon:
                // End Glass Cannon effect
                // Player can be healed, speed is reset, and primary cooldown reduction is reset
                SetCanBeHealed(true);
                entityMovement.SetSpeedMultiplier(1f);
                playerController.SetPrimaryCdrModifier(1f);
                break;
            case PlayerCurse.VacationMode:
                // End Vacation Mode effect
                // No additional effect to end
                break;
            case PlayerCurse.None:
                // No effect to end, do nothing
                break;
            default:
                // Should not reach here
                Debug.LogWarning($"Unknown PlayerStatusEffect: {currentCurse}");
                break;
        }
    }

    #endregion
}
