using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private ProjectileProperty property;

    private Transform owner;
    private Coroutine despawnCoroutine;

    private bool isInitialized;

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
        Initialize(transform, property);
    }

    public void Initialize(Transform owner, ProjectileProperty property)
    {
        this.owner = owner;
        this.property = property;

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

        if (collider.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.GetDamaged(property.Damage, property.DamageType, property.Hostility, owner);
            if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
            Despawn();
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
