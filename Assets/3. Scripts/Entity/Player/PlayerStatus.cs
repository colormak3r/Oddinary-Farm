using ColorMak3r.Utility;
using Steamworks;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerStatus : EntityStatus
{
    [Header("Player Settings")]
    [SerializeField]
    private Transform respawnPoint;
    [SerializeField]
    private Collider2D playerHitbox;
    [SerializeField]
    private PlayerNameUI playerNameUI;

    private NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, default, NetworkVariableWritePermission.Owner);

    public string PlayerNameValue => PlayerName.Value.ToString();
    public PlayerNameUI PlayerNameUI => playerNameUI;

    private IControllable[] controllables;
    private NetworkTransform networkTransform;

    private ulong timeDied;
    private float timeSinceLastDeath;

    protected override void Awake()
    {
        base.Awake();
        controllables = GetComponentsInChildren<IControllable>();
        networkTransform = GetComponent<NetworkTransform>();
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
        healthBarUI.SetValue(CurrentHealth, MaxHealth);
    }

    protected override IEnumerator DeathOnClientCoroutine()
    {
        // Record death data
        timeDied++;
        StatisticsManager.Main.UpdateStat(StatisticType.TimeDied, timeDied);
        StatisticsManager.Main.UpdateStat(StatisticType.TimeSinceLastDeath, (ulong)Mathf.RoundToInt(Time.time - timeSinceLastDeath));
        timeSinceLastDeath = Time.time;

        Coroutine effectCoroutine = null;

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
            effectCoroutine = StartCoroutine(transform.PopCoroutine(1, 0, 0.25f));
        }

        yield return effectCoroutine;

        // Disable all renderers
        //foreach (var renderer in renderers) renderer.enabled = false;
        foreach (var light in lights) light.enabled = false;

        // Determain respawn position
        var respawnPos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        var deathPos = transform.position;

        /*var int_respawnPos = ((Vector2)respawnPos).ToInt();
        if (WorldGenerator.Main.GetElevation(int_respawnPos.x, int_respawnPos.y) < FloodManager.Main.CurrentFloodLevelValue)
        {
            yield break;
        }*/

        // Black out and move player to respawn position
        if (IsOwner)
        {
            yield return new WaitForSeconds(3f);
            yield return TransitionUI.Main.ShowCoroutine();

            transform.position = respawnPos;
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);

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
}
