using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hoe : Item
{
    private HoeProperty hoeProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        hoeProperty = (HoeProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        if (!IsInRange(position)) return false;

        position = position.SnapToGrid();

        var invalid = Physics2D.OverlapPoint(position, hoeProperty.UnhoeableLayer);
        if (invalid)
        {
            if(showDebug) Debug.Log($"Cannot hoe at {position}, {invalid.name} is blocking");
            return false;
        }

        var terrainHit = Physics2D.OverlapPoint(position, hoeProperty.HoeableLayer);
        if (terrainHit && terrainHit.TryGetComponent<TerrainUnit>(out var terrain))
        {
            if (terrain.Property.IsAccessible)
            {
                if (showDebug) Debug.Log($"Hoe success at {position}");
                return true;
            }
            else
            {
                if (showDebug) Debug.Log($"Cannot hoe at {position}, {terrain.name} is not accessible");
                return false;
            }
        }

        return false;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        
        HoeRpc(position);
    }


    [Rpc(SendTo.Server)]
    private void HoeRpc(Vector2 position)
    {
        position = position.SnapToGrid() - TransformUtility.HALF_UNIT_Y_V2;
        GameObject go = Instantiate(AssetManager.Main.FarmPlotPrefab, position , Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }
}
