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
    private int wave = 5;
    [SerializeField]
    private int spawnPerWaveBase = 5;
    [SerializeField]
    private Vector2 spawnCoordinate = new Vector2(0, 0);
    [SerializeField]
    private int spawnRadius = 20;
    [SerializeField]
    private LayerMask spawnBlocker;
    [SerializeField]
    private GameObject monsterPrefab;
    [SerializeField]
    private GameObject monsterBossPrefab;

    private int currentWave = 0;
    private int currentSpawnPerWave = 0;
    private bool hourchanged = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentSpawnPerWave = spawnPerWaveBase;
            TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
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
        if (hour == spawnHour && TimeManager.Main.CurrentDay > 2)
        {
            if (canSpawn)
                StartCoroutine(SpawnMonsterWaves());
            currentSpawnPerWave += 2;
        }

        hourchanged = true;
    }

    private IEnumerator SpawnMonsterWaves()
    {
        currentWave = 0;
        while (currentWave < wave)
        {
            hourchanged = false;
            yield return SpawnMonsterCoroutine();
            currentWave++;
            yield return new WaitUntil(() => hourchanged);
        }
    }

    private IEnumerator SpawnMonsterCoroutine()
    {
        var spawnablePositions = new List<Vector2>();
        spawnablePositions.Capacity = spawnRadius * spawnRadius * 4;

        for (int x = -spawnRadius; x < spawnRadius; x++)
        {
            for (int y = -spawnRadius; y < spawnRadius; y++)
            {
                var position = new Vector2(spawnCoordinate.x + x, spawnCoordinate.y + y);
                position = position.SnapToGrid();
                if (Physics2D.OverlapPoint(position, spawnBlocker) == null)
                {
                    spawnablePositions.Add(position);
                }
            }
        }
        yield return null;

        for (int i = 0; i < currentSpawnPerWave; i++)
        {
            var randomIndex = UnityEngine.Random.Range(0, spawnablePositions.Count);
            var randomPosition = spawnablePositions[randomIndex];

            var monster = Instantiate(monsterPrefab, randomPosition, Quaternion.identity);
            monster.GetComponent<NetworkObject>().Spawn();

            yield return null;
        }

        if (TimeManager.Main.CurrentDay % 5 == 0)
        {
            for (int i = 0; i < currentSpawnPerWave / 5; i++)
            {
                var randomIndex = UnityEngine.Random.Range(0, spawnablePositions.Count);
                var randomPosition = spawnablePositions[randomIndex];

                var monster = Instantiate(monsterBossPrefab, randomPosition, Quaternion.identity);
                monster.GetComponent<NetworkObject>().Spawn();

                yield return null;
            }
        }       
    }
}
