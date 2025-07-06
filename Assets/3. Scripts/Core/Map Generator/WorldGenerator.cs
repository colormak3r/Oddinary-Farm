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

public class ObservablePrefabStatus
{
    public GameObject prefab = null;
    public bool isObservable = false;
    public bool isSpawned = false;
    public ObservabilityController controller = null;

    public ObservablePrefabStatus(GameObject prefab, bool isObservable)
    {
        this.prefab = prefab;
        this.isObservable = isObservable;
        isSpawned = false;
        controller = null;
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
    public Vector2Int MapSize => mapSize;
    [SerializeField]
    private int chunkSize = 5;
    public int ChunkSize => chunkSize;
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
    private TerrainUnitProperty sandUnitProperty;
    [SerializeField]
    private GameObject terrainUnitPrefab;

    [Header("Flood Settings")]
    [SerializeField]
    private Color floodColor;

    [Header("Resource Settings")]
    [SerializeField]
    private bool canSpawnResources = true;
    [SerializeField]
    private GameObject[] resourcePrefabs;
    [SerializeField]
    private int noResourceZoneSize = 15; // Size of no resource zone around the player spawn point
    [SerializeField]
    private int renderResourceDistance = 5; // Distance to render resources around the player
    public int RenderResourceDistance => renderResourceDistance;

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
    [SerializeField]
    private TMP_Text elevationText;

    private Vector2Int halfMapSize;
    private Vector2Int paddingSize;
    private Vector2Int trueHalfMapSize;
    private Vector2Int trueMapSize;
    private int halfChunkSize;

    private Offset2DArray<TerrainUnitProperty> terrainMap;
    private Texture2D miniMapTexture;

    private Offset2DArray<bool> folliageMap;
    private HashSet<Vector2> invalidFoliagePositionHashSet = new HashSet<Vector2>();

    private Offset2DArray<ObservablePrefabStatus> resourceStatusMap;

    private Dictionary<Vector2, Chunk> positionToChunk = new Dictionary<Vector2, Chunk>();

    [Header("Debugs")]
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private bool showDebugs = false; // Show debug information in the console
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private bool showStep;

    public float HighestElevationValue => elevationMap.HighestElevationValue;
    public Vector2 HighestElevationPoint => elevationMap.HighestElevationPoint;
    public float OceanElevationThreshold => sandUnitProperty.Elevation.max;

    public IEnumerator Initialize()
    {
        yield return GenerateWorld();
        FloodManager.Main.Initialize(HighestElevationValue);
        yield return BuildWorld(HighestElevationPoint);
        StartCoroutine(UnloadResourceCoroutine());
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
        while (!GameManager.Main.IsInitialized) yield return null;

        var map = elevationMap.RawMap;
        var halfMapSize = mapSize / 2;
        var invalidPosition = new List<Vector2>();

        for (int i = map.minX; i < map.maxX; i++)
        {
            for (int j = map.minY; j < map.maxY; j++)
            {
                if (map[i, j] < depthLevel)
                {
                    miniMapTexture.SetPixel(i + halfMapSize.x, j + halfMapSize.y, floodColor);
                    var position = new Vector2(i, j);
                    InvalidateFolliageOnClient(position);
                    RemoveFoliage(position);
                    yield return null; // Yield to prevent freezing the main thread
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

    public void ResetMinimap(Vector2Int[] positions)
    {
        var map = elevationMap.RawMap;
        var halfMapSize = mapSize / 2;

        foreach (var position in positions)
        {
            var x = position.x;
            var y = position.y;
            miniMapTexture.SetPixel(x + halfMapSize.x, y + halfMapSize.y, GetDefaultProperty(elevationMap.RawMap[x, y]).MapColor);
        }

        miniMapTexture.Apply();
        UpdateMapTexture(miniMapTexture);
    }

    #region World Generation
    public IEnumerator GenerateWorld()
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

        // Generate data from the maps
        GenerateTerrain();
        GenerateFolliage();
        GenerateResources();

        yield return null;
    }
    #endregion

    #region Terrain Generation
    private void GenerateTerrain()
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
    }

    private void UpdateMapTexture(Texture2D texture)
    {
        var terrainMapSprite = Sprite.Create(texture, new Rect(0, 0, mapSize.x, mapSize.y), new Vector2(0.5f, 0.5f));
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
    private void GenerateResources()
    {
        resourceStatusMap = new Offset2DArray<ObservablePrefabStatus>(-trueHalfMapSize.x, trueHalfMapSize.x, -trueHalfMapSize.y, trueHalfMapSize.y);
        var prefabLength = resourcePrefabs.Length;

        for (int x = -trueHalfMapSize.x; x < trueHalfMapSize.x + 1; x++)
        {
            for (int y = -trueHalfMapSize.y; y < trueHalfMapSize.y + 1; y++)
            {
                if (x < -halfMapSize.x || x >= halfMapSize.x || y < -halfMapSize.y || y >= halfMapSize.y)
                {
                    // Padding area
                    resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false);    // No resource      
                }
                else
                {
                    var dx = x - HighestElevationPoint.x;
                    var dy = y - HighestElevationPoint.y;
                    if (terrainMap[x, y] != voidUnitProperty                                        // No resources in water
                        && resourceMap.RawMap[x, y] >= 0.99f                                        // Resource threshold
                        && terrainMap[x, y].Elevation.min > FloodManager.Main.CurrentSafeLevel      // Not flooded
                        && dx * dx + dy * dy > noResourceZoneSize * noResourceZoneSize)             // Outside of no resource zone (around player spawn point)
                    {
                        resourceStatusMap[x, y] = new ObservablePrefabStatus(resourcePrefabs.GetRandomElement(), true);
                    }
                    else
                    {
                        resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false); // No resource                    
                    }
                }
            }
        }
    }
    #endregion

    #region Generate Folliage
    private void GenerateFolliage()
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
    }
    #endregion

    #region World Building
    private Vector2 closetChunkPosition_cached = Vector2.one;
    private bool isGenerating = false;
    public bool IsGenerating => isGenerating;
    private Vector2 cached_position;
    private float traveledDistance = 0;

    public IEnumerator BuildWorld(Vector2 position)
    {
        // Update the cached position
        if (cached_position != position)
        {
            traveledDistance += Vector2.Distance(cached_position, position);
            StatisticsManager.Main.UpdateStat(StatisticType.DistanceTravelled, (ulong)Mathf.RoundToInt(traveledDistance));
            cached_position = position;
        }

        // Update the player position on the map
        MapUI.Main.UpdatePlayerPosition(position, trueMapSize);

        // Snap the position to the grid
        var snappedPosition = position.SnapToGrid();

        // Update the elevation text
        var elevation = GetElevation(snappedPosition.x, snappedPosition.y, true);
        elevationText.text = Mathf.RoundToInt((elevation - FloodManager.Main.BaseFloodLevel) * 1000) + "ft";

        // Update the audio elevation for biome music context switcher
        AudioManager.Main.OnElevationUpdated(elevation);

        // Build the terrain at the snapped position
        yield return BuildTerrain(position);
    }

    public IEnumerator BuildTerrain(Vector2 position)
    {
        // Assume the position has been snapped in BuildWorld
        // position = position.SnapToGrid();

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
        List<Vector2> positionsToRemove = positionToChunk.Keys.Where(pos =>
        Mathf.Abs(pos.x - closetChunkPosition.x) > (renderDistance + renderXOffset + 1) * chunkSize ||
        Mathf.Abs(pos.y - closetChunkPosition.y) > (renderDistance + 1) * chunkSize).ToList();

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

    private Dictionary<Vector2Int, ObservablePrefabStatus> spawnedResources = new Dictionary<Vector2Int, ObservablePrefabStatus>();

    public void BuildResourceOnServer(Vector2 position)
    {
        if (!IsServer) return;

        if (!canSpawnResources || (ScenarioManager.Main.OverrideSettings && !ScenarioManager.Main.CanSpawnResources)) return;

        StartCoroutine(LoadResourceCoroutine(position));
    }

    private IEnumerator LoadResourceCoroutine(Vector2 position)
    {
        var snappedPosition = position.SnapToGrid().ToInt();
        int renderRadius = renderResourceDistance * chunkSize;

        for (int x = snappedPosition.x - renderRadius; x <= snappedPosition.x + renderRadius; x++)
        {
            for (int y = snappedPosition.y - renderRadius; y <= snappedPosition.y + renderRadius; y++)
            {
                var resourcePos = new Vector2Int(x, y);
                if (resourceStatusMap.GetElementSafe(resourcePos.x, resourcePos.y, out var resourceStatus)
                    && resourceStatus.prefab != null)
                {
                    if (resourceStatus.isObservable && !resourceStatus.isSpawned)
                    {
                        //TODO: Handle resource spawning logic with buffer
                        var resObj = SpawnResource(resourcePos, resourceStatus.prefab);
                        resObj.GetComponent<ObservabilityController>().InitializeOnServer(resourceStatus);
                        resourceStatus.isSpawned = true;
                        spawnedResources[resourcePos] = resourceStatus;

                        yield return null;
                    }
                }
            }
        }
    }

    private IEnumerator UnloadResourceCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            List<Vector2Int> positionsToRemove = new List<Vector2Int>();
            var listOfPlayers = NetworkManager.ConnectedClients.Values.ToList();

            foreach (var resource in spawnedResources)
            {
                var pos = resource.Key;
                bool isVisibleToAnyPlayer = false;

                foreach (var player in listOfPlayers)
                {
                    var netObj = player.PlayerObject;
                    if (netObj == null || !netObj.IsSpawned) continue;

                    var playerChunkPos = netObj.transform.position.SnapToGrid(chunkSize, true);

                    if (Mathf.Abs(pos.x - playerChunkPos.x) <= (renderResourceDistance + 1) * chunkSize &&
                        Mathf.Abs(pos.y - playerChunkPos.y) <= (renderResourceDistance + 1) * chunkSize)
                    {
                        isVisibleToAnyPlayer = true;
                        break; // No need to check other players
                    }
                }

                if (!isVisibleToAnyPlayer)
                {
                    positionsToRemove.Add(pos);
                }
            }

            if (showDebugs) Debug.Log($"Unloading {positionsToRemove.Count} objects");
            foreach (var item in positionsToRemove)
            {
                if (spawnedResources[item].isSpawned)
                {
                    spawnedResources[item].controller.UnloadOnServer();
                    spawnedResources.Remove(item);
                }
            }
        }
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

    private GameObject SpawnResource(Vector2 position, GameObject prefab)
    {
        var resObj = Instantiate(prefab, new Vector2(position.x, position.y - 0.5f), Quaternion.identity, transform);
        var resNetObject = resObj.GetComponent<NetworkObject>();
        resNetObject.Spawn();
        resNetObject.TrySetParent(transform);

        return resObj;
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

    public void SetCanSpawnResources(bool value)
    {
        if (!IsServer) return;

        canSpawnResources = value;
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