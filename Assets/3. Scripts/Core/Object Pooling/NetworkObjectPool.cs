using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkObjectPool : MonoBehaviour
{
    public static NetworkObjectPool Main;

    private Dictionary<string, List<GameObject>> guidToPool = new Dictionary<string, List<GameObject>>();

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(Main.gameObject);
    }

    public GameObject Spawn(GameObject prefab, Vector2 position)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Spawn can only be called on the server.");
            return null;
        }

        if (!prefab.TryGetComponent<NetworkObjectPoolController>(out var controller))
        {
            Debug.LogError("Prefab does not have NetworkObjectPoolController component.", prefab);
            return null;
        }

        if (!guidToPool.ContainsKey(controller.Guid))
        {
            guidToPool[controller.Guid] = new List<GameObject>();
        }

        var pool = guidToPool[controller.Guid];

        if (pool.Count == 0)
        {
            for (int i = 0; i < 10; i++)
            {
                var newObj = Instantiate(prefab);
                newObj.GetComponent<NetworkObject>().Spawn();
                pool.Add(newObj);
            };
        }

        var obj = pool[0];
        pool.RemoveAt(0);
        if(obj.TryGetComponent<NetworkTransform>(out var netTransform)) netTransform.Teleport(position, Quaternion.identity, Vector3.one);
        obj.GetComponent<NetworkObjectPoolController>().NetworkSpawn();
        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Despawn can only be called on the server.");
            return;
        }

        if (!obj.TryGetComponent<NetworkObjectPoolController>(out var controller))
        {
            Debug.LogError("Object does not have NetworkObjectPoolController component.", obj);
            return;
        }

        obj.GetComponent<NetworkObjectPoolController>().NetworkDespawn();
        guidToPool[controller.Guid].Add(obj);
    }
}
