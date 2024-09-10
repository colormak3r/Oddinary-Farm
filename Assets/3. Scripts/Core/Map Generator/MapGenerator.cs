using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;
using UnityEngine.UIElements;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Main;

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
        public List<GameObject> blocks;

        public Chunk(Vector2 position, int size)
        {
            this.position = position;
            blocks = new List<GameObject>();
        }

        public void Destroy()
        {
            while (blocks.Count > 0)
            {
                var obj = blocks[0];
                blocks.RemoveAt(0);
                UnityEngine.Object.Destroy(obj);
            }
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

        for (int i = -(renderDistance + renderXOffset); i < renderDistance + 1 + renderXOffset; i++)
        {
            for (int j = -renderDistance + 1; j < renderDistance; j++)
            {
                var chunkPos = new Vector2(closetChunkPosition.x + i * chunkSize, closetChunkPosition.y + j * chunkSize);

                if (!positionToChunk.ContainsKey(chunkPos))
                {
                    yield return GenerateChunk(chunkPos, chunkSize, positionToChunk);
                }
            }
        }

        yield return RemoveExcessChunks(closetChunkPosition);

        isGenerating = false;
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
                var property = GetProperty(pos.x,pos.y);
                var terrainObj = Instantiate(terrainPrefab, pos, Quaternion.identity, transform);
                terrainObj.GetComponent<TerrainUnit>().Initialize(property);
                chunk.blocks.Add(terrainObj);
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
            positionToChunk[pos].Destroy();
            positionToChunk.Remove(pos);
        }

        yield return null;
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
