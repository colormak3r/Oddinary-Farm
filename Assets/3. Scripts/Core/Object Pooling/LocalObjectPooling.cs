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
            var newObj = Instantiate(prefab, pool);
            newObj.GetComponent<LocalObjectController>().LocalDespawn(true);
        }

        var obj = pool.GetChild(0).gameObject;
        obj.GetComponent<LocalObjectController>().LocalSpawn(pool, false);
        obj.transform.SetParent(null);
        return obj;
    }

    public void Despawn(GameObject obj, bool instant = false)
    {
        if (!obj.TryGetComponent<LocalObjectController>(out var controller))
        {
            Debug.LogError("Object does not have LocalObjectController component.", obj);
            return;
        }

        controller.LocalDespawn(instant);
        obj.transform.SetParent(controller?.Pool);
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
