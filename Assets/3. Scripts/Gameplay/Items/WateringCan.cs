using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;

public class WateringCan : Item
{
    private WateringCanProperty wateringCanProperty;
    private int currentCharge;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        wateringCanProperty = (WateringCanProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        return IsInRange(position);
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        WaterRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void WaterRpc(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, wateringCanProperty.Radius, wateringCanProperty.WaterableLayer);
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
