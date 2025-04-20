using UnityEngine;

[System.Serializable]
public struct ColorMapping
{
    public Color32 color;
    public float value;

    public ColorMapping(Color32 color, float value)
    {
        this.color = color;
        this.value = value;
    }
}

public class PerlinNoiseGenerator : MapGenerator
{
    [Header("Map Settings")]
    [SerializeField]
    private Vector2 origin = new Vector2(1264, 234);
    [SerializeField]
    private Vector2Int dimension = new Vector2Int(50, 50);
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

    public override void GenerateMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        mapTexture = new Texture2D(mapSize.x, mapSize.y);
        mapTexture.filterMode = FilterMode.Point;

        var mapColorLength = mapColors.Length;

        // Generate the map
        rawMap = new Offset2DArray<float>(-halfMapSize.x, halfMapSize.x, -halfMapSize.y, halfMapSize.y);
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                rawMap[x, y] = GetValue(x + halfMapSize.x, y + halfMapSize.y, mapSize);
                mapTexture.SetPixel(x + halfMapSize.x, y + halfMapSize.y, GetColor(rawMap[x, y]));
            }
        }

        TransformMap(mapSize);

        mapTexture.Apply();
    }

    public override void RandomizeMap()
    {
        origin = new Vector2(Random.Range(-1000, 1000), Random.Range(-1000, 1000));
    }

    protected virtual void TransformMap(Vector2Int mapSize)
    {
        // Override this method to transform the map
    }

    protected virtual float GetValue(float x, float y, Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        return GetNoise(x + halfMapSize.x, y + halfMapSize.y, origin, dimension, scale, octaves, persistence, frequencyBase, exponent);
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

    protected Color GetColor(float noiseValue)
    {
        Color32 selectedColor = mapColors[0].color;

        // Find the correct color mapping
        for (int i = 0; i < mapColors.Length; i++)
        {
            if (noiseValue <= mapColors[i].value)
            {
                selectedColor = mapColors[i].color;
                break;
            }
        }
        return selectedColor;
    }
}
