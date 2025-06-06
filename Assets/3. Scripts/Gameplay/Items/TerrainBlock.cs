using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TerrainBlock : Item
{
    private TerrainBlockProperty terrainBlockProperty;
    private WorldGenerator worldGenerator;

    private void Start()
    {
        worldGenerator = WorldGenerator.Main;
    }

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        terrainBlockProperty = (TerrainBlockProperty)baseProperty;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        if (!ItemSystem.IsInRange(position, terrainBlockProperty.Range)) return false;

        position = position.SnapToGrid();
        var hit = Physics2D.OverlapPoint(position, terrainBlockProperty.PlaceableLayer);
        if (hit && hit.TryGetComponent(out TerrainUnit terrainUnit))
        {
            if (terrainUnit.Property.IsAccessible)
            {
                if (showDebug) Debug.Log($"Terrain Block at {position} is accessible", hit?.gameObject);
                return false;
            }
            else
            {
                return true;
            }

        }
        else
        {
            if (showDebug) Debug.Log($"No Terrain Block at {position}, hit = {hit?.gameObject.name}", hit?.gameObject);
            return false;
        }
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        position = position.SnapToGrid();
        TerrainBlockPrimaryRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void TerrainBlockPrimaryRpc(Vector2 position)
    {
        position = position.SnapToGrid();
        //worldGenerator.PlaceBlock(position, terrainBlockProperty.UnitProperty);
    }
}
