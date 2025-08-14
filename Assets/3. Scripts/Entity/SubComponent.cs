using System.Collections;
using UnityEngine;

public class SubComponent : MonoBehaviour, IDamageable
{
    [Header("SubComponent Settings")]
    [SerializeField]
    private Sprite originalSprite;
    [SerializeField]
    private Sprite damagedSprite;
    [SerializeField]
    private Sprite whiteSprite;

    [Header("SubComponent Debugs")]
    [SerializeField]
    private bool isDestroyed = false;

    private SpriteRenderer spriteRenderer;
    private Collider2D componentCollider;
    private ComponentStatus componentStatus;

    public uint CurrentHealthValue => componentStatus.CurrentHealthValue;
    public Hostility Hostility => componentStatus.Hostility;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        componentCollider = GetComponent<Collider2D>();
    }

    public void Initialize(ComponentStatus componentStatus)
    {
        this.componentStatus = componentStatus;
    }

    public bool TakeDamage(uint damage, DamageType damageType, Hostility attackerHostility, Transform attacker)
    {
        if (isDestroyed) return false;

        return componentStatus.TakeDamage(damage, damageType, attackerHostility, attacker);
    }

    public void DamageFlash()
    {
        if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private Coroutine damageFlashCoroutine;
    private IEnumerator DamageFlashCoroutine()
    {
        spriteRenderer.sprite = whiteSprite;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.sprite = originalSprite;
    }

    public void Destroyed()
    {
        isDestroyed = true;
        spriteRenderer.sprite = damagedSprite;
        componentCollider.enabled = false;
    }
}
