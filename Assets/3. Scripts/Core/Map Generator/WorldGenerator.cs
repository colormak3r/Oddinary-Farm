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
    private TerrainUnitProperty sandUnitProperty;
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
    public float OceanElevationThreshold => sandUnitProperty.Elevation.max;

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
        var elevation = GetElevation(snappedPosition.x, snappedPosition.y, true);
        elevationText.text = Mathf.RoundToInt((elevation - FloodManager.Main.BaseFloodLevel) * 1000) + "ft";
        AudioManager.Main.OnElevationUpdated(elevation);
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