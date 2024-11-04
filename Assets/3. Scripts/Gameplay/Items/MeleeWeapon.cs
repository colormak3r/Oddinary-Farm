using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class MeleeWeapon : Item
{
    [SerializeField]
    private MeleeWeaponProperty meleeWeaponProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        meleeWeaponProperty = (MeleeWeaponProperty)newValue;
    }

    // This method can run on both server and client
    protected virtual void DealDamage(Vector2 position)
    {
        var hits = Physics2D.CircleCastAll(transform.position, meleeWeaponProperty.Radius, position - (Vector2)transform.position, meleeWeaponProperty.Range, meleeWeaponProperty.DamageableLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider.gameObject == gameObject) continue;

                if (collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.GetDamaged(meleeWeaponProperty.Damage, meleeWeaponProperty.DamageType);
                }

                if (damageable.GetCurrentHealth() == 0) continue;

                if (collider.TryGetComponent<EntityMovement>(out var movement))
                {
                    movement.Knockback(meleeWeaponProperty.KnockbackForce, transform);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeWeaponProperty.Range);
    }
}
