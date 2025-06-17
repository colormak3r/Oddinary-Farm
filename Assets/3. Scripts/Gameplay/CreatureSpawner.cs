using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CreatureSpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField]
    private bool canSpawn = true;
    [SerializeField]
    private CreatureWave creatureWave;
    [SerializeField]
    private int safeRadius = 1;
    [SerializeField]
    private int spawnRadius = 3;

    [Header("Guard Settings")]
    [SerializeField]
    private bool guardLocation = true;

    public override void OnNetworkSpawn()
    {
        if (IsServer && canSpawn) StartCoroutine(SpawnWaveCoroutine());
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
            var creatures = CreatureSpawnManager.Main.SpawnWaveInstantlyOnServer(creatureWave, transform.position, safeRadius, spawnRadius);
            foreach (var creature in creatures)
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