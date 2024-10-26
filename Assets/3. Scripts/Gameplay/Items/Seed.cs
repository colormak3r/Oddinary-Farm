using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;

public class Seed : Item
{
    private SeedProperty seedProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        seedProperty = (SeedProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        if (!IsInRange(position)) return false;

        position = position.SnapToGrid();
        var farmPlotHits = Physics2D.OverlapPointAll(position, seedProperty.FarmPlotLayer);
        if (farmPlotHits.Length > 0)
        {
            var plantHits = Physics2D.OverlapPointAll(position, seedProperty.PlantLayer);
            if (plantHits.Length > 0)
            {
                if(showDebug) Debug.Log($"A plant already exist at {position}");
                return false;
            }
            else
            {
                if (showDebug) Debug.Log($"A crop is created at {position}");
                return true;
            }
        }
        else
        {
            if (showDebug) Debug.Log($"Need Farm Plot, use the Hoe to create Farm Plot at {position}");
            return false;
        }
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        SpawnPlantRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlantRpc(Vector2 position)
    {
        GameObject go = Instantiate(seedProperty.PlantPrefab, position.SnapToGrid(), Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var plant = go.GetComponent<Plant>();
        plant.Initialize(seedProperty.PlantProperty);
    }
}
