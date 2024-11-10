using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Shovel : Item
{
    private ShovelProperty shovelProperty;
    private WorldGenerator worldGenerator;

    private void Start()
    {
        worldGenerator = WorldGenerator.Main;
    }

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        shovelProperty = (ShovelProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        if (!IsInRange(position)) return false;

        position = position.SnapToGrid();

        var invalid = Physics2D.OverlapPoint(position, shovelProperty.UndiggableLayer);
        if (invalid)
        {
            if (showDebug) Debug.Log($"Not Diggable at {position}, invalid terrain unit", invalid.gameObject);
            return false;
        }

        var hit = Physics2D.OverlapPoint(position, shovelProperty.DiggableLayer);
        if (hit && hit.TryGetComponent(out TerrainUnit terrainUnit))
        {
            if (terrainUnit.Property.IsAccessible)
            {
                return true;
            }
            else
            {
                if (showDebug) Debug.Log($"Not Diggable at {position}, terrain unit is not accessible", hit?.gameObject);
                return false;
            }
            
        }
        else
        {
            if (showDebug) Debug.Log($"Not Diggable at {position}, hit = {hit?.gameObject.name}", hit?.gameObject);
            return false;
        }
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        ShovelPrimaryRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void ShovelPrimaryRpc(Vector2 position)
    {
        position = position.SnapToGrid();

        // Get the terrain unit
        var hit = Physics2D.OverlapPoint(position, shovelProperty.DiggableLayer);
        if (!hit)
        {
            if (showDebug) Debug.Log($"No terrain unit found at {position} on Server");
            return;
        }

        // Spawn the item replica
        GameObject go = Instantiate(AssetManager.Main.ItemReplicaPrefab, position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var item = go.GetComponent<ItemReplica>();
        item.SetProperty(hit.GetComponent<TerrainUnit>().Property.BlockProperty);

        // Destroy the terrain unit
        worldGenerator.RemoveBlock(position);
    }
}
