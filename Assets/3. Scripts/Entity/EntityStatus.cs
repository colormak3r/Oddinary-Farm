using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EntityStatus : NetworkBehaviour, IDamageable
{
    [Header("Entity Settings")]
    [SerializeField]
    private int maxHealth;
    [SerializeField]
    private float iframeDuration = 0.1f;
    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    public int CurrentHealthValue => CurrentHealth.Value;

    private bool isDamageable;
    private float nextDamagable;

    private void Update()
    {
        if (!IsServer) return;
        if (Time.time > nextDamagable)
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

    protected virtual void HandleCurrentHealthChange(int previousValue, int newValue)
    {

    }

    public void GetDamaged(int damage, DamageType type)
    {
        GetDamagedServerRpc(damage, type);
    }

    [Rpc(SendTo.Server)]
    private void GetDamagedServerRpc(int damage, DamageType type)
    {
        GetDamagedClientRpc(damage, type);

        if (CurrentHealthValue - damage >= 0)
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

    [Rpc(SendTo.ClientsAndHost)]
    private void GetDamagedClientRpc(int damage, DamageType type)
    {
        if (!IsOwner) return;

        if (CurrentHealthValue - damage > 0)
        {
            OnEntityDamagedOnClient();
        }
        else
        {
            OnEntityDeathOnClient();
        }
        

    }

    protected virtual void OnEntityDamagedOnClient()
    {
        // Damaged effects
    }

    protected virtual void OnEntityDamagedOnServer()
    {
        // Damaged effects
    }

    protected virtual void OnEntityDeathOnServer()
    {
        NetworkObject.Despawn();
    }

    protected virtual void OnEntityDeathOnClient()
    {
        
    }

    protected virtual void OnEntitySpawnOnServer()
    {
        CurrentHealth.Value = maxHealth;
    }

    protected virtual void OnEntitySpawnOnClient()
    {

    }
}
