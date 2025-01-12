using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Spawner : Tool
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
        position = position.SnapToGrid(spawnerProperty.Size);
        if (!IsInRange(position))
        {
            if (showDebug) Debug.Log($"Cannot spawn {spawnerProperty.Name} at {position}, out of range");
            return false;
        }

        var invalid = OverlapArea(spawnerProperty.Size, position, spawnerProperty.InvalidLayers);
        if (invalid != null)
        {
            if (showDebug) Debug.Log($"Cannot spawn {spawnerProperty.Name} at {position}, {invalid.name} is blocking");
            return false;
        }

        return true;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid(spawnerProperty.Size);
        base.OnPrimaryAction(position);
        SpawnRpc(position);
    }

    [Rpc(SendTo.Server)]
    public void SpawnRpc(Vector2 position)
    {
        var gameobject = Instantiate(spawnerProperty.PrefabToSpawn, position + spawnerProperty.SpawnOffset, Quaternion.identity);
        gameobject.GetComponent<NetworkObject>().Spawn();
        OnSpawn(gameobject);
    }

    protected virtual void OnSpawn(GameObject gameObject)
    {
        // Initialize the gameobject here
    }
}
