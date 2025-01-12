using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField]
    private ProjectileProperty property;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

    private Transform owner;
    private Coroutine despawnCoroutine;

    private bool isInitialized;
    private bool isAuthoritative;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
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
        animator.runtimeAnimatorController = property.AnimatorController;

        despawnCoroutine = StartCoroutine(DespawnCoroutine());
        isInitialized = true;
    }

    private IEnumerator DespawnCoroutine()
    {
        yield return new WaitForSeconds(property.LifeTime);
        Despawn();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isInitialized || owner == null) return;

        if (showDebugs) Debug.Log(collider.name);

        if (collider.TryGetComponent<IDamageable>(out var damageable))
        {
            if (isAuthoritative)
            {
                var success = damageable.GetDamaged(property.Damage, property.DamageType, property.Hostility, owner);
                if (success)
                {
                    if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
                    Despawn();
                }
                else
                {
                    // Pass through the target and continue
                }
            }
            else
            {
                var success = damageable.GetDamaged(0, property.DamageType, property.Hostility, owner);
                if (success)
                {
                    if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
                    Despawn();
                }
                else
                {
                    // Pass through the target and continue
                }
            }

        }
    }

    private void Despawn()
    {
        LocalObjectPooling.Main.Despawn(gameObject);
        isInitialized = false;
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;
        transform.position += property.Speed * Time.fixedDeltaTime * transform.right;
    }
}
