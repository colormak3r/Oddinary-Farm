using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalObjectPooling : MonoBehaviour
{
    public static LocalObjectPooling Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(Main.gameObject);
    }

    public GameObject Spawn(GameObject prefab)
    {
        if (!prefab.TryGetComponent<LocalObjectController>(out var controller))
        {
            Debug.LogError("Prefab does not have LocalObjectController component.", prefab);
        }

        var pool = GetPool(controller.Guid);
        if (pool == null)
        {
            pool = new GameObject(controller.Guid).transform;
            pool.gameObject.AddComponent<PoolMonitor>();
            pool.SetParent(transform);
        }

        if (pool.childCount == 0)
        {
            for (int i = 0; i < 10; i++)
            {
                var newObj = Instantiate(prefab, pool);
            }
        }

        var obj = pool.GetChild(0).gameObject;
        obj.GetComponent<LocalObjectController>().LocalSpawn();
        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (!obj.TryGetComponent<LocalObjectController>(out var controller))
        {
            Debug.LogError("Prefab does not have LocalObjectController component.", obj);
        }
        controller.LocalDespawn();
        var pool = GetPool(controller.Guid);
        if (pool)
        {
            obj.transform.SetParent(pool);
        }
        else
        {
            Debug.LogError("No pool found for " + obj.name);
        }
    }

    private Transform GetPool(string poolGuid)
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains(poolGuid))
                return child;
        }
        return null;
    }
}
