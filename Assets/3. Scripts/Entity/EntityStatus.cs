using ColorMak3r.Utility;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

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

    [Header("Flood Settings")]
    [SerializeField]
    private float drownTickRate = 1f;

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

    protected Collider2D[] colliders;
    protected SpriteRenderer[] renderers;
    protected Light2D[] lights;
    private Coroutine drownCoroutine;

    private float nextDamagable;

    protected virtual void Awake()
    {
        healthBarUI = GetComponentInChildren<HealthBarUI>();
        lootGenerator = GetComponent<LootGenerator>();
        audioElement = GetComponent<AudioElement>();
        rbody = GetComponent<Rigidbody2D>();

        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        lights = GetComponentsInChildren<Light2D>();
    }

    public override void OnNetworkSpawn()
    {
        HandleCurrentHealthChange(0, CurrentHealthValue);

        if (IsServer)
        {
            FloodManager.Main.OnFloodLevelChanged += HandleOnFloodLevelChange;
            HandleOnFloodLevelChange(FloodManager.Main.CurrentFloodLevelValue, FloodManager.Main.CurrentWaterLevel);
            if (rbody != null) StartCoroutine(DynamicDrownBootstrap());

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
        if (IsServer)
        {
            FloodManager.Main.OnFloodLevelChanged -= HandleOnFloodLevelChange;
            if (rbody != null) StartCoroutine(DynamicDrownBootstrap());
        }
        CurrentHealth.OnValueChanged -= HandleCurrentHealthChange;
    }

    protected virtual void HandleCurrentHealthChange(uint previousValue, uint newValue)
    {

    }

    #region Flood Level

    private void HandleOnFloodLevelChange(float currentFloodLevel, float waterLevel)
    {
        if (rbody == null)
        {
            StartCoroutine(StaticDrownBootstrap(currentFloodLevel));
        }
        else
        {
            if (WorldGenerator.Main.IsInitialized)
                CheckFloodLevel(((Vector2)transform.position).SnapToGrid());
        }
    }

    private Vector2 position_cached = Vector2.one;
    private IEnumerator DynamicDrownBootstrap()
    {
        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);
        while (IsSpawned)
        {
            var position = ((Vector2)transform.position).SnapToGrid();
            if (position != position_cached)
            {
                position_cached = position;
                CheckFloodLevel(position);
            }
            yield return null;
        }
    }

    private void CheckFloodLevel(Vector2 position)
    {
        if (FloodManager.Main.CurrentFloodLevelValue > WorldGenerator.Main.GetElevation((int)position.x, (int)position.y))
        {
            if (drownCoroutine == null) drownCoroutine = StartCoroutine(DrownCoroutine());
        }
        else
        {
            if (drownCoroutine != null) { StopCoroutine(drownCoroutine); drownCoroutine = null; }
        }
    }

    private IEnumerator StaticDrownBootstrap(float currentFloodLevel)
    {
        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);

        var position = ((Vector2)transform.position).SnapToGrid();
        if (currentFloodLevel > WorldGenerator.Main.GetElevation((int)position.x, (int)position.y))
        {
            if (drownCoroutine != null) StopCoroutine(drownCoroutine);
            drownCoroutine = StartCoroutine(DrownCoroutine());
        }
    }

    private IEnumerator DrownCoroutine()
    {
        while (CurrentHealthValue > 0)
        {
            yield return new WaitForSeconds(drownTickRate);
            GetDamaged();
        }
    }

    #endregion

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

    #region Get Damaged

    [ContextMenu("Get Damaged")]
    private void GetDamaged()
    {
        GetDamaged(1, DamageType.Slash, Hostility.Neutral, null);
    }

    public bool GetDamaged(uint damage, DamageType type, Hostility hostility, Transform attacker)
    {
        if (showDebugs) Debug.Log($"GetDamaged: Damage = {damage}, type = {type}, hostility = {hostility}");

        if (Hostility == hostility && hostility != Hostility.Neutral) return false;

        if (!IsSpawned) return false;

        if (isInvincible) return false;

        if (Time.time < nextDamagable) return false;
        nextDamagable = Time.time + iframeDuration;


        GetDamagedRpc(damage, type, attacker?.gameObject);

        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void GetDamagedRpc(uint damage, DamageType type, NetworkObjectReference attackerRef)
    {
        if (CurrentHealthValue > damage)
        {
            if (healthBarUI && damage > 0)
                healthBarUI.SetValue(CurrentHealthValue - damage, maxHealth);


            // Damaged sound
            if (audioElement)
                audioElement.PlayOneShot(damagedSound);

            // Damaged effects
            if (damagedEffectPrefab)
                Instantiate(damagedEffectPrefab, transform.position, Quaternion.identity);

            if (IsServer)
            {
                CurrentHealth.Value -= damage;
                OnEntityDamagedOnServer();
            }

            OnEntityDamagedOnClient();
        }
        else
        {
            if (IsServer)
            {
                CurrentHealth.Value = 0;

                OnDeathOnServer?.Invoke();
                OnDeathOnServer.RemoveAllListeners();

                // TODO: Create virtual method that check for loot drop condition
                // TODO: Pass attacker as prefer picker to loot generator
                if (lootGenerator != null)
                {
                    if (TryGetComponent(out Plant plant))
                    {
                        if (plant.IsHarvestable)
                            lootGenerator.DropLootOnServer(attackerRef);
                    }
                    else
                    {
                        lootGenerator.DropLootOnServer(attackerRef);
                    }
                }

                isInvincible = true;
                OnEntityDeathOnServer();
            }

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

        // Disable all physics
        if (rbody) rbody.linearVelocity = Vector2.zero;
        foreach (var collider in colliders) collider.enabled = false;

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

    public uint GetCurrentHealth() => CurrentHealthValue;
    public Hostility GetHostility() => Hostility;

    #endregion
}
