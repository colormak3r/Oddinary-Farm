/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CreatureSpawnManager : NetworkBehaviour
{
    public static CreatureSpawnManager Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Settings")]
    [SerializeField]
    private bool canSpawn = true;
    [SerializeField]
    private float spawnDelay = 0.2f; // Delay between spawns
    [SerializeField]
    private CreatureSpawnSetting creatureSpawnSetting;

    [Header("Spawn Settings")]
    [SerializeField]
    private LayerMask spawnBlockLayer;

    [Header("Wave Testing")]
    [SerializeField]
    private CreatureWave testCreatureWave;

    [Header("Spawn Testing")]
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private float testSpawnDelay = 0.5f;
    [SerializeField]
    private Vector2 spawnPosition = Vector2.zero;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false; // Show debug logs in the console
    [SerializeField]
    private bool showGizmos = false; // Show gizmos in the editor
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private TimeManager timeManager;
    private List<Vector2> spawnablePositions;

    public override void OnNetworkSpawn()
    {
        timeManager = TimeManager.Main;
        timeManager.OnHourChanged.AddListener(OnHourChanged);

        isInitialized = true;
    }

    public override void OnNetworkDespawn()
    {
        timeManager.OnHourChanged.RemoveListener(OnHourChanged);
    }

    private void OnHourChanged(int currentHour)
    {
        if (!canSpawn) return;

        var currentDay = timeManager.CurrentDate;
        var currentDaySetting = creatureSpawnSetting.creatureDays[creatureSpawnSetting.creatureDays.Length - 1];
        if (currentDay < creatureSpawnSetting.creatureDays.Length) currentDaySetting = creatureSpawnSetting.creatureDays[currentDay - 1];

        foreach (var wave in currentDaySetting.creatureWaves)
        {
            if (currentHour == wave.spawnHour)
            {
                if (IsServer)
                {
                    StartCoroutine(SpawnWaveOnServerCoroutine(wave));
                }
            }
            else if (currentHour == wave.spawnHour - 1 && wave.showWarning)
            {
                WarningUI.Main.ShowWarning();
            }
        }
    }

    private IEnumerator SpawnWaveOnServerCoroutine(CreatureWave wave)
    {
        yield return new WaitUntil(() => GameManager.Main.IsInitialized);

        Vector2 waveCenter = HeatMapManager.Main.HeatCenter;
        int radius = 10;
        int entitiesCount = 0;
        int entitiesCount_cached = 0;
        int playerCount = 0;

        while (radius < 100)
        {
            entitiesCount = 0;
            playerCount = 0;
            var hits = Physics2D.OverlapCircleAll(waveCenter, radius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out MapElement mapElement))
                {
                    entitiesCount++;
                    //Debug.Log($"Found entity: {mapElement.name} at {hit.transform.position} within radius {radius}");
                }

                if (hit.TryGetComponent(out PlayerStatus playerStatus))
                {
                    playerCount++;
                    //Debug.Log($"Found player: {playerStatus.name} at {hit.transform.position} within radius {radius}");
                }
            }

            if (showDebugs) Debug.Log($"Checking radius {radius}: Found {entitiesCount} entities, cached {entitiesCount_cached}");
            // Difference is less than 10%
            if (entitiesCount - entitiesCount_cached < entitiesCount * 0.1f)
            {
                break;
            }
            else
            {
                radius += 5;
                entitiesCount_cached = entitiesCount;
            }

            yield return null;
        }


        var multiplier = GetMultiplierFromPlayerCount(playerCount);
        //if (showDebugs) 
        Debug.Log($"Spawning wave: {wave} at {waveCenter} with radius {radius} and multiplier {multiplier}");
        SpawnWaveOnServer(wave, waveCenter, radius, radius + 5, multiplier);
    }

    #region Spawn Waves On Server

    public void SpawnWaveOnServer(CreatureWave wave, Vector2 position, int safeRadius, int spawnRadius, float multiplier)
    {
        if (!IsServer) return;

        StartCoroutine(SpawnWaveCoroutine(wave, position, safeRadius, spawnRadius, multiplier));
    }

    private IEnumerator SpawnWaveCoroutine(CreatureWave wave, Vector2 position, int safeRadius, int spawnRadius, float multiplier)
    {
        spawnablePositions = GetSpawnPositions(position, safeRadius, spawnRadius);

        foreach (var spawn in wave.creatureSpawns)
        {
            yield return SpawnCreatureCoroutine(spawn, spawnablePositions, multiplier, wave.headToHeadCenter);
        }
    }

    private IEnumerator SpawnCreatureCoroutine(CreatureSpawn spawn, List<Vector2> spawnablePositions, float multiplier, bool headToHeatCenter)
    {
        var scaledCount = Mathf.FloorToInt(spawn.spawnCount * multiplier);
        for (int i = 0; i < scaledCount; i++)
        {
            var creature = Instantiate(spawn.creaturePrefab, spawnablePositions.GetRandomElement(), Quaternion.identity);
            creature.GetComponent<NetworkObject>().Spawn();
            if (headToHeatCenter && creature.TryGetComponent<MoveTowardStimulus>(out var moveTowardStimulus))
            {
                moveTowardStimulus.SetTargetPositionOnServer(HeatMapManager.Main.HeatCenter);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
    #endregion

    #region Spawn Instantly
    public List<GameObject> SpawnWaveInstantlyOnServer(CreatureWave wave, Vector2 position, int safeRadius, int spawnRadius)
    {
        if (!IsServer) return null;

        spawnablePositions = GetSpawnPositions(position, safeRadius, spawnRadius);
        var multiplier = 1;

        var creatures = new List<GameObject>();
        foreach (var spawn in wave.creatureSpawns)
        {
            creatures.AddRange(SpawnCreatureInstantly(spawn, spawnablePositions, multiplier));
        }
        return creatures;
    }

    private List<GameObject> SpawnCreatureInstantly(CreatureSpawn spawn, List<Vector2> spawnablePositions, int multiplier)
    {
        List<GameObject> creatures = new List<GameObject>();
        var scaledCount = spawn.spawnCount * multiplier;
        for (int i = 0; i < scaledCount; i++)
        {
            var creature = Instantiate(spawn.creaturePrefab, spawnablePositions.GetRandomElement(), Quaternion.identity);
            creature.GetComponent<NetworkObject>().Spawn();
            creatures.Add(creature);
        }
        return creatures;
    }
    #endregion

    #region Utilities
    private List<Vector2> GetSpawnPositions(Vector2 position, int safeRadius, int spawnRadius)
    {
        position = position.SnapToGrid();
        var pos = new List<Vector2>();
        for (int x = -spawnRadius; x < spawnRadius; x++)
        {
            for (int y = -spawnRadius; y < spawnRadius; y++)
            {
                if (safeRadius != 0 && Mathf.Abs(x) <= safeRadius && Mathf.Abs(y) <= safeRadius) continue;
                var offsetPos = new Vector2(x, y) + position;
                if (Physics2D.OverlapPoint(offsetPos, spawnBlockLayer) == null)
                {
                    pos.Add(offsetPos);
                }
            }
        }
        return pos;
    }

    public void SetCanSpawn(bool canSpawn)
    {
        SetCanSpawnRpc(canSpawn);
    }

    [Rpc(SendTo.Server)]
    private void SetCanSpawnRpc(bool canSpawn)
    {
        this.canSpawn = canSpawn;
    }

    public void SpawnTestWave(Vector2 position, int safeRadius, int spawnRadius)
    {
        SpawnTestWaveRpc(position, safeRadius, spawnRadius);
    }

    [Rpc(SendTo.Server)]
    private void SpawnTestWaveRpc(Vector2 position, int safeRadius, int spawnRadius)
    {
        SpawnWaveOnServer(testCreatureWave, position, safeRadius, spawnRadius, 1);
    }

    private float GetMultiplierFromPlayerCount(int playerCount)
    {
        // y = 3-4*2^-x
        // x, y
        // 1, 1
        // 2, 2
        // 3, 2.5
        // 4, 2.75
        // 5, ...

        return 3 - 4 * Mathf.Pow(2, -playerCount);
    }

    #endregion

    #region Spawn Testing

    private bool isSpawningTestCreature = false;

    [ContextMenu("Spawn Test Creature")]
    private void SpawnTestCreature()
    {
        if (!IsServer) return;

        isSpawningTestCreature = !isSpawningTestCreature;

        if (isSpawningTestCreature)
            StartCoroutine(SpawnTestCreatureCoroutine());
        else
            StopCoroutine(SpawnTestCreatureCoroutine());
    }

    private IEnumerator SpawnTestCreatureCoroutine()
    {
        while (isSpawningTestCreature)
        {
            var creature = Instantiate(prefab, spawnPosition, Quaternion.identity);
            creature.GetComponent<NetworkObject>().Spawn();

            yield return new WaitForSeconds(testSpawnDelay);
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (spawnablePositions != null && spawnablePositions.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (var position in spawnablePositions)
            {
                Gizmos.DrawSphere(position, 0.1f);
            }
        }
    }
}
