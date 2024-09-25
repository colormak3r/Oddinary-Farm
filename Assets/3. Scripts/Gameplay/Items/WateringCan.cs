using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;

public class WateringCan : Item
{
    private WateringCanProperty property;
    private int currentCharge;

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
        property = (WateringCanProperty)newValue;
    }

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        //if (currentCharge <= 0) return;
        base.OnPrimaryAction(position, inventory);

        WaterRpc(position);
        return true;
    }

    [Rpc(SendTo.Server)]
    private void WaterRpc(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, property.Radius, property.WaterableLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IWaterable>(out var waterable))
                {
                    waterable.GetWatered();
                }
            }
        }
    }
}
