using UnityEngine;

// Referencing https://www.redblobgames.com/maps/terrain-from-noise/
[System.Serializable]
public struct ColorMapping      // Pairing color threshold
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
    private Vector2Int dimension = new Vector2Int(50, 50);      // Sampling grid
    [SerializeField]
    private float scale = 1.0f;     // Normal scale
    [SerializeField]
    private int octaves = 3;
    [SerializeField]
    private float persistence = 0.5f;       // Effects of each octave layer
    [SerializeField]                    // NOTE: problably change the name of this varable to 'frequencyIncrease' or 'frequencyMult' for better clarity
    private float frequencyBase = 2f;       // Frequency increase between octaves
    [SerializeField]
    private float exponent = 1f;        // Sharpens/flattens map

    public override void GenerateMap(Vector2Int mapSize)        // Generate perlin noise texture
    {
        var halfMapSize = mapSize / 2;
        mapTexture = new Texture2D(mapSize.x, mapSize.y);
        mapTexture.filterMode = FilterMode.Point;           // Sharpen pixels of map

        var mapColorLength = mapColors.Length;

        // Generate the map
        rawMap = new Offset2DArray<float>(-halfMapSize.x, halfMapSize.x, -halfMapSize.y, halfMapSize.y);        // Map Grid
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)        // Loop over each pixel on the map/grid
            {
                rawMap[x, y] = GetValue(x + halfMapSize.x, y + halfMapSize.y, mapSize);         // Get perlin noise value
                mapTexture.SetPixel(x + halfMapSize.x, y + halfMapSize.y, GetColor(rawMap[x, y]));      // Set pixel in texture
            }
        }

        TransformMap(mapSize);

        mapTexture.Apply();     // Copies changes you've made in a CPU texture to the GPU.
    }

    public override void RandomizeMap()
    {
        origin = new Vector2(Random.Range(-1000, 1000), Random.Range(-1000, 1000));
    }

    protected virtual void TransformMap(Vector2Int mapSize)
    {
        // Override this method to transform the map
    }

    protected virtual float GetValue(float x, float y, Vector2Int mapSize)      // Shift and prep coords for sampling
    {
        var halfMapSize = mapSize / 2;
        return GetNoise(x + halfMapSize.x, y + halfMapSize.y, origin, dimension, scale, octaves, persistence, frequencyBase, exponent);
    }

    private float GetNoise(float x, float y, Vector2 origin, Vector2 dimension,
        float scale, int octaves, float persistence, float frequencyBase, float exponent)
    {
        // Normalize coordinates then mult by scale; larger = smoother, smaller = finer
        float xCoord = origin.x + x / dimension.x * scale;
        float yCoord = origin.y + y / dimension.y * scale;

        var total = 0f;
        var frequency = 1f;
        var amplitude = 1f;
        var maxValue = 0f;
        for (int i = 0; i < octaves; i++)
        {
            // frequency = wavelength
            // amplitude = height
            // octave = different samples; layers
            total += Mathf.PerlinNoise(xCoord * frequency, yCoord * frequency) * amplitude;     // (Coord * frequency) * amplitude

            // octave change
            maxValue += amplitude;          // decrease amplitude
            amplitude *= persistence;
            frequency *= frequencyBase;     // increase frequency
        }

        return Mathf.Pow(total / maxValue, exponent);       // increase/decrease map's rate of change
    }

    // return the color who's value best reflects the noise value of a particular coordinate
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
