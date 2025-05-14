using Unity.Netcode;
using UnityEngine;

public class CreatureSpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
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
        if (IsServer) SpawnWave();
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