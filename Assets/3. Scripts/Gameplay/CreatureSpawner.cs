using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CreatureSpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField]
    private bool canSpawn = true;
    [SerializeField]
    private bool destroyCreaturesOnDespawn = true;
    [SerializeField]
    private CreatureWave creatureWave;
    [SerializeField]
    private int safeRadius = 1;
    [SerializeField]
    private int spawnRadius = 3;

    [Header("Guard Settings")]
    [SerializeField]
    private bool guardLocation = true;

    private List<GameObject> creatureList = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer && canSpawn) StartCoroutine(SpawnWaveCoroutine());
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && destroyCreaturesOnDespawn)
        {
            foreach (var creature in creatureList)
            {
                if (creature != null)
                {
                    NetworkObject networkObject = creature.GetComponent<NetworkObject>();
                    if (networkObject != null && networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                }
            }
        }
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        yield return new WaitUntil(() => CreatureSpawnManager.Main.IsInitialized);
        SpawnWave();
    }

    [ContextMenu("Spawn Wave")]
    private void SpawnWave()
    {
        if (guardLocation)
        {
            creatureList = CreatureSpawnManager.Main.SpawnWaveInstantlyOnServer(creatureWave, transform.position, safeRadius, spawnRadius);
            foreach (var creature in creatureList)
            {
                if (creature.TryGetComponent<MoveTowardStimulus>(out var moveTowardStimulus))
                {
                    moveTowardStimulus.SetTargetPositionOnServer(transform.position);
                    moveTowardStimulus.SetGuardMode(true);
                }
            }
        }
        else
        {
            CreatureSpawnManager.Main.SpawnWaveOnServer(creatureWave, transform.position, safeRadius, spawnRadius);
        }
    }
}