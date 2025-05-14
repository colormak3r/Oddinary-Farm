using UnityEngine;

[System.Serializable]
public struct CreatureDay
{
    public CreatureWave[] creatureWaves;
}

[System.Serializable]
public struct CreatureWave
{
    [Range(0, 23)]
    public int spawnHour;
    public bool showWarning;
    public CreatureSpawn[] creatureSpawns;
}

[System.Serializable]
public struct CreatureSpawn
{
    public GameObject creaturePrefab;
    [Range(1, 100)]
    public int spawnCount;
}

[CreateAssetMenu(fileName = "CreatureSpawnSetting", menuName = "Scriptable Objects/Creature Spawn Setting", order = 1)]
public class CreatureSpawnSetting : ScriptableObject
{
    public CreatureDay[] creatureDays;
}
