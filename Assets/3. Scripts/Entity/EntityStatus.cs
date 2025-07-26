using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class EntityStatus : NetworkBehaviour, IDamageable
{
    [Header("Entity Settings")]
    [SerializeField]
    private Hostility hostility;
    public Hostility Hostility => hostility;
    [SerializeField]
    private uint maxHealth;
    public uint MaxHealth => maxHealth;
    [SerializeField]
    private float iframeDuration = 0.1f;
    [SerializeField]
    private bool isInvincible;
    [SerializeField]
    protected GameObject damagedEffectPrefab;
    [SerializeField]
    protected GameObject deathEffectPrefab;

    [Header("Entity Audio")]
    [SerializeField]
    protected AudioClip damagedSound;
    [SerializeField]
    protected AudioClip deathSound;

    [Header("Debugs")]
    [SerializeField]
    protected bool showDebugs;
    [SerializeField]
    private NetworkVariable<bool> CanBeHealed = new NetworkVariable<bool>(true);
    public bool CanBeHealedValue => CanBeHealed.Value;
    [SerializeField]
    private NetworkVariable<uint> CurrentHealth = new NetworkVariable<uint>();
    public uint CurrentHealthValue => CurrentHealth.Value;

    [HideInInspector]
    public UnityEvent OnDeathOnServer;
    public Action<int> OnAttackerListCountChange;
    [SerializeField]
    private List<Transform> attackerList = new List<Transform>();

    protected HealthBarUI healthBarUI;
    public HealthBarUI HealthBarUI => healthBarUI;
    protected LootGenerator lootGenerator;
    protected AudioElement audioElement;
    protected Rigidbody2D rbody;
    private ObservabilityController observabilityController;

    protected Collider2D[] colliders;
    protected SpriteRenderer[] renderers;
    protected Light2D[] lights;

    private float nextDamagable;

    protected virtual void Awake()
    {
        healthBarUI = GetComponentInChildren<HealthBarUI>();
        lootGenerator = GetComponent<LootGenerator>();
        audioElement = GetComponent<AudioElement>();
        rbody = GetComponent<Rigidbody2D>();
        observabilityController = GetComponent<ObservabilityController>();

        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        lights = GetComponentsInChildren<Light2D>();
    }

    public override void OnNetworkSpawn()
    {
        HandleCurrentHealthChange(0, CurrentHealthValue);

        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
            OnEntitySpawnOnServer();
        }
        else
        {
            OnEntitySpawnOnClient();
        }

        CurrentHealth.OnValueChanged += HandleCurrentHealthChange;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleCurrentHealthChange;
    }

    protected virtual void HandleCurrentHealthChange(uint previousValue, uint newValue)
    {

    }

    #region Heal
    [ContextMenu("Get Healed")]
    private void GetHealed()
    {
        GetHealed(1);
    }

    public bool GetHealed(uint healAmount)
    {
        if (showDebugs) Debug.Log($"GetHealed: HealAmount = {healAmount}");
        if (!IsSpawned) return false;
        if (!CanBeHealedValue) return false;
        if (CurrentHealthValue < maxHealth)
        {
            GetHealedRpc(healAmount);
            return true;
        }
        else
        {
            return false;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void GetHealedRpc(uint healAmount)
    {
        var newHealthValue = CurrentHealthValue + healAmount;
        if (newHealthValue > maxHealth)
        {
            if (showDebugs) Debug.Log($"GetHealedRpc: NewHealthValue = {newHealthValue} > MaxHealth = {maxHealth}");
            newHealthValue = maxHealth;
        }

        if (IsServer)
        {
            if (showDebugs) Debug.Log($"GetHealedRpc: NewHealthValue = {newHealthValue}");
            CurrentHealth.Value = newHealthValue;
        }

        if (healthBarUI) healthBarUI.SetValue(newHealthValue, maxHealth);
    }
    #endregion

    #region Can Be Healed
    public void SetCanBeHealed(bool canBeHealed)
    {
        SetCanBeHealedRpc(canBeHealed);
    }

    [Rpc(SendTo.Server)]
    public void SetCanBeHealedRpc(bool canBeHealed)
    {
        if (showDebugs) Debug.Log($"SetCanBeHealed: {canBeHealed} for {gameObject.name}");
        CanBeHealed.Value = canBeHealed;
    }
    #endregion

    #region Take Damage

    [ContextMenu("Take Damage")]
    private void TakeDamage()
    {
        TakeDamage(1, DamageType.Slash, Hostility.Absolute, null);
    }

    public bool TakeDamage(uint damage, DamageType damageType, Hostility attackerHostility, Transform attacker)
    {
        if (showDebugs) Debug.Log($"GetDamaged: Damage = {damage}, type = {damageType}, hostility = {attackerHostility}, from {attacker.gameObject} to {gameObject}", this);

        // Check if the attacker is hostile towards this entity
        // If the attacker is neutral, it will also damage neutral entities
        if (Hostility == attackerHostility && attackerHostility != Hostility.Neutral) return false;

        if (!IsSpawned) return false;

        if (isInvincible && damageType != DamageType.Absolute) return false;

        // Iframe to prevent multiple damage in a short time
        if (Time.time < nextDamagable) return false;
        nextDamagable = Time.time + iframeDuration;

        // Check if the attacker still exist before getting reference
        if (attacker != null && attacker.TryGetComponent(out NetworkBehaviour attackerNetworkBehaviour))
        {
            if (attackerNetworkBehaviour.IsSpawned)
            {
                TakeDamageRpc(damage, damageType, attackerHostility, attacker.gameObject);
            }
        }
        else
        {
            if (showDebugs) Debug.Log($"Attacker is not spawned or null");
            TakeDamageRpc(damage, damageType, attackerHostility, default);
        }

        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void TakeDamageRpc(uint damage, DamageType damageType, Hostility attackerHostility, NetworkObjectReference attackerRef)
    {
        // Notify AudioManager of potential combat event
        if (hostility != global::Hostility.Neutral && attackerHostility != global::Hostility.Absolute)
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            if (playerObject != null && Vector2.Distance(playerObject.transform.position, transform.position) < AudioManager.Main.CombatMusicRange)
            {
                AudioManager.Main.TriggerCombatMusic();
            }
        }

        if (CurrentHealthValue > damage)
        {
            // Entity Take Damage Events
            // Change UI 
            if (healthBarUI && damage > 0)
                healthBarUI.SetValue(CurrentHealthValue - damage, maxHealth);

            // Damaged sound
            if (audioElement)
                audioElement.PlayOneShot(damagedSound);

            // Damaged effects
            if (damagedEffectPrefab)
                Instantiate(damagedEffectPrefab, transform.position, Quaternion.identity);

            // Only the server should handle health changes
            if (IsServer)
            {
                CurrentHealth.Value -= damage;
                OnEntityDamagedOnServer(damage, attackerRef);
            }

            // Trigger callback on all clients
            OnEntityDamagedOnClient(damage, attackerRef);

            // Update stat on kill
            // Check if the attacker is the local player
            if (attackerRef.TryGet(out NetworkObject networkObject) && networkObject == NetworkManager.ConnectedClients[NetworkManager.LocalClientId].PlayerObject)
            {
                StatisticsManager.Main.UpdateStat(StatisticType.DamageDealt, gameObject.name, 1);
            }
        }
        else
        {
            // Entity Death Events
            if (IsServer)
            {
                CurrentHealth.Value = 0;

                OnDeathOnServer?.Invoke();
                OnDeathOnServer.RemoveAllListeners();

                // TODO: Create virtual method that check for loot drop condition
                if (lootGenerator != null && damageType != DamageType.Water)
                {
                    if (TryGetComponent(out Plant plant))
                    {
                        if (plant.IsHarvestable)
                            lootGenerator.DropLoot(attackerRef);
                    }
                    else
                    {
                        lootGenerator.DropLoot(attackerRef);
                    }
                }

                isInvincible = true;
                OnEntityDeathOnServer();
            }

            // Update stat on kill
            // Check if the attacker is the local player
            if (attackerRef.TryGet(out NetworkObject networkObject) && networkObject == NetworkManager.ConnectedClients[NetworkManager.LocalClientId].PlayerObject)
            {
                StatisticsManager.Main.UpdateStat(StatisticType.EntitiesKilled, gameObject.name, 1);
            }

            OnEntityDeathOnClient();
        }
    }

    #endregion

    #region On Damaged

    protected virtual void OnEntityDamagedOnClient(uint damage, NetworkObjectReference attackerRef)
    {

    }

    protected virtual void OnEntityDamagedOnServer(uint damage, NetworkObjectReference attackerRef)
    {

    }

    #endregion

    #region On Death

    protected virtual void OnEntityDeathOnServer()
    {
        if (observabilityController) observabilityController.DespawnOnServer();

        NetworkObject.Despawn(false);
    }

    protected virtual void OnEntityDeathOnClient()
    {
        SpawnDeathPrefab();

        StartCoroutine(DeathOnClientCoroutine());
    }

    protected virtual void SpawnDeathPrefab()
    {
        if (deathEffectPrefab != null)
        {
            var damageObj = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            var audioElement = damageObj.GetComponent<AudioElement>();
            if (audioElement && deathSound) audioElement.PlayOneShot(deathSound);
        }
    }

    protected virtual IEnumerator DeathOnClientCoroutine()
    {
        Coroutine effectCoroutine = null;

        // Disable all physics
        if (rbody) rbody.linearVelocity = Vector2.zero;
        foreach (var collider in colliders) collider.enabled = false;

        effectCoroutine = StartCoroutine(transform.PopCoroutine(1, 0, 0.25f));

        yield return effectCoroutine;

        // Under heavy load, the object might not be despawned immediately on server
        yield return new WaitUntil(() => IsSpawned == false);
        Destroy(gameObject);
    }

    #endregion

    #region On Spawn

    protected virtual void OnEntitySpawnOnServer()
    {

    }

    protected virtual void OnEntitySpawnOnClient()
    {

    }

    #endregion

    #region On Respawn

    public void Respawn()
    {
        RespawnRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void RespawnRpc()
    {
        if (IsServer)
        {
            isInvincible = false;
            CurrentHealth.Value = maxHealth;
            OnEntityRespawnOnServer();
        }

        OnEntityRespawnOnClient();
    }

    protected virtual void OnEntityRespawnOnServer()
    {

    }

    protected virtual void OnEntityRespawnOnClient()
    {

    }

    #endregion

    #region Utility

    public void SetCurrentHealth(uint healthValue)
    {
        SetCurrentHealthRpc(healthValue);
    }

    [Rpc(SendTo.Everyone)]
    private void SetCurrentHealthRpc(uint healthValue)
    {
        if (showDebugs) Debug.Log($"SetCurrentHealth: {healthValue} for {gameObject.name}");

        if (IsServer)
        {
            CurrentHealth.Value = healthValue;
        }
        else
        {
            if (healthBarUI) healthBarUI.SetValue(healthValue, maxHealth);
        }
    }

    /*private void OnValidate()
    {
        // Check how many bits are set
        int bitsSet = CountSetBits((int)hostility);

        if (bitsSet > 1)
        {
            Debug.LogWarning("Only one Hostility value can be selected at a time. Defaulting to first selected.");
            // Keep the least significant bit only
            int firstBit = (int)hostility & -(int)hostility;
            hostility = (Hostility)firstBit;
        }
    }

    private int CountSetBits(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }*/
    #endregion
}
