using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField]
    private int spawnIndex;
    [SerializeField]
    private GameObject itemPrefab;
    [SerializeField]
    private ItemProperty[] itemProperties;

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        var randomPos = 2 * (Vector2)Random.onUnitSphere;
        GameObject go = Instantiate(itemPrefab, transform.position + (Vector3)randomPos, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var item = go.GetComponent<Item>();
        item.Initialize(itemProperties[spawnIndex]);
    }

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        foreach(var property in itemProperties)
        {
            var randomPos = 2 * (Vector2)Random.onUnitSphere;
            GameObject go = Instantiate(itemPrefab, transform.position + (Vector3)randomPos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
            var item = go.GetComponent<Item>();
            item.Initialize(property);
        }
    }
}
