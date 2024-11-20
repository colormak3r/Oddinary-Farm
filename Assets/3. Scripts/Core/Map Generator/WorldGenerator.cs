using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using static UnityEditor.PlayerSettings;


public class WorldGenerator : NetworkBehaviour
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
        public Transform transform;
        public List<GameObject> terrainUnits;

        public Chunk(Vector2 position, int size, Transform transform)
        {
            this.position = position;
            terrainUnits = new List<GameObject>();
            this.transform = transform;
        }

        public GameObject GetUnit(Vector2 position)
        {
            var debug = $"Chunk position = {this.position}, size = {size}, query = {position}";
            foreach (var unit in terrainUnits)
            {
                debug += $"\n   unit = {(Vector2)unit.transform.position}, {(Vector2)unit.transform.position == position}";
                if ((Vector2)unit.transform.position == position)
                {
                    return unit;
                }
            }
            Debug.Log(debug);
            return null;
        }
    }

    [Header("Terrain Settings")]
    [SerializeField]
    private TerrainUnitProperty[] terrainUnitProperties;
    [SerializeField]
    private GameObject terrainUnitPrefab;

    [Header("Generation Settings")]
    [SerializeField]
    private int chunkSize = 5;
    [SerializeField]
    private int renderDistance = 3;
    [SerializeField]
    private int renderXOffset = 4;

    [Header("Map Settings")]
    [SerializeField]
    private Vector2Int mapSize = new Vector2Int(500, 500);
    [SerializeField]
    private TerrainUnitProperty voidUnitProperty;

    [Header("Starter Area Settings")]
    [SerializeField]
    private float starterShaping = 0.5f;
    [SerializeField]
    private Vector2Int starterArea = new Vector2Int(50, 50);
    [SerializeField]
    private Vector2Int starterBound = new Vector2Int(100, 100);

    [Header("Elevation Map Settings")]
    [SerializeField]
    private Vector2 elevationOrigin = new Vector2(1264, 234);
    [SerializeField]
    private Vector2Int elevationDimension;
    [SerializeField]
    private float elevationScale = 1.0f;
    [SerializeField]
    private int elevationOctaves = 3;
    [SerializeField]
    private float elevationPersistence = 0.5f;
    [SerializeField]
    private float elevationFrequencyBase = 2f;
    [SerializeField]
    private float elevationExp = 1f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebug;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private bool isGenerating;
    public bool IsGenerating => isGenerating;

    private Dictionary<Vector2, Chunk> positionToChunk = new Dictionary<Vector2, Chunk>();

    private Vector2 closetChunkPosition_cached = Vector2.one;

    private LocalObjectPooling localObjectPooling;

    private TerrainUnitProperty[,] terrainMap;
    int halfMapSizeX;
    int halfMapSizeY;

    private Sprite terrainMapSprite;

    private void Start()
    {
        localObjectPooling = LocalObjectPooling.Main;
        GenerateMap();
    }

    [ContextMenu("Generate Map")]
    private void GenerateMap()
    {
        var terrainMapTexture = new Texture2D(mapSize.x, mapSize.y);

        halfMapSizeX = mapSize.x / 2;
        halfMapSizeY = mapSize.y / 2;
        terrainMap = new TerrainUnitProperty[mapSize.x, mapSize.y];
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                terrainMap[i, j] = GetDefaultProperty(i - halfMapSizeX, j - halfMapSizeY);
                terrainMapTexture.SetPixel(i, j, terrainMap[i, j].MapColor);
            }
        }

        terrainMapTexture.Apply();
        terrainMapSprite = Sprite.Create(terrainMapTexture, new Rect(0, 0, mapSize.x, mapSize.y), Vector2.zero);
        terrainMapSprite.texture.filterMode = FilterMode.Point;

        MapUI.Main.UpdateMap(terrainMapSprite);
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

        if (showDebug) Debug.Log("Child count = " + transform.childCount);
    }

    private IEnumerator GenerateChunks(Vector2 closetChunkPosition)
    {
        var coroutines = new List<Coroutine>();
        for (int i = -(renderDistance + renderXOffset); i < renderDistance + 1 + renderXOffset; i++)
        {
            for (int j = -renderDistance + 1; j < renderDistance; j++)
            {
                var chunkPos = new Vector2(closetChunkPosition.x + i * chunkSize, closetChunkPosition.y + j * chunkSize);

                if (!positionToChunk.ContainsKey(chunkPos))
                {
                    coroutines.Add(StartCoroutine(GenerateChunk(chunkPos, chunkSize, positionToChunk)));
                    yield return null;
                }
            }
        }

        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

    private IEnumerator GenerateChunk(Vector2 position, int chunkSize, Dictionary<Vector2, Chunk> positionToChunk)
    {
        var chunkObj = new GameObject("Chunk");
        chunkObj.transform.parent = transform;
        chunkObj.transform.position = position;
        Chunk chunk = new Chunk(position, chunkSize, chunkObj.transform);
        int halfChunkSize = chunkSize / 2;
        int lowerLimit = -halfChunkSize;
        int upperLimit = (chunkSize % 2 == 0) ? halfChunkSize : halfChunkSize + 1;

        for (int i = lowerLimit; i < upperLimit; i++)
        {
            for (int j = lowerLimit; j < upperLimit; j++)
            {
                var pos = new Vector2Int((int)position.x + i, (int)position.y + j);
                var property = GetMappedProperty(pos.x + halfMapSizeX, pos.y + halfMapSizeY);
                SpawnNewTerrainUnit(pos, chunk, property);
                yield return null;
            }
        }

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
        var coroutines = new List<Coroutine>();        
        foreach (var pos in positionsToRemove)
        {
            var chunk = positionToChunk[pos];
            coroutines.Add(StartCoroutine(RemoveChunk(chunk)));
            yield return null;
        }

        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

    private IEnumerator RemoveChunk(Chunk chunk)
    {
        foreach (var terrainUnit in chunk.terrainUnits)
        {
            localObjectPooling.Despawn(terrainUnit);
            yield return null;
        }
        
        positionToChunk.Remove(chunk.position);
        Destroy(chunk.transform.gameObject);
    }

    private TerrainUnitProperty GetMappedProperty(int i, int j)
    {
        //Debug.Log($"GetMappedProperty {x}, {y}");
        if (i >= mapSize.x || i < 0 || j >= mapSize.y || j < 0)
        {
            return voidUnitProperty;
        }
        else
        {
            try
            {
                return terrainMap[i, j];
            }
            catch (Exception e)
            {
                Debug.Log($"GetMappedProperty i = {i}, j = {j}");
                throw e;
            }
        }
    }

    public bool CanPlaceBlock(Vector2 position)
    {
        position = position.SnapToGrid();
        var i = (int)position.x + halfMapSizeX;
        var j = (int)position.y + halfMapSizeY;
        if (i >= mapSize.x || i < 0 || j >= mapSize.y || j < 0)
        {
            return false;
        }
        else
        {
            return !terrainMap[i, j].IsAccessible;
        }
    }

    public void PlaceBlock(Vector2 position, TerrainUnitProperty newProperty)
    {
        PlaceBlockRpc(position, newProperty);
    }

    [Rpc(SendTo.Everyone)]
    private void PlaceBlockRpc(Vector2 position, TerrainUnitProperty newProperty)
    {
        ReplaceMappedProperty(position, newProperty);
        ReplaceMappedUnit(position, newProperty);
    }

    private void ReplaceMappedProperty(Vector2 position, TerrainUnitProperty newProperty)
    {
        var pos = new Vector2Int((int)position.x, (int)position.y);
        var i = pos.x + halfMapSizeX;
        var j = pos.y + halfMapSizeY;
        terrainMap[i, j] = newProperty;
    }

    private void ReplaceMappedUnit(Vector2 position, TerrainUnitProperty newProperty)
    {
        var closetChunkPosition = position.SnapToGrid(chunkSize);

        if (!positionToChunk.ContainsKey(closetChunkPosition)) return;
        var chunk = positionToChunk[closetChunkPosition];
        var unit = chunk.GetUnit(position);

        // Remove the old unit
        localObjectPooling.Despawn(unit);
        chunk.terrainUnits.Remove(unit);

        // Spawn a new unit
        SpawnNewTerrainUnit(position, chunk, newProperty);
    }

    public void RemoveBlock(Vector2 position)
    {
        PlaceBlockRpc(position, voidUnitProperty);
    }

    private void SpawnNewTerrainUnit(Vector2 position, Chunk chunk, TerrainUnitProperty property)
    {
        var terrainObj = localObjectPooling.Spawn(terrainUnitPrefab);
        terrainObj.transform.position = position;
        terrainObj.transform.parent = chunk.transform;
        terrainObj.GetComponent<TerrainUnit>().Initialize(property);
        chunk.terrainUnits.Add(terrainObj);
    }

    private TerrainUnitProperty GetDefaultProperty(float x, float y)
    {
        TerrainUnitProperty candidate = null;
        var noise = GetNoise(x, y, elevationOrigin, elevationDimension,
                    elevationScale, elevationOctaves, elevationPersistence, elevationFrequencyBase, elevationExp);

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

        foreach (var property in terrainUnitProperties)
        {
            if (property.Match(noise)) candidate = property;
        }

        if (candidate == null)
        {
            candidate = terrainUnitProperties[0];
            Debug.Log($"Cannot match property at {x}, {y}, noise = {noise}");
        }

        return candidate;
    }

    public static float GetNoise(float x, float y, Vector2 origin, Vector2 dimension,
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

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.blue;
        foreach (var entry in positionToChunk)
        {
            Gizmos.DrawWireCube(entry.Key, Vector3.one * chunkSize);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(PlayerController.LookPosition.SnapToGrid(), Vector3.one);
    }

    /*private void OnGUI()
    {
        if (chunkSize % 2 == 0)
            chunkSize++;
    }*/
}
