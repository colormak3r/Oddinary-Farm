using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;
using UnityEngine.UIElements;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(Main.gameObject);
    }

    private class Chunk
    {
        public Vector2 position;
        public int size;
        public List<GameObject> terrainUnits;

        public Chunk(Vector2 position, int size)
        {
            this.position = position;
            terrainUnits = new List<GameObject>();
        }
    }

    [Header("Settings")]
    [SerializeField]
    private TerrainProperty[] terrainProperties;
    [SerializeField]
    private GameObject terrainPrefab;

    [Header("Map Settings")]
    [SerializeField]
    private int chunkSize = 4;
    [SerializeField]
    private int renderDistance = 3;
    [SerializeField]
    private int renderXOffset = 4;
    [SerializeField]
    private float starterShaping = 0.5f;
    [SerializeField]
    private Vector2Int starterArea = new Vector2Int(50, 50);
    [SerializeField]
    private Vector2Int starterBound = new Vector2Int(100, 100);


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
    private Vector2Int dimension;
    [SerializeField]
    private float exp = 1f;

    [Header("Debug")]
    [SerializeField]
    private bool isGenerating;

    private Dictionary<Vector2, Chunk> positionToChunk = new Dictionary<Vector2, Chunk>();

    private Vector2 closetChunkPosition_cached = Vector2.one;

    private LocalObjectPooling localObjectPooling;

    private void Start()
    {
        localObjectPooling = LocalObjectPooling.Main;
    }

    public IEnumerator GenerateTerrainCoroutine(Vector2 position)
    {
        position = position.SnapToGrid();

        var closetChunkPosition = position.SnapToGrid(chunkSize);
        if (closetChunkPosition_cached != closetChunkPosition)
            closetChunkPosition_cached = closetChunkPosition;
        else
            yield break;

        if (isGenerating) yield break;
        isGenerating = true;

        yield return GenerateChunks(closetChunkPosition);

        yield return RemoveExcessChunks(closetChunkPosition);

        isGenerating = false;
    }

    private IEnumerator GenerateChunks(Vector2 closetChunkPosition)
    {
        for (int i = -(renderDistance + renderXOffset); i < renderDistance + 1 + renderXOffset; i++)
        {
            for (int j = -renderDistance + 1; j < renderDistance; j++)
            {
                var chunkPos = new Vector2(closetChunkPosition.x + i * chunkSize, closetChunkPosition.y + j * chunkSize);

                if (!positionToChunk.ContainsKey(chunkPos))
                {
                    StartCoroutine(GenerateChunk(chunkPos, chunkSize, positionToChunk));
                }
            }
        }
        yield return null;
    }

    private IEnumerator GenerateChunk(Vector2 position, int chunkSize, Dictionary<Vector2, Chunk> positionToChunk)
    {
        Chunk chunk = new Chunk(position, chunkSize);
        int halfChunkSize = chunkSize / 2;
        int lowerLimit = -halfChunkSize;
        int upperLimit = (chunkSize % 2 == 0) ? halfChunkSize : halfChunkSize + 1;

        for (int i = lowerLimit; i < upperLimit; i++)
        {
            for (int j = lowerLimit; j < upperLimit; j++)
            {
                var pos = new Vector2(position.x + i, position.y + j);
                var property = GetProperty(pos.x, pos.y);
                var terrainObj = localObjectPooling.Spawn(terrainPrefab);
                terrainObj.transform.position = pos;
                terrainObj.transform.parent = transform;
                terrainObj.GetComponent<TerrainUnit>().Initialize(property);
                chunk.terrainUnits.Add(terrainObj);
            }
        }
        yield return null;

        positionToChunk.Add(position, chunk);
    }

    private IEnumerator RemoveExcessChunks(Vector2 closetChunkPosition)
    {
        List<Vector2> positionsToRemove = new List<Vector2>();

        // Determine the bounds of the loop
        int minX = (int)closetChunkPosition.x - (renderDistance + renderXOffset) * chunkSize;
        int maxX = (int)closetChunkPosition.x + (renderDistance + 1 + renderXOffset) * chunkSize;
        int minY = (int)closetChunkPosition.y - renderDistance * chunkSize;
        int maxY = (int)closetChunkPosition.y + renderDistance * chunkSize;

        // Iterate over the dictionary to find chunks outside the bounds
        foreach (var entry in positionToChunk)
        {
            Vector2 pos = entry.Key;
            if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
            {
                positionsToRemove.Add(pos);
            }
        }

        // Remove the identified chunks
        foreach (var pos in positionsToRemove)
        {
            foreach (var terrainUnit in positionToChunk[pos].terrainUnits)
            {
                localObjectPooling.Despawn(terrainUnit);
            }

            positionToChunk.Remove(pos);
        }

        yield return null;
    }

    public TerrainProperty GetProperty(float x, float y)
    {
        TerrainProperty candidate = null;
        var noise = GetNoise(x, y, elevationMapOrigin, dimension,
                    elevationMapScale, octaves, persistence, frequencyBase, exp);

        // Check if the coordinates are within the starter boundary
        if (x > -starterBound.x && x < starterBound.x && y > -starterBound.y && y < starterBound.y)
        {
            // Normalize x and y to the range [-1, 1] within the starter boundary
            float normalizedBoundaryX = x / starterBound.x;
            float normalizedBoundaryY = y / starterBound.y;

            // Apply a fall-off function to create a smooth transition at the boundary
            float exponent = 2f; // Adjust this exponent to control the fall-off rate
            float falloffBoundaryX = 1 - Mathf.Pow(Mathf.Abs(normalizedBoundaryX), exponent);
            float falloffBoundaryY = 1 - Mathf.Pow(Mathf.Abs(normalizedBoundaryY), exponent);
            float boundaryFalloff = Mathf.Clamp01(falloffBoundaryX * falloffBoundaryY);

            // Determine if additional shaping should be applied based on the fall-off
            bool applyShaping = Mathf.Lerp(noise, boundaryFalloff, starterShaping) > 0.5f;

            if (applyShaping)
            {
                // Normalize x and y to the range [-1, 1] within the starter area
                float normalizedAreaX = x / starterArea.x;
                float normalizedAreaY = y / starterArea.y;

                // Apply a fall-off function to create a smooth transition within the starter area
                float falloffAreaX = 1 - Mathf.Pow(Mathf.Abs(normalizedAreaX), exponent);
                float falloffAreaY = 1 - Mathf.Pow(Mathf.Abs(normalizedAreaY), exponent);
                float areaFalloff = Mathf.Clamp01(falloffAreaX * falloffAreaY);

                // Blend the base noise with the area fall-off using the starterShaping factor
                noise = Mathf.Lerp(noise, areaFalloff, starterShaping);
            }
        }

        foreach (var property in terrainProperties)
        {
            if (property.Match(noise)) candidate = property;
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
