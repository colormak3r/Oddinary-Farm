using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hammer : Tool
{
    private HammerProperty hammerProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        hammerProperty = (HammerProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        if (!IsInRange(position)) return false;

        var invalid = Physics2D.OverlapPoint(position, hammerProperty.InvalidLayers);
        if (invalid) 
        { 
            if(showDebug) Debug.Log($"Cannot hammer at {position}, {invalid.name} is blocking");
            return false; 
        }

        var terrainHit = Physics2D.OverlapPoint(position, hammerProperty.TerrainLayers);
        if (terrainHit && terrainHit.TryGetComponent(out TerrainUnit terrain))
        {
            if (terrain.Property.IsAccessible)
            {
                return true;
            }
            else
            {
                if (showDebug) Debug.Log($"Cannot hammer at {position}, {terrain.name} is not accessible");
                return false;
            }
        }
        else
        {
            Debug.Log($"Cannot hammer at {position}, no terrain found");
            return false;
        }
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        HammerPrimaryRpc(position);
    }

    public override bool CanSecondaryAction(Vector2 position)
    {
        return IsInRange(position);
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        base.OnSecondaryAction(position);
        HammerSecondaryRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void HammerPrimaryRpc(Vector2 position)
    {
        position = position.SnapToGrid() - TransformUtility.HALF_UNIT_Y_V2;
        GameObject go = Instantiate(hammerProperty.Structures[0], position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();        
    }

    [Rpc(SendTo.Server)]
    private void HammerSecondaryRpc(Vector2 position)
    {
        position = position.SnapToGrid();
        var hit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayers);
        if (hit && hit.TryGetComponent(out Structure structure))
        {
            structure.Remove();
        }
    }
}
