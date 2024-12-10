using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Tool : Item
{
    private ToolProperty toolProperty;
    public ToolProperty ToolProperty => toolProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        toolProperty = (ToolProperty)newValue;
    }

    protected void ClearFolliage(Vector2 position)
    {
        ClearFolliageRpc(position);
    }

    [Rpc(SendTo.Everyone)]
    private void ClearFolliageRpc(Vector2 position)
    {
        position = position.SnapToGrid();

        var collider = Physics2D.OverlapPoint(position, toolProperty.TerrainLayer);
        if (collider && collider.TryGetComponent<TerrainUnit>(out var terrainUnit))
        {
            terrainUnit.BreakFolliage();
            if (IsServer)
            {
                WorldGenerator.Main.InvalidateFolliagePositionOnServer(position);
            }
        }
    }
}
