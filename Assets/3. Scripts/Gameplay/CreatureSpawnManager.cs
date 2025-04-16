using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
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
        if (currentDay < creatureSpawnSetting.creatureDays.Length) currentDaySetting = creatureSpawnSetting.creatureDays[currentDay];

        foreach (var wave in currentDaySetting.creatureWaves)
        {
            if (currentHour == wave.spawnHour)
            {
                if (IsServer)
                    StartCoroutine(SpawnWave(wave, Vector2.zero, currentSafeRadius, currentSpawnRadius));
            }
            else if (currentHour == wave.spawnHour - 1 && wave.showWarning)
            {
                WarningUI.Main.ShowWarning($"Breach Detected");
            }
        }
    }

    private IEnumerator SpawnWave(CreatureWave wave, Vector2 position, int safeRadius, int spawnRadius)
    {
        spawnablePositions = new List<Vector2>();
        for (int x = -spawnRadius; x < spawnRadius; x++)
        {
            for (int y = -spawnRadius; y < spawnRadius; y++)
            {
                if (Mathf.Abs(x) <= safeRadius && Mathf.Abs(y) <= safeRadius) continue;
                var offsetPos = new Vector2(x, y) + position;
                offsetPos = offsetPos.SnapToGrid();
                if (Physics2D.OverlapPoint(offsetPos, spawnBlockLayer) == null)
                {
                    spawnablePositions.Add(offsetPos);
                }
            }
        }

        foreach (var spawn in wave.creatureSpawns)
        {
            SpawnCreature(spawn, spawnablePositions);
            yield return new WaitForSeconds(spawnDelay); // Optional delay between spawns
        }
    }

    private void SpawnCreature(CreatureSpawn spawn, List<Vector2> spawnablePositions)
    {
        for (int i = 0; i < spawn.spawnCount; i++)
        {
            var creature = Instantiate(spawn.creaturePrefab, spawnablePositions.GetRandomElement(), Quaternion.identity);
            creature.GetComponent<NetworkObject>().Spawn();
        }
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
        StartCoroutine(SpawnWave(testCreatureWave, position, safeRadius, spawnRadius));
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
