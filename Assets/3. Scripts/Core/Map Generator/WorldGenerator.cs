using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Chunk
{
    public Vector2 position;
    public int size;
    public Transform transform;

    public bool isBuilt;
    public bool isBuilding;
    public bool isRemoving;

    public List<GameObject> terrainUnits;
    public List<GameObject> foliages;

    public Chunk(Vector2 position, int size, Transform transform)
    {
        this.position = position;
        this.size = size;
        this.transform = transform;

        isBuilt = false;
        isBuilding = false;
        isRemoving = false;

        terrainUnits = new List<GameObject>();
        terrainUnits.Capacity = size * size;
        foliages = new List<GameObject>();
        foliages.Capacity = size * size;
    }
}

public class Offset2DArray<T> : IEnumerable<T>
{
    private T[,] array;
    private int offsetX, offsetY;
    public int minX => -offsetX;
    public int maxX => array.GetLength(0) - offsetX - 1;
    public int minY => -offsetY;
    public int maxY => array.GetLength(1) - offsetY - 1;

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

    public bool GetElementSafe(int x, int y, out T value)
    {
        if (x < minX || x > maxX || y < minY || y > maxY)
        {
            value = default;
            return false;
        }
        else
        {
            value = this[x, y];
            return true;
        }
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

    [Header("Resource Settings")]
    [SerializeField]
    private bool canSpawnResources = true;
    [SerializeField]
    private GameObject[] resourcePrefabs;
    [SerializeField]
    private int countPerYield = 10;

    [Header("Folliage Settings")]
    [SerializeField]
    private bool canSpawnFoliage = true;
    [SerializeField]
    private GameObject[] foliagePrefabs;

    [Header("Map Components")]
    [SerializeField]
    private ElevationMap elevationMap;
    [SerializeField]
    private ResourceMap resourceMap;
    [SerializeField]
    private MoistureMap moistureMap;

    [Header("Map Components")]
    [SerializeField]
    private TMP_Text heightText;

    private Vector2Int halfMapSize;
    private Vector2Int paddingSize;
    private Vector2Int trueHalfMapSize;
    private Vector2Int trueMapSize;
    private int halfChunkSize;

    private Offset2DArray<TerrainUnitProperty> terrainMap;
    private Texture2D miniMapTexture;

    private Offset2DArray<bool> folliageMap;
    private HashSet<Vector2> invalidFoliagePositionHashSet = new HashSet<Vector2>();

    private Dictionary<Vector2, Chunk> positionToChunk = new Dictionary<Vector2, Chunk>();

    [Header("Debugs")]
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private bool showStep;

    public float HighestElevation => elevationMap.MaxValue;

    public IEnumerator Initialize()
    {
        yield return GenerateWorld();
        FloodManager.Main.Initialize();
        yield return BuildWorld(Vector2.zero);
        isInitialized = true;
    }

    public override void OnNetworkSpawn()
    {
        FloodManager.Main.OnFloodLevelChanged += HandleOnFloodLevelChanged;
    }

    public override void OnNetworkDespawn()
    {
        FloodManager.Main.OnFloodLevelChanged -= HandleOnFloodLevelChanged;
    }

    Coroutine floodCoroutine;
    private void HandleOnFloodLevelChanged(float floodLevel, float waterLevel, float depthLevel)
    {
        if (floodCoroutine != null) StopCoroutine(floodCoroutine);
        floodCoroutine = StartCoroutine(FloodLevelChangeCoroutine(depthLevel));
    }

    private IEnumerator FloodLevelChangeCoroutine(float depthLevel)
    {
        yield return new WaitUntil(() => GameManager.Main.IsInitialized);
        var map = elevationMap.RawMap;
        var halfMapSize = mapSize / 2;
        var invalidPosition = new List<Vector2>();

        for (int i = map.minX; i < map.maxX; i++)
        {
            for (int j = map.minY; j < map.maxY; j++)
            {
                if (map[i, j] < depthLevel)
                {
                    miniMapTexture.SetPixel(i + halfMapSize.x, j + halfMapSize.y, Color.blue);
                    var position = new Vector2(i, j);
                    InvalidateFolliageOnClient(position);
                    RemoveFoliage(position);
                }
            }
        }

        miniMapTexture.Apply();
        UpdateMapTexture(miniMapTexture);
    }

    public void UpdateMinimap(Vector2Int[] positions, Color color)
    {
        foreach (var position in positions)
        {
            miniMapTexture.SetPixel(position.x + halfMapSize.x, position.y + halfMapSize.y, color);
        }

        miniMapTexture.Apply();
        UpdateMapTexture(miniMapTexture);
    }

    #region World Generation
    private IEnumerator GenerateWorld()
    {
        // Calculate the half map size and padding size
        halfMapSize = mapSize / 2;
        paddingSize = Vector2Int.one * paddingChunkCount * chunkSize;
        trueHalfMapSize = halfMapSize + paddingSize;
        trueMapSize = trueHalfMapSize * 2;
        halfChunkSize = chunkSize / 2;

        // Initialize the maps
        elevationMap.GenerateMap(mapSize);
        resourceMap.GenerateMap(mapSize);
        moistureMap.GenerateMap(mapSize);

        yield return GenerateTerrain();
        if (IsHost)
        {
            yield return GenerateResources();
        }
        yield return GenerateFolliage();
    }
    #endregion

    #region Terrain Generation
    private IEnumerator GenerateTerrain()
    {
        // Generate the terrain map        
        terrainMap = new Offset2DArray<TerrainUnitProperty>(-trueHalfMapSize.x, trueHalfMapSize.x, -trueHalfMapSize.y, trueHalfMapSize.y);

        for (int x = -trueHalfMapSize.x; x < trueHalfMapSize.x + 1; x++)
        {
            for (int y = -trueHalfMapSize.y; y < trueHalfMapSize.y + 1; y++)
            {
                if (x < -halfMapSize.x || x >= halfMapSize.x || y < -halfMapSize.y || y >= halfMapSize.y)

                    terrainMap[x, y] = voidUnitProperty;
                else
                    terrainMap[x, y] = GetDefaultProperty(elevationMap.RawMap[x, y]);
            }
        }

        // Update MapUI
        miniMapTexture = elevationMap.MapTexture;
        UpdateMapTexture(miniMapTexture);

        yield return null;
    }

    private void UpdateMapTexture(Texture2D texture)
    {
        var terrainMapSprite = Sprite.Create(texture, new Rect(0, 0, mapSize.x, mapSize.y), Vector2.zero);
        terrainMapSprite.texture.filterMode = FilterMode.Point;
        MapUI.Main.UpdateElevationMap(terrainMapSprite);
    }

    private TerrainUnitProperty GetDefaultProperty(float value)
    {
        TerrainUnitProperty candidate = null;
        foreach (var property in terrainUnitProperties)
        {
            if (property.Match(value)) candidate = property;
        }

        if (candidate == null)
        {
            candidate = terrainUnitProperties[0];
            Debug.LogError($"Cannot match property noise = {value}");
        }

        return candidate;
    }
    #endregion

    #region Resource Generation
    private IEnumerator GenerateResources()
    {
        if (!canSpawnResources) yield break;

        var count = 0;
        for (int x = -trueHalfMapSize.x; x < trueHalfMapSize.x + 1; x++)
        {
            for (int y = -trueHalfMapSize.y; y < trueHalfMapSize.y + 1; y++)
            {
                if (x < -halfMapSize.x || x >= halfMapSize.x || y < -halfMapSize.y || y >= halfMapSize.y)
                {
                    // Do nothing
                    // Padding area
                }
                else
                {
                    if (terrainMap[x, y] != voidUnitProperty && resourceMap.RawMap[x, y] >= 0.99f && terrainMap[x, y].Elevation.min > FloodManager.Main.CurrentSafeLevel)
                    {
                        SpawnResource(x, y);
                        count++;
                        if (count % countPerYield == 0)
                        {
                            yield return null;
                        }
                    }
                }
            }
        }
    }

    private void SpawnResource(int x, int y)
    {
        var res = Instantiate(resourcePrefabs.GetRandomElement(), new Vector3(x, y - 0.5f, 0), Quaternion.identity, transform);
        var resNetObject = res.GetComponent<NetworkObject>();
        resNetObject.Spawn();
        resNetObject.TrySetParent(transform);
    }
    #endregion

    #region Generate Folliage
    private IEnumerator GenerateFolliage()
    {
        folliageMap = new Offset2DArray<bool>(-trueHalfMapSize.x, trueHalfMapSize.x, -trueHalfMapSize.y, trueHalfMapSize.y);

        for (int x = -trueHalfMapSize.x; x < trueHalfMapSize.x + 1; x++)
        {
            for (int y = -trueHalfMapSize.y; y < trueHalfMapSize.y + 1; y++)
            {
                if (x < -halfMapSize.x || x >= halfMapSize.x || y < -halfMapSize.y || y >= halfMapSize.y)
                {
                    // Do nothing
                    // Padding area
                }
                else
                {
                    if (terrainMap[x, y].Elevation.min >= 0.55f && resourceMap.RawMap[x, y] < 0.99f && moistureMap.RawMap[x, y] > 0.1f && moistureMap.RawMap[x, y] < 0.2f)
                    {
                        folliageMap[x, y] = true;
                    }
                }
            }
        }
        yield return null;
    }
    #endregion

    #region World Building
    private Vector2 closetChunkPosition_cached = Vector2.one;
    private bool isGenerating = false;

    public IEnumerator BuildWorld(Vector2 position)
    {
        MapUI.Main.UpdatePlayerPosition(position, trueMapSize);
        var snappedPosition = position.SnapToGrid();
        heightText.text = Mathf.Round((GetElevation(position.x, position.y, true) - FloodManager.Main.BaseFloodLevel) * 1000) + "ft";
        yield return BuildTerrain(position);
    }

    public IEnumerator BuildTerrain(Vector2 position)
    {
        position = position.SnapToGrid();

        var closetChunkPosition = position.SnapToGrid(chunkSize, true);
        if (closetChunkPosition_cached != closetChunkPosition)
            closetChunkPosition_cached = closetChunkPosition;
        else
            yield break;

        if (isGenerating) yield break;
        isGenerating = true;

        yield return BuildChunkGrid(closetChunkPosition);

        yield return RemoveExcessChunks(closetChunkPosition);

        isGenerating = false;

        //if (showDebug) Debug.Log("Child count = " + transform.childCount);
    }

    private IEnumerator BuildChunkGrid(Vector2 closetChunkPosition)
    {
        //var coroutines = new List<Coroutine>();
        for (int i = -(renderDistance + renderXOffset); i < renderDistance + 1 + renderXOffset; i++)
        {
            for (int j = -renderDistance + 1; j < renderDistance; j++)
            {
                var chunkPos = new Vector2(closetChunkPosition.x + i * chunkSize, closetChunkPosition.y + j * chunkSize);

                if (!positionToChunk.ContainsKey(chunkPos))
                {
                    //coroutines.Add(StartCoroutine());
                    yield return BuildChunk(chunkPos, chunkSize, positionToChunk);
                }
            }
        }

        /*foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }*/
    }

    private IEnumerator BuildChunk(Vector2 position, int chunkSize, Dictionary<Vector2, Chunk> positionToChunk)
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
                var terrainUnit = SpawnTerrainUnit(terrainPos, property);
                chunk.terrainUnits.Add(terrainUnit);

                // Spawn Foliage
                folliageMap.GetElementSafe(terrainPos.x, terrainPos.y, out var validFoliagePosition);
                if (canSpawnFoliage && validFoliagePosition && !invalidFoliagePositionHashSet.Contains(terrainPos) && resourceMap.RawMap[terrainPos.x, terrainPos.y] < 0.99f)
                    chunk.foliages.Add(SpawnFoliage(terrainPos, foliagePrefabs.GetRandomElement()));


                if (showStep) yield return null;
            }
        }

        positionToChunk.Add(position, chunk);
        yield return null;
    }


    private IEnumerator RemoveExcessChunks(Vector2 closetChunkPosition)
    {
        /*List<Vector2> positionsToRemove = new List<Vector2>();

        // Determine the bounds of the loop
        int rangeX = (renderDistance + renderXOffset) * chunkSize;
        int rangeY = renderDistance * chunkSize;

        int minX = (int)closetChunkPosition.x - rangeX;
        int maxX = (int)closetChunkPosition.x + rangeX;
        int minY = (int)closetChunkPosition.y - rangeY;
        int maxY = (int)closetChunkPosition.y + rangeY;

        // Iterate over the dictionary to find chunks outside the bounds
        foreach (var entry in positionToChunk)
        {
            Vector2 pos = entry.Key;
            if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
            {
                positionsToRemove.Add(pos);
            }
        }*/

        List<Vector2> positionsToRemove = positionToChunk.Keys.Where(pos =>
        Mathf.Abs(pos.x - closetChunkPosition.x) > (renderDistance + renderXOffset) * chunkSize ||
        Mathf.Abs(pos.y - closetChunkPosition.y) > renderDistance * chunkSize).ToList();

        // Remove the identified chunks
        foreach (var pos in positionsToRemove)
        {
            var chunk = positionToChunk[pos];
            yield return RemoveChunk(chunk);
        }
    }

    private IEnumerator RemoveChunk(Chunk chunk)
    {
        foreach (var terrainUnit in chunk.terrainUnits)
        {
            LocalObjectPooling.Main.Despawn(terrainUnit);
            if (showStep) yield return null;
        }
        chunk.terrainUnits.Clear();

        foreach (var folliage in chunk.foliages)
        {
            LocalObjectPooling.Main.Despawn(folliage);
            if (showStep) yield return null;
        }

        positionToChunk.Remove(chunk.position);
        Destroy(chunk.transform.gameObject);
        yield return null;
    }

    private GameObject SpawnTerrainUnit(Vector2 position, TerrainUnitProperty property)
    {
        var terrainObj = LocalObjectPooling.Main.Spawn(terrainUnitPrefab);
        terrainObj.transform.position = position;
        terrainObj.transform.parent = transform;
        terrainObj.GetComponent<TerrainUnit>().Initialize(property);
        terrainObj.GetComponent<FloodController>().SetElevation(GetElevation(position.x, position.y, true));

        return terrainObj;
    }

    private GameObject SpawnFoliage(Vector2 position, GameObject prefab)
    {
        var folliageObj = LocalObjectPooling.Main.Spawn(prefab);
        folliageObj.transform.position = new Vector2(position.x, position.y - 0.5f);
        folliageObj.transform.parent = transform;
        folliageObj.GetComponent<FloodController>().SetElevation(GetElevation(position.x, position.y, true));

        return folliageObj;
    }

    #endregion

    #region Utility
    public float GetElevation(float x, float y, bool lerp = false)
    {
        var snappedPos = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        elevationMap.RawMap.GetElementSafe(snappedPos.x, snappedPos.y, out var snappedElevation);
        var elevation = snappedElevation;

        if (lerp)
        {
            var offsetX = Mathf.RoundToInt(Mathf.Sign(transform.position.x - snappedPos.x));
            var offsetY = Mathf.RoundToInt(Mathf.Sign(transform.position.y - snappedPos.y));
            elevationMap.RawMap.GetElementSafe(snappedPos.x + offsetX, snappedPos.y + offsetY, out var offsetElevation);
            elevation = Mathf.Lerp(snappedElevation, offsetElevation, 1 - Vector2.Distance(transform.position, snappedPos));
        }

        return elevation;
    }

    public TerrainUnitProperty GetMappedProperty(int x, int y)
    {
        if (x < -halfMapSize.x || x >= halfMapSize.x || y < -halfMapSize.y || y >= halfMapSize.y)
            return voidUnitProperty;
        else
            return terrainMap[x, y];
    }

    public bool IsValidResourcePosition(int x, int y)
    {
        return terrainMap[x, y].Elevation.min == 0.55f; // Elevation of grass
    }

    public void InvalidateFolliage(Vector2[] positions)
    {
        InvalidateFolliageRpc(positions);
    }

    [Rpc(SendTo.Everyone)]
    private void InvalidateFolliageRpc(Vector2[] positions)
    {
        foreach (var position in positions)
        {
            InvalidateFolliageOnClient(position);
        }
    }

    public void InvalidateFolliageOnClient(Vector2 position)
    {
        invalidFoliagePositionHashSet.Add(position);
    }

    public void RemoveFoliage(Vector2 position)
    {
        var chunkPosition = position.SnapToGrid(chunkSize, true);
        if (positionToChunk.ContainsKey(chunkPosition))
        {
            var chunk = positionToChunk[chunkPosition];
            position -= TransformUtility.HALF_UNIT_Y_V2;
            var folliage = chunk.foliages.Find(f => (Vector2)f.transform.position == position);
            if (folliage != null)
            {
                chunk.foliages.Remove(folliage);
                LocalObjectPooling.Main.Despawn(folliage);
            }
        }
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.blue;

        foreach (var chunk in positionToChunk.Values)
        {
            if (chunk == null) continue;
            Gizmos.DrawWireCube(chunk.position, Vector3.one * chunk.size);
            Gizmos.DrawSphere(chunk.position, 0.1f);
            //Handles.Label(chunk.position + Vector2.one * 0.1f, $"({chunk.position.x},{chunk.position.y})");
        }

        //Gizmos.color = Color.red;
        //Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x, mapSize.y, 0));
        //Gizmos.DrawWireCube(Vector3.zero, new Vector3(trueMapSize.x, trueMapSize.y, 0));
    }
#endif
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