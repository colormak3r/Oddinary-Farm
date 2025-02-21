using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class Chunk
{
    public Vector2 position;
    public int size;
    public Transform transform;

    public bool isBuilt;
    public bool isBuilding;
    public bool isRemoving;

    public GameObject[,] terrainUnits;
    public GameObject[,] folliages;

    public Chunk(Vector2 position, int size, Transform transform)
    {
        this.position = position;
        this.size = size;
        this.transform = transform;

        isBuilt = false;
        isBuilding = false;
        isRemoving = false;

        terrainUnits = new GameObject[size, size];
        folliages = new GameObject[size, size];
    }
}

public class Offset2DArray<T> : IEnumerable<T>
{
    private T[,] array;
    private int offsetX, offsetY;

    // minX, maxX, minY, maxY define the logical index bounds.
    public Offset2DArray(int minX, int maxX, int minY, int maxY)
    {
        offsetX = -minX;
        offsetY = -minY;
        array = new T[maxX - minX + 1, maxY - minY + 1];
    }

    public T this[int x, int y]
    {
        get { return array[x + offsetX, y + offsetY]; }
        set { array[x + offsetX, y + offsetY] = value; }
    }

    // Implementation of IEnumerable<T>
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                yield return array[i, j];
            }
        }
    }

    // Explicit interface implementation for non-generic IEnumerable.
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

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
    private int chunkSize = 5;
    [SerializeField]
    private int paddingChunkCount = 2;
    [SerializeField]
    private int renderDistance = 3;
    [SerializeField]
    private int renderXOffset = 4;

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

    private Offset2DArray<TerrainUnitProperty> terrainMap;
    private Sprite terrainMapSprite;
    private Vector2Int halfMapSize;
    private Vector2Int paddingSize;
    private Vector2Int trueHalfMapSize;
    private Vector2Int trueMapSize;
    private int halfChunkSize;

    private Offset2DArray<Chunk> chunkMap;

    private ResourceGenerator resourceGenerator;

    [Header("Debugs")]
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public IEnumerator Initialize()
    {
        yield return GenerateWorld();
        yield return BuildWorld(Vector2.zero);
        if (IsHost)
        {
            yield return resourceGenerator.Initialize(mapSize);
        }

        isInitialized = true;
    }


    #region World Generation
    private IEnumerator GenerateWorld()
    {
        yield return GenerateTerrain();
    }

    private IEnumerator GenerateTerrain()
    {
        // Calculate the half map size and padding size
        halfMapSize = mapSize / 2;
        paddingSize = Vector2Int.one * paddingChunkCount * chunkSize;
        trueHalfMapSize = halfMapSize + paddingSize;
        trueMapSize = trueHalfMapSize * 2;
        halfChunkSize = chunkSize / 2;

        // Generate the terrain map
        var terrainMapTexture = new Texture2D(trueMapSize.x, trueMapSize.y);
        terrainMap = new Offset2DArray<TerrainUnitProperty>(-trueHalfMapSize.x, trueHalfMapSize.x, -trueHalfMapSize.y, trueHalfMapSize.y);

        for (int x = -trueHalfMapSize.x; x < trueHalfMapSize.x; x++)
        {
            for (int y = -trueHalfMapSize.y; y < trueHalfMapSize.y; y++)
            {
                terrainMap[x, y] = GetDefaultProperty(x + trueHalfMapSize.x, y + trueHalfMapSize.y);
                terrainMapTexture.SetPixel(x + trueHalfMapSize.x, y + trueHalfMapSize.y, terrainMap[x, y].MapColor);
            }
        }

        terrainMapTexture.Apply();
        terrainMapSprite = Sprite.Create(terrainMapTexture, new Rect(0, 0, mapSize.x, mapSize.y), Vector2.zero);
        terrainMapSprite.texture.filterMode = FilterMode.Point;
        MapUI.Main.UpdateElevationMap(terrainMapSprite);

        // Generate the chunk map
        var chunkTransform = new GameObject("Chunks").transform;
        chunkTransform.parent = transform;

        chunkMap = new Offset2DArray<Chunk>(-trueHalfMapSize.x, trueHalfMapSize.x, -trueHalfMapSize.y, trueHalfMapSize.y);
        for (int x = -trueHalfMapSize.x; x < trueHalfMapSize.x; x += chunkSize)
        {
            for (int y = -trueHalfMapSize.y; y < trueHalfMapSize.y; y += chunkSize)
            {
                var chunkPos = new Vector2(x, y).SnapToGrid(chunkSize, true);

                var chunkObj = new GameObject("Chunk");
                chunkObj.transform.parent = chunkTransform;
                chunkObj.transform.position = chunkPos;

                chunkMap[(int)chunkPos.x, (int)chunkPos.y] = new Chunk(chunkPos, chunkSize, chunkObj.transform);
            }
        }

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
    public IEnumerator BuildWorld(Vector2 position)
    {
        yield return BuildTerrain(position);
    }

    private Vector2Int currentChunkPosition = Vector2Int.one;
    private IEnumerator BuildTerrain(Vector2 position)
    {
        // Get closet chunk position
        var snapped = position.SnapToGrid(chunkSize, true);
        var closetChunkPosition = new Vector2Int((int)snapped.x, (int)snapped.y);

        Coroutine buildCoroutine;
        Coroutine removeCoroutine;
        if (closetChunkPosition != currentChunkPosition)
        {
            currentChunkPosition = closetChunkPosition;
            buildCoroutine = StartCoroutine(BuildChunkGroup(closetChunkPosition));
            removeCoroutine = StartCoroutine(RemoveChunkOutOfView(closetChunkPosition));
            yield return buildCoroutine;
            yield return removeCoroutine;
        }

        yield return null;
    }

    private IEnumerator BuildChunkGroup(Vector2 position)
    {
        for (int i = -renderDistance - renderXOffset; i < renderDistance + 1 + renderXOffset; i++)
        {
            for (int j = -renderDistance; j < renderDistance + 1; j++)
            {
                var chunkPos = new Vector2(position.x + i * chunkSize, position.y + j * chunkSize);
                yield return BuildChunk(chunkMap[(int)chunkPos.x, (int)chunkPos.y]);
            }
        }
    }

    private List<Chunk> activeChunks = new List<Chunk>();

    private IEnumerator BuildChunk(Chunk chunk)
    {
        if (chunk.isBuilt || chunk.isBuilding) yield break;
        chunk.isBuilding = true;
        chunk.isRemoving = false;

        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                Vector2 pos = new Vector2(chunk.position.x - halfChunkSize + i, chunk.position.y - halfChunkSize + j);
                if (chunk.terrainUnits[i, j] != null) continue;

                chunk.terrainUnits[i, j] = SpawnTerrainUnit(pos, terrainMap[(int)pos.x, (int)pos.y]);

                // Terminate early if the chunk is no longer building
                if (chunk.isBuilding == false) yield break;
            }
        }

        chunk.isBuilt = true;
        chunk.isBuilding = false;

        activeChunks.Add(chunk);

        yield return null;
    }

    private IEnumerator RemoveChunkOutOfView(Vector2 position)
    {
        var minChunkX = (int)position.x - (renderDistance + renderXOffset) * chunkSize;
        var maxChunkX = (int)position.x + (renderDistance + 1 + renderXOffset) * chunkSize;
        var minChunkY = (int)position.y - renderDistance * chunkSize;
        var maxChunkY = (int)position.y + (renderDistance + 1) * chunkSize;

        var chunksToRemove = new List<Chunk>();
        foreach (var chunk in activeChunks)
        {
            if (chunk.position.x < minChunkX || chunk.position.x > maxChunkX || chunk.position.y < minChunkY || chunk.position.y > maxChunkY)
            {
                chunksToRemove.Add(chunk);
            }
        }

        foreach (var chunk in chunksToRemove)
        {
            activeChunks.Remove(chunk);
            yield return RemoveChunk(chunk);
        }
    }

    private IEnumerator RemoveChunk(Chunk chunk)
    {
        // Terminate early if the chunk is not built or is already building
        chunk.isBuilding = false;
        chunk.isRemoving = true;

        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                if (chunk.terrainUnits[i, j] != null)
                {
                    LocalObjectPooling.Main.Despawn(chunk.terrainUnits[i, j]);
                    chunk.terrainUnits[i, j] = null;
                }

                // Terminate early if the chunk is no longer removing
                if (chunk.isRemoving == false) yield break;
            }
        }

        chunk.isRemoving = false;
        chunk.isBuilt = false;

        yield return null;
    }

    private GameObject SpawnTerrainUnit(Vector2 position, TerrainUnitProperty property)
    {
        var terrainObj = LocalObjectPooling.Main.Spawn(terrainUnitPrefab);
        terrainObj.transform.position = position;
        terrainObj.transform.parent = transform;
        terrainObj.GetComponent<TerrainUnit>().Initialize(property);

        return terrainObj;
    }

    #endregion

    #region Utility
    public TerrainUnitProperty GetMappedProperty(int x, int y)
    {
        return terrainMap[x, y];
    }

    public bool IsValidResourcePosition(int x, int y)
    {
        return terrainMap[x, y].Elevation.min == 0.55f; // Elevation of grass
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (chunkMap != null)
        {
            foreach (var chunk in chunkMap)
            {
                if (chunk == null) continue;
                Gizmos.DrawWireCube(chunk.position, Vector3.one * chunk.size);
                Gizmos.DrawSphere(chunk.position, 0.1f);
                //Handles.Label(chunk.position + Vector2.one * 0.1f, $"({chunk.position.x},{chunk.position.y})");
            }
        }
    }
#endif

}
/*
    #region World Building
    [SerializeField]
    private bool isBuilding;
    private Coroutine buildWorldCoroutine;
    private Coroutine waitBuildWorldCoroutine;
    public IEnumerator BuildWorld(Vector2 position)
    {
        if (isBuilding) yield break;
        isBuilding = true;
        yield return BuildTerrain(position);
        isBuilding = false;
    }

    
    {
        var halfMapSize = mapSize / 2;
        var chunkXMax = mapSize.x / chunkSize + mapPadding * 2 + 1;
        var chunkYMax = mapSize.y / chunkSize + mapPadding * 2 + 1;
        var mapIndex = WorldToMapIndex(position);

        var coroutines = new List<Coroutine>();
        for (int x = mapIndex.x - renderDistance - renderXOffset; x < mapIndex.x + renderDistance + 1 + renderXOffset; x++)
        {
            for (int y = mapIndex.y - renderDistance; y < mapIndex.y + renderDistance; y++)
            {
                if (x >= 0 && x < chunkXMax && y >= 0 && y < chunkYMax)
                {
                    var chunk = chunkMap[x, y];
                    if (!chunk.isBuilt && !chunk.isBuilding)
                    {
                        activeChunks.Add(chunk);
                        coroutines.Add(StartCoroutine(BuildChunk(chunk)));
                        yield return null;
                    }
                }
            }
        }

        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }



    private IEnumerator RemoveChunkOutOfView(Vector2 position)
    {
        var mapIndex = WorldToMapIndex(position);

        var maxChunkX = mapIndex.x + renderDistance + 1 + renderXOffset;
        var minChunkX = mapIndex.x - renderDistance - renderXOffset;
        var maxChunkY = mapIndex.y + renderDistance;
        var minChunkY = mapIndex.y - renderDistance;

        var coroutines = new List<Coroutine>();
        List<Chunk> chunksToRemove = new List<Chunk>();
        foreach (var chunk in activeChunks)
        {
            if (chunk.position.x < minChunkX || chunk.position.x > maxChunkX || chunk.position.y < minChunkY || chunk.position.y > maxChunkY)
            {
                chunksToRemove.Add(chunk);
                coroutines.Add(StartCoroutine(RemoveChunk(chunk)));
                yield return null;
            }
        }

        foreach (var chunk in chunksToRemove)
        {
            activeChunks.Remove(chunk);
        }

        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

    private Vector2Int WorldToMapIndex(Vector2 position)
    {
        var halfMapSize = mapSize / 2;
        int i = Mathf.RoundToInt((position.x + halfMapSize.x) / chunkSize) + mapPadding;
        int j = Mathf.RoundToInt((position.y + halfMapSize.y) / chunkSize) + mapPadding;
        return new Vector2Int(i, j);
    }


    private IEnumerator RemoveChunk(Chunk chunk)
    {
        chunk.isBuilding = false;

        foreach (var terrainUnit in chunk.terrainUnits)
        {
            LocalObjectPooling.Main.Despawn(terrainUnit);
        }

        foreach (var folliage in chunk.folliages)
        {
            LocalObjectPooling.Main.Despawn(folliage);
        }

        chunk.terrainUnits.Clear();
        chunk.folliages.Clear();

        chunk.isBuilt = false;
        yield return null;
    }

    
}*/


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