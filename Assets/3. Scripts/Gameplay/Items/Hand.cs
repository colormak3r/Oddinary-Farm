using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hand : Item
{
    private HandProperty property;

    public override void OnNetworkSpawn()
    {
        Property.OnValueChanged += HandleOnPropertyChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandleOnPropertyChanged;
    }

    private void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        property = (HandProperty)newValue;
    }

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        HandRpc(position);
        return true;
    }

    [Rpc(SendTo.Server)]
    private void HandRpc(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, property.Radius);
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
}