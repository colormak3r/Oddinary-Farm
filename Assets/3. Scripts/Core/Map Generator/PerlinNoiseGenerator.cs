using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TerrainUtils;


public class PerlinNoiseGenerator : MapGenerator
{
    [Header("Map Settings")]
    [SerializeField]
    private Vector2 origin = new Vector2(1264, 234);
    [SerializeField]
    private Vector2Int dimension;
    [SerializeField]
    private float scale = 1.0f;
    [SerializeField]
    private int octaves = 3;
    [SerializeField]
    private float persistence = 0.5f;
    [SerializeField]
    private float frequencyBase = 2f;
    [SerializeField]
    private float exponent = 1f;

    Vector2Int halfMapSize;

    public IEnumerator Initialize(Vector2Int mapSize)
    {
        yield return GenerateMap(mapSize);
        yield return BuildMap(mapSize);
    }

    protected override IEnumerator GenerateMap(Vector2Int mapSize)
    {
        halfMapSize = mapSize / 2;

        // Generate the map
        rawMap = new Offset2DArray<float>(-halfMapSize.x, halfMapSize.x, -halfMapSize.y, halfMapSize.y);
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                rawMap[x, y] = GetNoise(x + halfMapSize.x, y + halfMapSize.y, origin, dimension, scale, octaves, persistence, frequencyBase, exponent);
            }
        }

        yield return GenerateMapExtension(mapSize);
    }

    protected virtual IEnumerator GenerateMapExtension(Vector2Int mapSize)
    {
        yield return null;
    }

    protected virtual IEnumerator BuildMap(Vector2Int mapSize)
    {
        yield return null;
    }

    private float GetNoise(float x, float y, Vector2 origin, Vector2 dimension,
        float scale, int octaves, float persistence, float frequencyBase, float exponent)
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

        return Mathf.Pow(total / maxValue, exponent);
    }
}
