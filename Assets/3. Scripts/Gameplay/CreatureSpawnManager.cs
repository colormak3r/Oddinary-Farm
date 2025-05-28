using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Splines.SplineInstantiate;

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
    private int baseSpawnRadius = 10;
    [SerializeField]
    private int baseSafeRadius = 10;
    [SerializeField]
    private LayerMask spawnBlockLayer;

    [Header("Testing")]
    [SerializeField]
    private CreatureWave testCreatureWave;

    [Header("Debugs")]
    [SerializeField]
    private bool showGizmos = false; // Show gizmos in the editor
    [SerializeField]
    private int currentSafeRadius = 0;
    [SerializeField]
    private int currentSpawnRadius = 0;

    private TimeManager timeManager;
    private List<Vector2> spawnablePositions;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentSafeRadius = baseSafeRadius;
            currentSpawnRadius = baseSpawnRadius + currentSafeRadius;
        }

        timeManager = TimeManager.Main;
        timeManager.OnHourChanged.AddListener(OnHourChanged);
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
                SpawnWaveOnServer(wave, Vector2.zero, currentSafeRadius, currentSpawnRadius);
            }
            else if (currentHour == wave.spawnHour - 1 && wave.showWarning)
            {
                WarningUI.Main.ShowWarning($"Breach Detected");
            }
        }
    }

    public void SpawnWaveOnServer(CreatureWave wave, Vector2 position, int safeRadius, int spawnRadius)
    {
        if (!IsServer) return;

        StartCoroutine(SpawnWaveCoroutine(wave, position, safeRadius, spawnRadius));
    }

    private IEnumerator SpawnWaveCoroutine(CreatureWave wave, Vector2 position, int safeRadius, int spawnRadius)
    {
        spawnablePositions = GetSpawnPositions(position, safeRadius, spawnRadius);

        foreach (var spawn in wave.creatureSpawns)
        {
            SpawnCreature(spawn, spawnablePositions);
            yield return new WaitForSeconds(spawnDelay); // Optional delay between spawns
        }
    }

    public List<GameObject> SpawnWaveInstantlyOnServer(CreatureWave wave, Vector2 position, int safeRadius, int spawnRadius)
    {
        if (!IsServer) return null;

        spawnablePositions = GetSpawnPositions(position, safeRadius, spawnRadius);

        var creatures = new List<GameObject>();
        foreach (var spawn in wave.creatureSpawns)
        {
            creatures.AddRange(SpawnCreature(spawn, spawnablePositions));
        }
        return creatures;
    }

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

    private List<GameObject> SpawnCreature(CreatureSpawn spawn, List<Vector2> spawnablePositions)
    {
        List<GameObject> creatures = new List<GameObject>();
        for (int i = 0; i < spawn.spawnCount; i++)
        {
            var creature = Instantiate(spawn.creaturePrefab, spawnablePositions.GetRandomElement(), Quaternion.identity);
            creature.GetComponent<NetworkObject>().Spawn();
            creatures.Add(creature);
        }
        return creatures;
    }

    public void UpdateSafeRadius(int value)
    {
        if (value < currentSafeRadius) return;

        currentSafeRadius = value;
        currentSpawnRadius = value + baseSpawnRadius;
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
        SpawnWaveOnServer(testCreatureWave, position, safeRadius, spawnRadius);
    }

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
