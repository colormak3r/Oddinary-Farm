using ColorMak3r.Utility;

using System;
using System.Collections;
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
    private bool showDebugs;
    [SerializeField]
    private NetworkVariable<uint> CurrentHealth = new NetworkVariable<uint>();
    public uint CurrentHealthValue => CurrentHealth.Value;

    [HideInInspector]
    public UnityEvent OnDeathOnServer;

    protected HealthBarUI healthBarUI;
    protected LootGenerator lootGenerator;
    protected AudioElement audioElement;
    protected Rigidbody2D rbody;

    private bool isDamageable;
    private float nextDamagable;

    private bool omawumishindeiru;

    protected virtual void Awake()
    {
        healthBarUI = GetComponentInChildren<HealthBarUI>();
        lootGenerator = GetComponent<LootGenerator>();
        audioElement = GetComponent<AudioElement>();
        rbody = GetComponent<Rigidbody2D>();
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

    #region Get Damaged

    public void GetDamaged(uint damage, DamageType type, Hostility hostility)
    {
        if (showDebugs) Debug.Log($"GetDamaged: Damage = {damage}, type = {type}, hostility = {hostility}");
        if (Hostility == hostility) return;
        if (!IsSpawned) return;

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

            OnDeathOnServer?.Invoke();
            OnDeathOnServer.RemoveAllListeners();

            // TODO: Create virtual method that check for loot drop condition
            if (lootGenerator != null && TryGetComponent(out Plant plant) && plant.IsHarvestable())
                lootGenerator.DropLootOnServer();

            OnEntityDeathOnServer();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void GetDamagedClientRpc(uint damage, DamageType type)
    {
        //if (!IsOwner) return;

        if (CurrentHealthValue > damage)
        {
            if (healthBarUI)
                healthBarUI.SetValue(CurrentHealthValue - damage, maxHealth);

            // Damaged sound
            if (audioElement)
                audioElement.PlayOneShot(damagedSound);

            // Damaged effects
            if (damagedEffectPrefab)
                Instantiate(damagedEffectPrefab, transform.position, Quaternion.identity);

            OnEntityDamagedOnClient();
        }
        else
        {
            OnEntityDeathOnClient();
        }
    }

    #endregion

    #region On Damaged

    protected virtual void OnEntityDamagedOnClient()
    {

    }

    protected virtual void OnEntityDamagedOnServer()
    {

    }

    #endregion

    #region On Death

    protected virtual void OnEntityDeathOnServer()
    {
        NetworkObject.Despawn(false);
    }

    protected virtual void OnEntityDeathOnClient()
    {
        StartCoroutine(DeathOnClientCoroutine());
    }

    protected virtual IEnumerator DeathOnClientCoroutine()
    {
        Coroutine audioCoroutine = null;
        Coroutine effectCoroutine = null;
        if (audioElement)
        {
            audioElement.PlayOneShot(deathSound);
            audioCoroutine = StartCoroutine(MiscUtility.WaitCoroutine(deathSound.length));
        }

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (rbody)
            rbody.velocity = Vector2.zero;

        effectCoroutine = StartCoroutine(transform.PopCoroutine(1, 0, 0.25f));

        yield return effectCoroutine;
        yield return audioCoroutine;

        Destroy(gameObject);
    }




    #endregion

    #region On Spawn

    protected virtual void OnEntitySpawnOnServer()
    {

    }

    protected virtual void OnEntitySpawnOnClient()
    {
        omawumishindeiru = false;
    }

    #endregion

    #region On Respawn

    public void Respawn()
    {
        RespawnServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RespawnServerRpc()
    {
        CurrentHealth.Value = maxHealth;

        OnEntityRespawnOnServer();

        RespawnClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void RespawnClientRpc()
    {
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

    public uint GetCurrentHealth() => CurrentHealthValue;
    public Hostility GetHostility() => Hostility;

    #endregion
}
