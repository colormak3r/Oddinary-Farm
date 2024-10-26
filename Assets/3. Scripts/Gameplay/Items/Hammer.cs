using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hammer : Item
{
    private HammerProperty hammerProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        hammerProperty = (HammerProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        return IsInRange(position);
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        HammerPrimaryRpc(position);
    }

    public override bool CanSecondaryAction(Vector2 position)
    {
        return IsInRange(position);
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        HammerSecondaryRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void HammerPrimaryRpc(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, hammerProperty.Radius, hammerProperty.InvalidLayers);
        if (hits.Length > 0) return;

        var hit = Physics2D.OverlapPoint(position, hammerProperty.TerrainLayers);
        if (hit && hit.TryGetComponent(out TerrainUnit t))
        {
            if (t.Property.IsAccessible)
            {
                GameObject go = Instantiate(hammerProperty.Structures[0], position, Quaternion.identity);
                go.GetComponent<NetworkObject>().Spawn();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void HammerSecondaryRpc(Vector2 position)
    {
        var hit = Physics2D.OverlapCircle(position, hammerProperty.Radius, hammerProperty.StructureLayers);
        if (hit && hit.TryGetComponent(out Structure structure))
        {
            structure.Remove();
        }
    }
}
