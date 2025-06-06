using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PrefabPosition
{
    public GameObject Prefab;
    public Vector2 Position;

    public PrefabPosition(GameObject prefab, Vector2 position)
    {
        Prefab = prefab;
        Position = position;
    }
}

[System.Serializable]
public struct SpawnerPosition
{
    public SpawnerProperty SpawnerProperty;
    public Vector2 Position;
    public Vector2 Offset;

    public SpawnerPosition(SpawnerProperty spawnerProperty, Vector2 position, Vector2 offset)
    {
        SpawnerProperty = spawnerProperty;
        Position = position;
        Offset = offset;
    }
}

[CreateAssetMenu(fileName = "AssetSpawnPreset", menuName = "Scriptable Objects/Asset Spawn Preset", order = 1)]
public class AssetSpawnPreset : ScriptableObject
{
    [SerializeField]
    private uint startingWallet = 10;
    public uint StartingWallet => startingWallet;
    [SerializeField]
    private ItemProperty[] startingItems;
    public ItemProperty[] StartingItems => startingItems;

    public List<PrefabPosition> PrefabPositions;
    public List<SpawnerPosition> SpawnerPositions;
}