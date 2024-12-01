using System.Collections;
using Unity.Netcode;
using UnityEngine;
using ColorMak3r.Utility;

public enum PrefabSpawnType
{
    Spider,
    Snail
}

public class PrefabSpawner : MonoBehaviour
{
    public static PrefabSpawner Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    [Header("Settings")]
    [SerializeField]
    private GameObject spiderPrefab; // Spider prefab
    [SerializeField]
    private GameObject snailPrefab;  // Snail prefab

    public void Spawn(PrefabSpawnType type, Vector2 position, int spawnCount = 1, float randomRange = 1, float spawnDelay = 0.25f)
    {
        SpawnPrefabRpc(type, position, spawnCount, randomRange, spawnDelay);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPrefabRpc(PrefabSpawnType type, Vector2 position, int spawnCount, float randomRange, float spawnDelay)
    {
        StartCoroutine(SpawnPrefabCoroutine(type, position, spawnCount, randomRange, spawnDelay));
    }

    private IEnumerator SpawnPrefabCoroutine(PrefabSpawnType type, Vector2 position, int spawnCount, float randomRange, float spawnDelay)
    {
        var prefab = GetPrefab(type);
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject go = Instantiate(prefab, position.RandomPointInRange(randomRange), Quaternion.identity); // Use the selected prefab
            go.GetComponent<NetworkObject>().Spawn();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private GameObject GetPrefab(PrefabSpawnType type)
    {
        switch (type)
        {
            case PrefabSpawnType.Spider:
                return spiderPrefab;
            case PrefabSpawnType.Snail:
                return snailPrefab;
            default:
                return null;
        }
    }
}
