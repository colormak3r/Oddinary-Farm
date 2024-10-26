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

    [Rpc(SendTo.Server)]
    protected virtual void DealDamageRpc(Vector2 position)
    {
        var hits = Physics2D.CircleCastAll(transform.position, meleeWeaponProperty.Radius, position - (Vector2)transform.position, meleeWeaponProperty.Range, meleeWeaponProperty.DamageableLayer);
        if (hits.Length > 0)
        {
            Debug.Log("Hit " + hits.Length + " entities");
            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider.gameObject == gameObject) continue;
                Debug.Log("Hit " + collider.name);
                if (IsServer && collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.GetDamaged(meleeWeaponProperty.Damage, meleeWeaponProperty.DamageType);
                }

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
