using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;

public class Seed : Spawner
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

    protected override void OnSpawn(GameObject gameObject)
    {
        base.OnSpawn(gameObject);
        var plant = gameObject.GetComponent<Plant>();
        plant.Initialize(seedProperty.PlantProperty);
    }
}
