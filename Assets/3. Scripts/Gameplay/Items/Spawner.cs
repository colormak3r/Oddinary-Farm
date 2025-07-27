using ColorMak3r.Utility;
using Unity.Netcode;
using UnityEngine;

public class Spawner : Tool
{
    private SpawnerProperty spawnerProperty;
    public SpawnerProperty SpawnerProperty => spawnerProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        spawnerProperty = (SpawnerProperty)baseProperty;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid(spawnerProperty.Size);
        if (!ItemSystem.IsInRange(position, spawnerProperty.Range))
        {
            if (showDebug) Debug.Log($"Cannot spawn {spawnerProperty.ItemName} at {position}, out of range");
            return false;
        }

        var invalid = ItemSystem.OverlapArea(position, spawnerProperty.Size, spawnerProperty.InvalidLayers);
        if (invalid != null)
        {
            if (showDebug) Debug.Log($"Cannot spawn {spawnerProperty.ItemName} at {position}, {invalid.name} is blocking");
            return false;
        }

        return true;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid(spawnerProperty.Size);
        base.OnPrimaryAction(position);
        ItemSystem.Spawn(position, spawnerProperty);
    }
}
