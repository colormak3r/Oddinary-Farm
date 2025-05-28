using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField]
    private ProjectileProperty property;
    [SerializeField]
    private ParticleSystem vfxSystem;

    [Header("Debugs")]
    [SerializeField]
    protected bool showDebugs { get; private set; }
    protected Transform owner { get; private set; }
    protected bool isInitialized { get; private set; }
    protected bool isAuthoritative { get; private set; }

    private Coroutine despawnCoroutine;
    private SpriteRenderer spriteRenderer;

    public Action<Projectile, Transform> OnHitTarget;
    public Action<Projectile> OnDespawned;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    [ContextMenu("Mock Initialize")]
    private void MockInitialize()
    {
        Initialize(transform, property, true);
    }

    public void Initialize(Transform owner, ProjectileProperty property, bool isAuthoritative)
    {
        this.owner = owner;
        this.property = property;
        this.isAuthoritative = isAuthoritative;

        spriteRenderer.sprite = property.Sprite;
        if (property.PlayVfx) vfxSystem.Play();

        despawnCoroutine = StartCoroutine(DespawnCoroutine());
        isInitialized = true;
    }

    private IEnumerator DespawnCoroutine()
    {
        yield return new WaitForSeconds(property.LifeTime);
        Despawn();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isInitialized || owner == null) return;

        if (showDebugs) Debug.Log(collider.transform.root.name);

        if (collider.transform.root.TryGetComponent<NetworkBehaviour>(out var networkBehaviour))
        {
            if (!networkBehaviour.IsSpawned)
            {
                // If the target is not spawned, we do not hit it
                return;
            }
        }

        if (collider.transform.root.TryGetComponent<IDamageable>(out var damageable))
        {
            if (isAuthoritative)
            {
                // Only apply damage if the projectile is authoritative
                var success = damageable.TakeDamage(property.Damage, property.DamageType, property.Hostility, owner);
                if (success)
                {
                    if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
                    HitDespawn(collider.transform.root);
                }
                else
                {
                    // Pass through the target and continue
                }
            }
            else
            {
                // The projectile is not authoritative, so we don't apply damage
                // Still check if the target is damageable so we can despawn the projectile on non authoritative hits
                var success = damageable.TakeDamage(0, property.DamageType, property.Hostility, owner);
                if (success)
                {
                    if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
                    HitDespawn(collider.transform.root);
                }
                else
                {
                    // Pass through the target and continue
                }
            }

        }
    }

    protected void HitDespawn(Transform target)
    {
        LocalObjectPooling.Main.Despawn(gameObject);
        isInitialized = false;
        OnHitTarget?.Invoke(this, target);
        OnHitTarget = null;
    }

    protected void Despawn()
    {
        LocalObjectPooling.Main.Despawn(gameObject);
        isInitialized = false;
        OnDespawned?.Invoke(this);
    }

    public void DespawnOnClient()
    {
        Despawn();
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;
        transform.position += property.Speed * Time.fixedDeltaTime * transform.right;
    }
}
