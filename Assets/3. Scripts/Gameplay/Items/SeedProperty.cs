using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Seed")]
public class SeedProperty : ItemProperty
{
    [Header("Seed Property")]
    [SerializeField]
    private GameObject plantPrefab;
    [SerializeField]
    private PlantProperty plantProperty;
    [SerializeField]
    private LayerMask farmPlotLayer;
    [SerializeField]
    private LayerMask plantLayer;

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        base.OnPrimaryAction(position, inventory);

        position = position.SnapToGrid();
        var farmPlotHits = Physics2D.OverlapPointAll(position, farmPlotLayer);
        if (farmPlotHits.Length > 0)
        {
            var plantHits = Physics2D.OverlapPointAll(position, plantLayer);
            if (plantHits.Length > 0)
            {
                Debug.Log("A plant already exist");
                return false;
            }
            else
            {
                SpawnPlant(position);
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
    private void SpawnPlant(Vector2 position)
    {
        GameObject go = Instantiate(plantPrefab, position.SnapToGrid(), Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var plant = go.GetComponent<Plant>();
        plant.Initialize(plantProperty);
    }
}
