using ColorMak3r.Utility;
using System;
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
    private int baseSpawnRadius = 10;
    [SerializeField]
    private int baseSafeRadius = 10;
    [SerializeField]
    private LayerMask spawnBlockLayer;

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
            timeManager = TimeManager.Main;
            timeManager.OnHourChanged.AddListener(OnHourChanged);
            currentSafeRadius = baseSafeRadius;
            currentSpawnRadius = baseSpawnRadius + currentSafeRadius;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            timeManager.OnHourChanged.RemoveListener(OnHourChanged);
    }

    private void OnHourChanged(int currentHour)
    {
        if (!canSpawn) return;

        var currentDay = timeManager.CurrentDay;
        var currentDaySetting = creatureSpawnSetting.creatureDays[creatureSpawnSetting.creatureDays.Length - 1];
        if (currentDay < creatureSpawnSetting.creatureDays.Length) currentDaySetting = creatureSpawnSetting.creatureDays[currentDay];

        foreach (var wave in currentDaySetting.creatureWaves)
        {
            if (currentHour == wave.spawnHour)
            {
                StartCoroutine(SpawnWave(wave));
            }
        }
    }

    private IEnumerator SpawnWave(CreatureWave wave)
    {
        spawnablePositions = new List<Vector2>();
        for (int x = -currentSpawnRadius; x < currentSpawnRadius; x++)
        {
            for (int y = -currentSpawnRadius; y < currentSpawnRadius; y++)
            {
                if (Mathf.Abs(x) <= currentSafeRadius && Mathf.Abs(y) <= currentSafeRadius) continue;
                var position = new Vector2(x, y);
                position = position.SnapToGrid();
                if (Physics2D.OverlapPoint(position, spawnBlockLayer) == null)
                {
                    spawnablePositions.Add(position);
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
