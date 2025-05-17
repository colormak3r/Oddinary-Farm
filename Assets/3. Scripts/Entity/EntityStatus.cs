using ColorMak3r.Utility;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

// Common status effects and behaviors for entities
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
    private bool showDebugs;        // NOTE: consider renaming to "showLogs" or "debugLogs" for clarity
    [SerializeField]
    private NetworkVariable<uint> CurrentHealth = new NetworkVariable<uint>();
    public uint CurrentHealthValue => CurrentHealth.Value;

    [HideInInspector]
    public UnityEvent OnDeathOnServer;

    protected HealthBarUI healthBarUI;
    public HealthBarUI HealthBarUI => healthBarUI;
    protected LootGenerator lootGenerator;
    protected AudioElement audioElement;        // Custom AudioSource Wrapper
    protected Rigidbody2D rbody;

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

        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        lights = GetComponentsInChildren<Light2D>();
    }

    public override void OnNetworkSpawn()
    {
        // Init. Health
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
        // QUESTION: Why is this here if there is no code inside the method
    }

    #region Heal

    // Heal the player through context menu
    [ContextMenu("Get Healed")]
    private void GetHealed()
    {
        GetHealed(1);
    }

    public bool GetHealed(uint healAmount)
    {
        if (showDebugs) 
            Debug.Log($"GetHealed: HealAmount = {healAmount}");

        if (!IsSpawned)       // NOTE: Move this early return above the last statement; makes more sense 
            return false;

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

    [Rpc(SendTo.Everyone)]      // Executed on all clients
    private void GetHealedRpc(uint healAmount)
    {
        var newHealthValue = CurrentHealthValue + healAmount;

        if (newHealthValue > maxHealth)     // Don't heal over max hp
        {
            if (showDebugs) Debug.Log($"GetHealedRpc: NewHealthValue = {newHealthValue} > MaxHealth = {maxHealth}");
            newHealthValue = maxHealth;
        }

        if (IsServer)       // Set new health on server
        {
            if (showDebugs) Debug.Log($"GetHealedRpc: NewHealthValue = {newHealthValue}");
            CurrentHealth.Value = newHealthValue;
        }

        if (healthBarUI) healthBarUI.SetValue(newHealthValue, maxHealth);
    }
    #endregion

    #region Take Damaged
    // Deal damage to the player though the context menu
    [ContextMenu("Take Damaged")]
    private void TakeDamage()
    {
        // TakeDamage(value, type, whosImmune, attackerTransform)
        TakeDamage(1, DamageType.Slash, Hostility.Neutral, null);
    }

    public bool TakeDamage(uint damage, DamageType type, Hostility hostility, Transform attacker)
    {
        if (showDebugs) Debug.Log($"GetDamaged: Damage = {damage}, type = {type}, hostility = {hostility}");

        if (Hostility == hostility && hostility != Hostility.Neutral) return false;     // Do not take damage

        if (!IsSpawned) return false;     // Do not take damage

        if (isInvincible) return false;     // Do not take damage

        if (Time.time < nextDamagable) return false;         // If invulnerable return
        nextDamagable = Time.time + iframeDuration;

        TakeDamageRpc(damage, type, attacker?.gameObject);      // Take damage

        return true;
    }

    [Rpc(SendTo.Everyone)]      // Executed on all clients
    private void TakeDamageRpc(uint damage, DamageType type, NetworkObjectReference attackerRef)
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
                if (lootGenerator != null && type != DamageType.Water)
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

        // Play Audio
        if (audioElement)
        {
            audioElement.PlayOneShot(deathSound);        // Custom AudioSource Wrapper
            audioCoroutine = StartCoroutine(MiscUtility.WaitCoroutine(deathSound.length));      // Wait full length of clip
        }

        // Play VFX
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Disable all physics
        if (rbody) rbody.linearVelocity = Vector2.zero;
        foreach (var collider in colliders) collider.enabled = false;

        effectCoroutine = StartCoroutine(transform.PopCoroutine(1, 0, 0.25f));

        yield return effectCoroutine;       // Finish Effect
        yield return audioCoroutine;        // Finish Audio

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

    [Rpc(SendTo.Everyone)]      // Executed on all clients
    private void RespawnRpc()
    {
        if (IsServer)       // If server respawn player
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

    // NOTE: Unreleated, but looking into what this function does I found this cool way of specifying enums
    /*       that makes enums into actual bitmasks, allowing you to select more than one in the inspector
    [System.Flags]
    public enum Hostility
    {
        None     = 0,      // 0 = 0b000
        Neutral  = 1 << 0, // 1 = 0b001
        Friendly = 1 << 1, // 2 = 0b010
        Hostile  = 1 << 2  // 4 = 0b100
    }
    */

    // Bitmask Validation; Makes sure two hostilities are never enabled at once
    private void OnValidate()       // Script Loaded or changed in the inspector; after values change
{
    // Check how many bits are set
    int bitsSet = CountSetBits((int)hostility);

    if (bitsSet > 1)
    {
        Debug.LogWarning("Only one Hostility value can be selected at a time. Defaulting to first selected.");

        // Isolate least significant bit
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
}
#endregion
}
