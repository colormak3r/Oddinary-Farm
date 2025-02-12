using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(Main.gameObject);

        resourceGenerator = GetComponent<ResourceGenerator>();
    }

    [Header("Map Settings")]
    [SerializeField]
    private Vector2Int mapSize = new Vector2Int(500, 500);
    [SerializeField]
    private int tilePerFrame = 100;
    [SerializeField]
    private int mapPadding = 10;

    [Header("Terrain Settings")]
    [SerializeField]
    private TerrainUnitProperty[] terrainUnitProperties;
    [SerializeField]
    private TerrainUnitProperty voidUnitProperty;
    [SerializeField]
    private GameObject terrainUnitPrefab;

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

    private TerrainUnitProperty[,] terrainMap;
    private Sprite terrainMapSprite;

    private ResourceGenerator resourceGenerator;

    [Header("Debugs")]
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public IEnumerator Initialize()
    {
        yield return GenerateWord();
        yield return BuildWorld();
        if (IsHost)
        {
            yield return resourceGenerator.Initialize(mapSize);
        }

        isInitialized = true;
    }


    #region World Generation
    private IEnumerator GenerateWord()
    {
        yield return GenerateTerrain();
    }

    private IEnumerator GenerateTerrain()
    {
        var terrainMapTexture = new Texture2D(mapSize.x, mapSize.y);
        var halfMapSize = mapSize / 2;
        terrainMap = new TerrainUnitProperty[mapSize.x, mapSize.y];

        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                terrainMap[i, j] = GetDefaultProperty(i, j);
                terrainMapTexture.SetPixel(i, j, terrainMap[i, j].MapColor);
            }
        }

        terrainMapTexture.Apply();
        terrainMapSprite = Sprite.Create(terrainMapTexture, new Rect(0, 0, mapSize.x, mapSize.y), Vector2.zero);
        terrainMapSprite.texture.filterMode = FilterMode.Point;
        MapUI.Main.UpdateElevationMap(terrainMapSprite);

        yield return null;
    }

    private TerrainUnitProperty GetDefaultProperty(float x, float y)
    {
        TerrainUnitProperty candidate = null;
        var noise = GetNoise(x, y, elevationOrigin, elevationDimension,
                    elevationScale, elevationOctaves, elevationPersistence, elevationFrequencyBase, elevationExp);

        // Apply island shaping to the noise
        var nx = 2 * x / mapSize.x - 1;
        var ny = 2 * y / mapSize.y - 1;
        var d = 1 - (1 - nx * nx) * (1 - ny * ny);
        //var d = Mathf.Min(1, (nx * nx + ny * ny) / Mathf.Sqrt(2));
        noise = Mathf.Clamp01(Mathf.Lerp(noise, 1 - d, 0.5f));

        foreach (var property in terrainUnitProperties)
        {
            if (property.Match(noise)) candidate = property;
        }

        if (candidate == null)
        {
            candidate = terrainUnitProperties[0];
            Debug.LogError($"Cannot match property at {x}, {y}, noise = {noise}");
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

    #endregion

    #region World Building

    private IEnumerator BuildWorld()
    {
        yield return BuildTerrain();
    }

    private IEnumerator BuildTerrain()
    {
        var halfMapSize = mapSize / 2;

        var count = 0;
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                SpawnTerrainUnit(new Vector2(i - halfMapSize.x, j - halfMapSize.y), terrainMap[i, j]);
                count++;
                if (count > tilePerFrame)
                {
                    count = 0;
                    yield return null;
                }
            }
        }

        // Spawn the padding tiles around the main map.
        // Loop through the full range, which is the main map plus padding on every side.
        for (int i = -mapPadding; i < mapSize.x + mapPadding; i++)
        {
            for (int j = -mapPadding; j < mapSize.y + mapPadding; j++)
            {
                // Skip positions that are inside the main map,
                // since those tiles have already been spawned.
                if (i >= 0 && i < mapSize.x && j >= 0 && j < mapSize.y)
                    continue;

                // Calculate the position with the same center offset.
                Vector2 pos = new Vector2(i - halfMapSize.x, j - halfMapSize.y);
                SpawnTerrainUnit(pos, voidUnitProperty);
                count++;
                if (count >= tilePerFrame)
                {
                    count = 0;
                    yield return null;
                }
            }
        }
    }

    private void SpawnTerrainUnit(Vector2 position, TerrainUnitProperty property)
    {
        var terrainObj = LocalObjectPooling.Main.Spawn(terrainUnitPrefab);
        terrainObj.transform.position = position;
        terrainObj.transform.parent = transform;
        terrainObj.GetComponent<TerrainUnit>().Initialize(property);
    }

    #endregion

    #region Utility
    public TerrainUnitProperty GetMappedProperty(int x, int y)
    {
        //Debug.Log($"GetMappedProperty {x}, {y}");
        var halfMapSize = mapSize / 2;
        var i = x + halfMapSize.x;
        var j = y + halfMapSize.y;
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

    public bool IsValidResourcePosition(int x, int y)
    {
        return terrainMap[x, y].Elevation.min == 0.55f; // Elevation of grass
    }

    #endregion
}


/*using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using Random = UnityEngine.Random;
using UnityEngine.UIElements;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(Main.gameObject);

        InvalidFolliagePositionList = new NetworkList<Vector2Int>();
    }

    private class Chunk
    {
        public Vector2 position;
        public int size;
        public Transform transform;
        public List<GameObject> terrainUnits;
        public List<GameObject> folliages;

        public Chunk(Vector2 position, int size, Transform transform)
        {
            this.position = position;
            this.size = size;
            terrainUnits = new List<GameObject>();
            terrainUnits.Capacity = size * size;
            folliages = new List<GameObject>();
            folliages.Capacity = size * size;
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
    private bool isInitialized;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private bool isGenerating;
    public bool IsGenerating => isGenerating;

    private NetworkList<Vector2Int> InvalidFolliagePositionList;
    private HashSet<Vector2Int> invalidFolliagePositionHashSet = new HashSet<Vector2Int>();

    private Dictionary<Vector2, Chunk> positionToChunk = new Dictionary<Vector2, Chunk>();

    private Vector2 closetChunkPosition_cached = Vector2.one;

    private LocalObjectPooling localObjectPooling;
    private ResourceGenerator resourceGenerator;

    private TerrainUnitProperty[,] terrainMap;
    int halfMapSizeX;
    int halfMapSizeY;

    private Sprite terrainMapSprite;

    private void Start()
    {
        localObjectPooling = LocalObjectPooling.Main;
        resourceGenerator = GetComponent<ResourceGenerator>();
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

        MapUI.Main.UpdateElevationMap(terrainMapSprite);

        resourceGenerator.GenerateMap(mapSize);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InvalidFolliagePositionList.OnListChanged += OnInvalidFolliagePositionsChanged;

        if (IsHost)
        {
            //var builder = $"Host {resourceGenerator.ResourcePositions.Count}\n";
            foreach (var position in resourceGenerator.ResourcePositions)
            {
                InvalidFolliagePositionList.Add(position);
                //builder += position + "\n";
            }
            //Debug.Log(builder);
        }
        else
        {
            //var builder = $"Client {InvalidFolliagePositionList.Count}\n";
            foreach (var position in InvalidFolliagePositionList)
            {
                invalidFolliagePositionHashSet.Add(position);
                //builder += position + "\n";
            }
            //Debug.Log(builder);
        }

        isInitialized = true;
    }


    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        InvalidFolliagePositionList.OnListChanged -= OnInvalidFolliagePositionsChanged;
    }

    private void OnInvalidFolliagePositionsChanged(NetworkListEvent<Vector2Int> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<Vector2Int>.EventType.Add)
        {
            invalidFolliagePositionHashSet.Add(changeEvent.Value);
        }
        else if (changeEvent.Type == NetworkListEvent<Vector2Int>.EventType.Remove)
        {
            invalidFolliagePositionHashSet.Remove(changeEvent.Value);
        }
        //Debug.Log($"Set: {invalidFolliagePositionHashSet.Count}, List: {InvalidFolliagePositionList.Count}");
    }

    public bool IsValidResourcePosition(int x, int y)
    {
        return GetMappedProperty(x, y).Elevation.min >= 0.55f;
    }

    public void InvalidateFolliagePositionOnServer(Vector2 position)
    {
        InvalidFolliagePositionList.Add(position.ToInt());
    }

    public IEnumerator GenerateTerrainCoroutine(Vector2 position)
    {
        position = position.SnapToGrid();

        var closetChunkPosition = position.SnapToGrid(chunkSize, true);
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
                var terrainPos = new Vector2Int((int)position.x + i, (int)position.y + j);
                var property = GetMappedProperty(terrainPos.x, terrainPos.y);

                //var canSpawnFolliage = !resourceGenerator.ResourcePositions.Contains(new Vector2Int((int)position.x, (int)position.y));
                SpawnTerrainUnit(terrainPos, chunk, property, !invalidFolliagePositionHashSet.Contains(terrainPos));
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

        foreach (var folliage in chunk.folliages)
        {
            localObjectPooling.Despawn(folliage);
            yield return null;
        }

        positionToChunk.Remove(chunk.position);
        Destroy(chunk.transform.gameObject);
    }

    public TerrainUnitProperty GetMappedProperty(int x, int y)
    {
        //Debug.Log($"GetMappedProperty {x}, {y}");
        var i = x + halfMapSizeX;
        var j = y + halfMapSizeY;
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
        var closetChunkPosition = position.SnapToGrid(chunkSize, true);

        if (!positionToChunk.ContainsKey(closetChunkPosition)) return;
        var chunk = positionToChunk[closetChunkPosition];
        var unit = chunk.GetUnit(position);

        // Remove the old unit
        localObjectPooling.Despawn(unit);
        chunk.terrainUnits.Remove(unit);

        // Spawn a new unit
        SpawnTerrainUnit(position, chunk, newProperty, !invalidFolliagePositionHashSet.Contains(position.ToInt()));
    }

    public void RemoveBlock(Vector2 position)
    {
        PlaceBlockRpc(position, voidUnitProperty);
    }

    private void SpawnTerrainUnit(Vector2 position, Chunk chunk, TerrainUnitProperty property, bool canSpawnFolliage)
    {
        var terrainObj = localObjectPooling.Spawn(terrainUnitPrefab);
        terrainObj.transform.position = position;
        terrainObj.transform.parent = chunk.transform;
        terrainObj.GetComponent<TerrainUnit>().Initialize(property, canSpawnFolliage);
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
    }
}
*/