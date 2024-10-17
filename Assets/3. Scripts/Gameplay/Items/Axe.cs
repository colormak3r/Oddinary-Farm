using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class Axe : Item
{
    [SerializeField]
    private AxeProperty property;

    public override void OnNetworkSpawn()
    {
        HandleOnPropertyChanged(null, PropertyValue);
        Property.OnValueChanged += HandleOnPropertyChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandleOnPropertyChanged;
    }

    private void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        property = (AxeProperty)newValue;
    }

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        AxePrimaryRpc(position);
        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void AxePrimaryRpc(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, property.Range, property.DamageableLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                if (IsServer && hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.GetDamaged(property.Damage, property.DamageType);
                }

                if (hit.TryGetComponent<EntityMovement>(out var movement))
                {
                    movement.Knockback(property.KnockbackForce, transform);
                }
            }
        }
    }
}
