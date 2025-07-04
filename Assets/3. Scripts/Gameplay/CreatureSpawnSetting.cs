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
    public bool headToHeadCenter;
    public CreatureSpawn[] creatureSpawns;
}

[System.Serializable]
public struct CreatureSpawn
{
    public GameObject creaturePrefab;
    public int spawnCount;
}

[CreateAssetMenu(fileName = "CreatureSpawnSetting", menuName = "Scriptable Objects/Creature Spawn Setting", order = 1)]
public class CreatureSpawnSetting : ScriptableObject
{
    public CreatureDay[] creatureDays;
}
