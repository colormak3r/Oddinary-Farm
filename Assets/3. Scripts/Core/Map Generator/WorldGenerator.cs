using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public struct TileData          // one grid cell
{
    public GameObject terrain;  // never null
    public GameObject foliage;  // null if none spawned
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
    [SerializeField]
    private int tileStep = 10;

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

    [Header("Chihuahua Rescue Settings")]
    [SerializeField]
    private MinMaxFloat chihuahuaElevation;

    [Header("HypnoFrog Settings")]
    [SerializeField]
    private GameObject hypnoFrogPrefab;

    [Header("Resource Settings")]
    [SerializeField]
    private bool canSpawnResources = true;
    [SerializeField]
    private ResourceProperty[] resourceProperties;
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

    //private Dictionary<Vector2, Chunk> positionToChunk = new Dictionary<Vector2, Chunk>();

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
        BuildWorld(HighestElevationPoint);
        yield return new WaitUntil(() => !isGenerating);
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
                    RemoveFoliageOnClient(position);
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
        GenerateChihuahuaRescuePositions();

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
    private Dictionary<GameObject, int> prefabCount = new Dictionary<GameObject, int>();
    private void GenerateResources()
    {
        // 1. Initialise the full map
        resourceStatusMap = new Offset2DArray<ObservablePrefabStatus>(
            -trueHalfMapSize.x, trueHalfMapSize.x,
            -trueHalfMapSize.y, trueHalfMapSize.y);

        // 2. Fast-fill the padding strip once
        for (int x = -trueHalfMapSize.x; x <= trueHalfMapSize.x; x++)
            for (int y = -trueHalfMapSize.y; y <= trueHalfMapSize.y; y++)
                if (x < -halfMapSize.x || x >= halfMapSize.x
                 || y < -halfMapSize.y || y >= halfMapSize.y)
                    resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false);

        // 3. Collect all playable tiles and sort by distance to (0,0)
        var tiles = new List<Vector2Int>((halfMapSize.x * 2 + 1) * (halfMapSize.y * 2 + 1));

        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
                tiles.Add(new Vector2Int(x, y));

        tiles.Sort((a, b) =>
        {
            int da = a.x * a.x + a.y * a.y;
            int db = b.x * b.x + b.y * b.y;
            return da.CompareTo(db);          // inner-first, outer-last
        });

        int noResSq = noResourceZoneSize * noResourceZoneSize;

        // 4. Apply existing prefab-matching logic in the new order
        foreach (var pos in tiles)
        {
            int x = pos.x;
            int y = pos.y;

            int distSq = x * x + y * y;
            if (terrainMap[x, y] == voidUnitProperty)
            {
                resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false);
                continue;       // water
            }

            if (distSq <= noResSq)
            {
                resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false);
                continue;       // origin-spawn zone
            }

            var elevation = elevationMap.RawMap[x, y];
            var moisture = moistureMap.RawMap[x, y];
            var resource = resourceMap.RawMap[x, y];

            bool matched = false;

            foreach (var property in resourceProperties)
            {
                if (!property.Match(elevation, moisture, resource, out var prefab, out var maxCount))
                    continue;

                if (maxCount > 0 &&
                    prefabCount.TryGetValue(prefab, out int cnt) &&
                    cnt >= maxCount)
                {
                    var fallback = property.GetNotLimitedPrefab();
                    resourceStatusMap[x, y] = new ObservablePrefabStatus(fallback, fallback != null);
                }
                else
                {
                    resourceStatusMap[x, y] = new ObservablePrefabStatus(prefab, true);
                    prefabCount[prefab] = prefabCount.TryGetValue(prefab, out int c) ? c + 1 : 1;

                    if (maxCount > 0) Debug.Log($" Spawning resource {prefab.name} at ({x}, {y}), count: {prefabCount[prefab]}");
                }

                matched = true;
                break;
            }

            if (!matched)
                resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false);

            if (resourceStatusMap[x, y].prefab == hypnoFrogPrefab)
                HypnoFrogManager.Main.RequestAddToList(new Vector2(x, y));
        }

        // 5. Optional: log counts
        if (showDebugs)
            foreach (var kvp in prefabCount)
                Debug.Log($"Resource {kvp.Key.name} spawned {kvp.Value} times.");
    }


    /*private void GenerateResources()
    {
        resourceStatusMap = new Offset2DArray<ObservablePrefabStatus>(-trueHalfMapSize.x, trueHalfMapSize.x, -trueHalfMapSize.y, trueHalfMapSize.y);

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
                    if (terrainMap[x, y] != voidUnitProperty                            // No resources in water
                        && dx * dx + dy * dy > noResourceZoneSize * noResourceZoneSize) // Outside of no resource zone (around player spawn point)
                    {
                        var elevation = elevationMap.RawMap[x, y];
                        var moisture = moistureMap.RawMap[x, y];
                        var resource = resourceMap.RawMap[x, y];

                        var matched = false;
                        foreach (var property in resourceProperties)
                        {
                            if (property.Match(elevation, moisture, resource, out var prefab, out var maxCount))
                            {
                                if (maxCount > 0)
                                {
                                    // If the prefab has maxCount > 0, we need to track how many times it has been spawned
                                    if (prefabCount.ContainsKey(prefab))
                                    {
                                        if (prefabCount[prefab] >= maxCount)
                                        {
                                            // If the prefab count exceeds maxCount, we need to fallback to a different prefab
                                            var fallbackPrefab = property.GetNot(prefab);
                                            if (fallbackPrefab != null)
                                                resourceStatusMap[x, y] = new ObservablePrefabStatus(fallbackPrefab, true); // Fallback                                            
                                            else
                                                resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false); // No resource, this should not happen 
                                        }
                                        else
                                        {
                                            // If the prefab count is within the limit, we can spawn it
                                            resourceStatusMap[x, y] = new ObservablePrefabStatus(prefab, true);
                                            prefabCount[prefab]++;  // Increment count
                                            Debug.Log($" Spawning resource {prefab.name} at ({x}, {y}), count: {prefabCount[prefab]}");
                                        }
                                    }
                                    else
                                    {
                                        // If the prefab is not in the count dictionary, we can spawn it
                                        resourceStatusMap[x, y] = new ObservablePrefabStatus(prefab, true);
                                        prefabCount[prefab] = 1; // Initialize count
                                        Debug.Log($" Spawning resource {prefab.name} at ({x}, {y}), count: {prefabCount[prefab]}");
                                    }
                                }
                                else
                                {
                                    // If the prefab has maxCount == 0, we can spawn it without tracking count
                                    resourceStatusMap[x, y] = new ObservablePrefabStatus(prefab, true);
                                    if (prefabCount.ContainsKey(prefab))
                                        prefabCount[prefab]++;
                                    else
                                        prefabCount[prefab] = 1; // Initialize count
                                }
                                matched = true; // We found a matching property
                                continue;
                            }
                        }

                        // If no property matched, we set it to no resource
                        if (!matched) resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false); // No resource
                    }
                    else
                    {
                        resourceStatusMap[x, y] = new ObservablePrefabStatus(null, false); // No resource                    
                    }

                    Debug.Assert(resourceStatusMap[x, y] != null, $"Missing assignment at {x}, {y}");
                }
            }
        }

        foreach (var prefab in prefabCount.Keys)
        {
            Debug.Log($"Resource {prefab.name} has been spawned {prefabCount[prefab]} times.");
        }
    }*/
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

    #region Generate Chihuahua Rescue Positions
    private List<Vector2> chihuahuaRescuePositions;
    public Vector2 RandomChihuahuaRescuePosition => chihuahuaRescuePositions.GetRandomElement();
    private void GenerateChihuahuaRescuePositions()
    {
        chihuahuaRescuePositions = new List<Vector2>();
        chihuahuaRescuePositions.Capacity = mapSize.x;
        for (int x = -halfMapSize.x; x < halfMapSize.x + 1; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y + 1; y++)
            {
                if (elevationMap.RawMap[x, y] > chihuahuaElevation.min
                    && elevationMap.RawMap[x, y] <= chihuahuaElevation.max)
                    chihuahuaRescuePositions.Add(new Vector2(x, y));
            }
        }
    }
    #endregion

    #region World Building
    private Vector2 closetChunkPosition_cached = Vector2.one;
    [SerializeField]
    private bool isGenerating = false;
    public bool IsGenerating => isGenerating;
    private Vector2 cached_position;
    private float traveledDistance = 0;

    private Coroutine buildTerrainCoroutine;
    private Coroutine removeTerrainCoroutine;

    private readonly Dictionary<Vector2Int, TileData> tiles = new Dictionary<Vector2Int, TileData>();

    public void BuildWorld(Vector2 position)
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

        // If BuildTerrainCoroutine not started, start it
        if (buildTerrainCoroutine == null) buildTerrainCoroutine = StartCoroutine(BuildTerrain());
    }

    public IEnumerator BuildTerrain()
    {
        // Assume the position has been snapped in BuildWorld
        // position = position.SnapToGrid();

        while (true)
        {
            yield return null;
            var closetChunkPosition = cached_position.SnapToGrid(chunkSize, true).ToInt();
            if (closetChunkPosition_cached != closetChunkPosition)
            {
                closetChunkPosition_cached = closetChunkPosition;
            }
            else
            {
                continue; // If the position has not changed, skip the generation
            }

            if (isGenerating) continue; // If already generating, skip the generation
            isGenerating = true;

            GetActiveArea(closetChunkPosition, out int xMin, out int xMax, out int yMin, out int yMax);

            yield return BuildTerrainInternal(xMin, xMax, yMin, yMax, closetChunkPosition);

            isGenerating = false;

            if (removeTerrainCoroutine != null) StopCoroutine(removeTerrainCoroutine);
            removeTerrainCoroutine = StartCoroutine(RemoveTerrainCoroutine(xMin, xMax, yMin, yMax));
        }

        //if (showDebug) Debug.Log("Child count = " + transform.childCount);
    }

    /// <summary>
    /// Pre-computes the inclusive bounds of the area that should stay loaded.
    /// </summary>
    private void GetActiveArea(Vector2Int closestChunkPos,
                               out int xMin, out int xMax,
                               out int yMin, out int yMax)
    {
        xMin = closestChunkPos.x - (renderDistance + renderXOffset) * chunkSize;
        xMax = closestChunkPos.x + (renderDistance + renderXOffset) * chunkSize;
        yMin = closestChunkPos.y - renderDistance * chunkSize;
        yMax = closestChunkPos.y + renderDistance * chunkSize;
    }

    private IEnumerator BuildTerrainInternal(int xMin, int xMax, int yMin, int yMax, Vector2Int center)
    {
        int step = 0;
        int maxRadius = Mathf.Max(center.x - xMin,  // distance to each edge
                                  xMax - center.x,
                                  center.y - yMin,
                                  yMax - center.y);

        // r = 0 handles the centre tile; r > 0 walks the square "ring" at radius r
        for (int r = 0; r <= maxRadius; r++)
        {
            int left = center.x - r;
            int right = center.x + r;
            int bottom = center.y - r;
            int top = center.y + r;

            // Top & bottom rows
            for (int x = left; x <= right; x++)
            {
                var built = false;
                built = TryBuild(new Vector2Int(x, bottom), xMin, xMax, yMin, yMax);            // bottom
                if (r > 0) built = TryBuild(new Vector2Int(x, top), xMin, xMax, yMin, yMax);    // top (skip duplicate when r == 0)
                if (built && (showStep || (++step >= 10))) { step = 0; yield return null; }
            }

            // Left & right columns (excluding corners already done)
            for (int y = bottom + 1; y <= top - 1; y++)
            {
                var built = false;
                TryBuild(new Vector2Int(left, y), xMin, xMax, yMin, yMax);              // left
                if (r > 0) TryBuild(new Vector2Int(right, y), xMin, xMax, yMin, yMax);  // right
                if (built && (showStep || (++step >= 10))) { step = 0; yield return null; }
            }
        }
    }

    private bool TryBuild(Vector2Int pos, int xMin, int xMax, int yMin, int yMax)
    {
        // Outside the allowed rectangle?  Bail early.
        if (pos.x < xMin || pos.x > xMax || pos.y < yMin || pos.y > yMax)
            return false;

        // Already built?
        if (tiles.ContainsKey(pos))
            return false;

        // Terrain
        var property = GetMappedProperty(pos.x, pos.y);
        var terrainGO = SpawnTerrainUnit(pos, property);

        // Foliage (optional)
        GameObject foliageGO = null;
        folliageMap.GetElementSafe(pos.x, pos.y, out var validFoliage);
        if (canSpawnFoliage && validFoliage &&
            !invalidFoliagePositionHashSet.Contains(pos) &&
            resourceMap.RawMap[pos.x, pos.y] < 0.99f)
        {
            foliageGO = SpawnFoliage(pos, foliagePrefabs.GetRandomElement());
        }

        // Commit to the dictionary
        tiles[pos] = new TileData { terrain = terrainGO, foliage = foliageGO };
        return true;
    }

    private readonly List<Vector2Int> keysBuffer = new List<Vector2Int>();
    private IEnumerator RemoveTerrainCoroutine(int xMin, int xMax, int yMin, int yMax)
    {
        int step = 0;
        // Collect keys that fall outside the active rectangle
        keysBuffer.Clear();
        foreach (var kv in tiles)
        {
            var p = kv.Key;
            if (p.x < xMin || p.x > xMax || p.y < yMin || p.y > yMax)
                keysBuffer.Add(p);
        }

        // Despawn & delete
        for (int i = 0; i < keysBuffer.Count; i++)
        {
            var key = keysBuffer[i];
            var tile = tiles[key];

            if (tile.terrain) LocalObjectPooling.Main.Despawn(tile.terrain, true);
            if (tile.foliage) LocalObjectPooling.Main.Despawn(tile.foliage, true);

            tiles.Remove(key);

            // showStep: Yield every frame may slow down generation, but useful for debugging
            // normal: Yield every 10 iterations to prevent freezing the main thread
            if (showStep || (++step >= tileStep)) { step = 0; yield return null; }
        }
    }
    #endregion

    #region Resource Building
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
                        var offset = resourceStatus.prefab.GetComponent<ObservabilityController>().SpawnOffset;
                        var resObj = SpawnResource(resourcePos + offset, resourceStatus.prefab);
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
                if (spawnedResources[item] != null && spawnedResources[item].isSpawned && spawnedResources[item].controller != null)
                {
                    spawnedResources[item].controller.UnloadOnServer();
                    spawnedResources.Remove(item);
                }
            }
        }
    }
    #endregion

    #region Spawn Methods
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

    public void RemoveFoliageOnClient(Vector2 position)
    {
        // Snap position to cell position
        var positionInt = position.SnapToGrid().ToInt();

        // Tile look up
        if (!tiles.TryGetValue(positionInt, out var tile)) return;

        // Verify & despawn
        if (tile.foliage != null)
        {
            LocalObjectPooling.Main.Despawn(tile.foliage);
            tile.foliage = null;                    // clear the reference

            // TilePair is a *struct*, so write it back
            tiles[positionInt] = tile;
        }

        // Remove from invalid foliage positions
        InvalidateFolliageOnClient(position);
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

        if (chihuahuaRescuePositions != null && chihuahuaRescuePositions.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (var pos in chihuahuaRescuePositions)
            {
                Gizmos.DrawSphere(pos, 0.2f);
            }
        }

        //Gizmos.color = Color.red;
        //Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x, mapSize.y, 0));
        //Gizmos.DrawWireCube(Vector3.zero, new Vector3(trueMapSize.x, trueMapSize.y, 0));
    }
#endif
}