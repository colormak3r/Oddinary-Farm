using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Spawner : Item
{
    private SpawnerProperty spawnerProperty;
    public SpawnerProperty SpawnerProperty => spawnerProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        spawnerProperty = (SpawnerProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        return IsInRange(position);
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        base.OnPrimaryAction(position);
        SpawnRpc(position);
    }

    [Rpc(SendTo.Server)]
    public void SpawnRpc(Vector2 position)
    {
        var gameobject = Instantiate(spawnerProperty.PrefabToSpawn, position, Quaternion.identity);
        gameobject.GetComponent<NetworkObject>().Spawn();
        OnSpawn(gameobject);
    }

    protected virtual void OnSpawn(GameObject gameObject)
    {
        // Initialize the gameobject here
    }
}
