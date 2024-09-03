using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private TerrainProperty[] terrainProperties;
    [SerializeField]
    private GameObject terrainPrefab;
    [SerializeField]
    private Vector2Int dimension;

    [Header("Elevation Map")]
    [SerializeField]
    private Vector2 elevationMapOrigin;
    [SerializeField]
    private float elevationMapScale = 1.0f;
    [SerializeField]
    private int octaves = 3;
    [SerializeField]
    private float persistence = 0.5f;
    [SerializeField]
    private float frequencyBase = 2f;
    [SerializeField]
    private float exp = 1f;

    [ContextMenu("Generate")]
    public void Generate()
    {
        var startX = -dimension.x / 2;
        var startY = -dimension.y / 2;
        var endX = startX + dimension.x;
        var endY = startY + dimension.y;
        for (var x = startX; x < endX; x++)
        {
            for (var y = startY; y < endY; y++)
            {
                var unit = Instantiate(terrainPrefab, new Vector3(x, y), Quaternion.identity, transform);
                unit.GetComponent<TerrainUnit>().SetProperty(GetProperty(x, y));
            }
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    public TerrainProperty GetProperty(float x, float y)
    {
        TerrainProperty candidate = null;

        var noise = GetNoise(x, y, elevationMapOrigin, dimension,
                    elevationMapScale, octaves, persistence, frequencyBase, exp);

        foreach (var p in terrainProperties)
        {
            if (p.Match(noise)) candidate = p;
        }

        if (candidate == null)
        {
            candidate = terrainProperties[0];
            Debug.Log($"Cannot match property at {x}, {y}, noise = {noise}");
        }

        return candidate;
    }

    public float GetNoise(float x, float y, Vector2 origin, Vector2 dimension,
        float scale, int octaves, float persistence, float frequencyBase, float exp)
    {
        float xCoord = origin.x + x / dimension.x * scale;
        float yCoord = origin.y + y / dimension.y * scale;

        var total = 0f;
        var frequency = 1f;
        var amplitude = 1f;
        var maxValue = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(xCoord * frequency, yCoord * frequency) * amplitude;

            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= frequencyBase;
        }

        return Mathf.Pow(total / maxValue, exp);
    }
}
