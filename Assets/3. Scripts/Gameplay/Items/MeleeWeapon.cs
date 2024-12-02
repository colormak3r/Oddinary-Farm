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

                // Deal damage to the object
                if (collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.GetDamaged(meleeWeaponProperty.Damage, meleeWeaponProperty.DamageType, meleeWeaponProperty.Hostility);
                }

                // Check if the object is already dead
                if (damageable.GetCurrentHealth() == 0) continue;

                // Check hostility before applying knockback
                if (damageable.GetHostility() == meleeWeaponProperty.Hostility) continue;

                if (collider.TryGetComponent<EntityMovement>(out var movement))
                {
                    movement.Knockback(meleeWeaponProperty.KnockbackForce, transform);
                }
            }
        }
    }

    protected void Harvest(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, meleeWeaponProperty.Radius);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IHarvestable>(out var harvestable))
                {
                    harvestable.GetHarvested();
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || meleeWeaponProperty == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeWeaponProperty.Range);
    }
}
