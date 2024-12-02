using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class EntityStatus : NetworkBehaviour, IDamageable
{
    [Header("Entity Settings")]
    [SerializeField]
    private Hostility hostility;
    public Hostility Hostility => hostility;
    [SerializeField]
    private uint maxHealth;
    [SerializeField]
    private float iframeDuration = 0.1f;
    [SerializeField]
    private bool isInvincible;
    [SerializeField]
    protected GameObject damagedEffectPrefab;
    [SerializeField]
    protected GameObject deathEffectPrefab;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private NetworkVariable<uint> CurrentHealth = new NetworkVariable<uint>();
    public uint CurrentHealthValue => CurrentHealth.Value;

    [HideInInspector]
    public UnityEvent OnDeathOnServer;

    protected HealthBarUI healthBarUI;

    private bool isDamageable;
    private float nextDamagable;

    private void Awake()
    {
        healthBarUI = GetComponentInChildren<HealthBarUI>();
    }

    private void Update()
    {
        if (!IsServer) return;
        if (Time.time > nextDamagable && !isInvincible)
            isDamageable = true;
    }

    public override void OnNetworkSpawn()
    {
        HandleCurrentHealthChange(0, CurrentHealthValue);
        CurrentHealth.OnValueChanged += HandleCurrentHealthChange;
        if (IsServer)
        {
            OnEntitySpawnOnServer();
        }
        else
        {
            OnEntitySpawnOnClient();
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleCurrentHealthChange;
    }

    protected virtual void HandleCurrentHealthChange(uint previousValue, uint newValue)
    {
        if (healthBarUI == null) return;
        healthBarUI.SetValue(newValue, maxHealth);
    }

    public void GetDamaged(uint damage, DamageType type, Hostility hostility)
    {
        if (showDebugs) Debug.Log($"GetDamaged: {damage} {type} {hostility}");
        if (Hostility == hostility) return;

        GetDamagedServerRpc(damage, type);
    }

    [Rpc(SendTo.Server)]
    private void GetDamagedServerRpc(uint damage, DamageType type)
    {
        if (!isDamageable) return;

        GetDamagedClientRpc(damage, type);

        if (CurrentHealthValue > damage)
        {
            CurrentHealth.Value -= damage;
            nextDamagable = Time.time + iframeDuration;
            isDamageable = false;
            OnEntityDamagedOnServer();
        }
        else
        {
            CurrentHealth.Value = 0;
            OnEntityDeathOnServer();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void GetDamagedClientRpc(uint damage, DamageType type)
    {
        //if (!IsOwner) return;

        if (CurrentHealthValue > damage)
        {
            OnEntityDamagedOnClient();
        }
        else
        {
            OnEntityDeathOnClient();
        }


    }

    public uint GetCurrentHealth()
    {
        return CurrentHealthValue;
    }

    public Hostility GetHostility()
    {
        return Hostility;
    }

    protected virtual void OnEntityDamagedOnClient()
    {
        // Damaged effects
        if (damagedEffectPrefab != null)
            Instantiate(damagedEffectPrefab, transform.position, Quaternion.identity);
    }

    protected virtual void OnEntityDamagedOnServer()
    {
        // Damaged effects
    }

    protected virtual void OnEntityDeathOnServer()
    {
        OnDeathOnServer?.Invoke();
        OnDeathOnServer.RemoveAllListeners();
        NetworkObject.Despawn();
    }

    protected virtual void OnEntityDeathOnClient()
    {
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
    }

    protected virtual void OnEntitySpawnOnServer()
    {
        CurrentHealth.Value = maxHealth;
    }

    protected virtual void OnEntitySpawnOnClient()
    {

    }
}
