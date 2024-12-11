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
                    damageable.GetDamaged(meleeWeaponProperty.Damage, meleeWeaponProperty.DamageType, meleeWeaponProperty.Hostility, transform.root);
                }

                if (damageable.GetHostility() == meleeWeaponProperty.Hostility)
                {
                    if (meleeWeaponProperty.DamageType == DamageType.Slash || meleeWeaponProperty.CanHarvest)
                    {
                        if (collider.TryGetComponent<Plant>(out var plant))
                        {
                            plant.GetHarvested(transform.root);
                        }
                    }
                }
                else
                {
                    // Check if the object is already dead
                    if (damageable.GetCurrentHealth() == 0) continue;

                    if (collider.TryGetComponent<EntityMovement>(out var movement))
                    {
                        movement.Knockback(meleeWeaponProperty.KnockbackForce, transform);
                    }
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
