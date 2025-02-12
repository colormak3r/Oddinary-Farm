using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


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

    public IEnumerator Initialize(Vector2Int mapSize)
    {
        yield return GenerateMap(mapSize);
        yield return BuildMap(mapSize);
    }

    protected override IEnumerator GenerateMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        map = new float[mapSize.x, mapSize.y];
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                map[i, j] = GetNoise(i - halfMapSize.x, j - halfMapSize.y, origin, dimension, scale, octaves, persistence, frequencyBase, exponent);
            }
        }

        yield return null;
    }

    protected virtual IEnumerator BuildMap(Vector2Int mapSize)
    {
        yield return null;
    }

    public float GetValueNormalized(int x, int y, Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        var i = x + halfMapSize.x;
        var j = y + halfMapSize.y;

        return map[i, j];
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
