using ColorMak3r.Utility;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PrefabChance
{
    public GameObject prefab;
    public float chance;
    public int maxCount;
}

[CreateAssetMenu(fileName = " ResourceProperty", menuName = "Scriptable Objects/Resource Property")]
public class ResourceProperty : ScriptableObject
{
    [SerializeField]
    private PrefabChance[] prefabs;
    public PrefabChance[] Prefabs => prefabs;
    [SerializeField]
    private MinMaxFloat elevation;
    public MinMaxFloat Elevation => elevation;
    [SerializeField]
    private MinMaxFloat moisture;
    public MinMaxFloat Moisture => moisture;
    [SerializeField]
    private MinMaxFloat resource;
    public MinMaxFloat Resource => resource;

    public bool Match(float elevation, float moisture, float resource, out GameObject prefab, out int maxCount)
    {
        if (Elevation.IsZero() || Elevation.IsInRange(elevation)
            && (Moisture.IsZero() || Moisture.IsInRange(moisture))
            && (Resource.IsZero() || Resource.IsInRange(resource)))
        {
            var value = GetRandomPrefab(Prefabs);
            prefab = value.Prefab;
            maxCount = value.MaxCount;
            return true;
        }
        else
        {
            prefab = null;
            maxCount = 0;
            return false;
        }
    }

    public GameObject GetNot(GameObject notThisPrefab)
    {
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (var prefabChance in Prefabs)
        {
            if (prefabChance.prefab != notThisPrefab)
            {
                validPrefabs.Add(prefabChance.prefab);
            }
        }

        return validPrefabs.GetRandomElement();
    }

    private (GameObject Prefab, int MaxCount) GetRandomPrefab(PrefabChance[] chances)
    {
        if (chances == null || chances.Length == 0)
            return (null, 0);

        float totalChance = 0f;
        foreach (var item in chances)
            totalChance += item.chance;

        float randomValue = Random.Range(0f, totalChance);

        float cumulative = 0f;
        foreach (var item in chances)
        {
            cumulative += item.chance;
            if (randomValue <= cumulative)
                return (item.prefab, item.maxCount);
        }

        var fallback = chances[chances.Length - 1];
        return (fallback.prefab, fallback.maxCount); // fallback
    }
}