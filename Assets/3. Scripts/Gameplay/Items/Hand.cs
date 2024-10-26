using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hand : MeleeWeapon
{
    private HandProperty handProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        handProperty = (HandProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        return IsInRange(position);
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        HandRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void HandRpc(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, handProperty.Radius);
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

        DealDamageRpc(position);
    }
}