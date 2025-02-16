using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AnimalManager : NetworkBehaviour
{
    [Header("Monster Settings")]
    [SerializeField]
    private bool canSpawn = false;
    [SerializeField]
    private int spawnHour = 20;
    [SerializeField]
    private int startDay = 1;
    [SerializeField]
    private float waveCooldown = 1;
    [SerializeField]
    private int waveTotalBase = 3;
    [SerializeField]
    private int waveTotalIncrement = 1;
    [SerializeField]
    private int spawnPerWaveBase = 5;
    [SerializeField]
    private int spawnPerWaveIncrement = 5;
    [SerializeField]
    private int bossPerSpawn = 10;
    [SerializeField]
    private Vector2 spawnCoordinate = new Vector2(0, 0);
    [SerializeField]
    private int spawnRadius = 20;
    [SerializeField]
    private int spawnBlockRadius = 10;
    [SerializeField]
    private float spawnCooldown = 0.2f;
    [SerializeField]
    private LayerMask spawnBlocker;
    [SerializeField]
    private GameObject monsterPrefab;
    [SerializeField]
    private GameObject monsterBossPrefab;

    [Header("Debugs")]
    [SerializeField]
    private int currentSpawnPerWave = 0;
    [SerializeField]
    private int currentTotalWave = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentSpawnPerWave = spawnPerWaveBase;
            currentTotalWave = waveTotalBase;
            TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
            StartCoroutine(SpawnMonsterWaves());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            TimeManager.Main.OnHourChanged.RemoveListener(OnHourChanged);
        }
    }

    private void OnHourChanged(int hour)
    {
        if (hour == spawnHour)
        {
            if (canSpawn && TimeManager.Main.CurrentDay >= startDay)
            {
                StartCoroutine(SpawnMonsterWaves());
                currentTotalWave += waveTotalIncrement;
                currentSpawnPerWave += spawnPerWaveIncrement * NetworkManager.ConnectedClients.Count;
            }
        }
    }

    private IEnumerator SpawnMonsterWaves()
    {
        var currentWave = 0;
        while (currentWave < currentTotalWave)
        {
            yield return SpawnMonsterCoroutine();
            currentWave++;
            var nextWave = Time.time + waveCooldown * TimeManager.Main.HourDuration;
            yield return new WaitForSeconds(nextWave);
        }
    }

    private IEnumerator SpawnMonsterCoroutine()
    {
        // Get all spawnable positions
        var spawnablePositions = new List<Vector2>();
        spawnablePositions.Capacity = spawnRadius * spawnRadius * 4;
        for (int x = -spawnRadius; x < spawnRadius; x++)
        {
            for (int y = -spawnRadius; y < spawnRadius; y++)
            {
                if (Mathf.Abs(x) > spawnBlockRadius || Mathf.Abs(y) > spawnBlockRadius) continue;
                var position = new Vector2(spawnCoordinate.x + x, spawnCoordinate.y + y);
                position = position.SnapToGrid();
                if (Physics2D.OverlapPoint(position, spawnBlocker) == null)
                {
                    spawnablePositions.Add(position);
                }
            }
        }
        yield return null;

        // Spawn monsters
        for (int i = 0; i < currentSpawnPerWave; i++)
        {
            var randomIndex = UnityEngine.Random.Range(0, spawnablePositions.Count);
            var randomPosition = spawnablePositions[randomIndex];

            var monster = Instantiate(monsterPrefab, randomPosition, Quaternion.identity);
            monster.GetComponent<NetworkObject>().Spawn();

            // Spawn boss
            if (i > 0 && i % bossPerSpawn == 0)
            {
                randomIndex = UnityEngine.Random.Range(0, spawnablePositions.Count);
                randomPosition = spawnablePositions[randomIndex];

                monster = Instantiate(monsterBossPrefab, randomPosition, Quaternion.identity);
                monster.GetComponent<NetworkObject>().Spawn();
            }

            yield return new WaitForSeconds(spawnCooldown);
        }
    }
}
