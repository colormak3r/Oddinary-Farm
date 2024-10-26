 using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Spawner : Item
{
    private SpawnerProperty spawnerProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        spawnerProperty = (SpawnerProperty) newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        return IsInRange(position);
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        SpawnRpc(position);
    }

    [Rpc(SendTo.Server)]
    public void SpawnRpc(Vector2 position)
    {
        if((position - (Vector2)transform.position).magnitude > spawnerProperty.Range) 
            position = (Vector2)transform.position + (position - (Vector2)transform.position).normalized * spawnerProperty.Range;

        var gameobject = Instantiate(spawnerProperty.PrefabToSpawn, position, Quaternion.identity);
        gameobject.GetComponent<NetworkObject>().Spawn();
    }

    protected virtual void OnSpawn(GameObject gameObject)
    {
        // Initialize the gameobject here
    }
}
