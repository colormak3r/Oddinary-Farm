using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;

public class Seed : Item
{
    private SeedProperty property;

    public override void OnNetworkSpawn()
    {
        HandleOnPropertyChanged(null, PropertyValue);
        Property.OnValueChanged += HandleOnPropertyChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandleOnPropertyChanged;
    }

    private void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        property = (SeedProperty)newValue;
    }


    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        base.OnPrimaryAction(position, inventory);

        position = position.SnapToGrid();
        var farmPlotHits = Physics2D.OverlapPointAll(position, property.FarmPlotLayer);
        if (farmPlotHits.Length > 0)
        {
            var plantHits = Physics2D.OverlapPointAll(position, property.PlantLayer);
            if (plantHits.Length > 0)
            {
                Debug.Log("A plant already exist");
                return false;
            }
            else
            {
                SpawnPlantRpc(position);
                return true;
            }
        }
        else
        {
            Debug.Log("Need Farm Plot, use the Hoe to create Farm Plot at this location");
            return false;
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlantRpc(Vector2 position)
    {
        GameObject go = Instantiate(property.PlantPrefab, position.SnapToGrid(), Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var plant = go.GetComponent<Plant>();
        plant.Initialize(property.PlantProperty);
    }
}
