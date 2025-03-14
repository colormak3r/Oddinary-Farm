using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;

public class WateringCan : Tool
{
    private WateringCanProperty wateringCanProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        wateringCanProperty = (WateringCanProperty)baseProperty;
    }
    public override bool CanPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        return ItemSystem.IsInRange(position, wateringCanProperty.Range);
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        base.OnPrimaryAction(position);
        WaterRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void WaterRpc(Vector2 position)
    {
        var hits = ItemSystem.OverlapAreaAll(wateringCanProperty.Size, position, wateringCanProperty.WaterableLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IWaterable>(out var waterable))
            {
                waterable.GetWatered();
            }
        }
    }
}
