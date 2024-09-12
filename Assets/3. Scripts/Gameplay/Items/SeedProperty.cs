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

    public override void OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        base.OnPrimaryAction(position, inventory);
        SpawnPlant(position);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlant(Vector2 position)
    {
        GameObject go = Instantiate(plantPrefab, position.SnapToGrid(), Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var plant = go.GetComponent<Plant>();
        plant.MockPropertyChange();
    }
}
