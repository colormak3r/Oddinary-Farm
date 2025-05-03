using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct ItemID
{
    public int id;
    public ItemProperty itemProperty;

    public ItemID(int id, ItemProperty itemProperty)
    {
        this.id = id;
        this.itemProperty = itemProperty;
    }
}

[Serializable]
public struct SpawnableID
{
    public int id;
    public GameObject prefab;

    public SpawnableID(int id, GameObject prefab)
    {
        this.id = id;
        this.prefab = prefab;
    }
}

public class AssetManager : NetworkBehaviour
{
    public static AssetManager Main;

    private static int ITEM_ID_OFFSET = 10000;
    private static int SPAWNABLE_ID_OFFSET = 20000;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(Main.gameObject);

#if UNITY_EDITOR
        // Scan and fetch all assets in the specified folder. In Editor mode only.
        FetchAssets();
#endif
        BuildAssetDictionary();
        //PopulateCurrencyDictionary();
    }

    [Header("Settings")]
    [SerializeField]
    private string assetPath;
    [SerializeField]
    private string spawnablePath;
    [SerializeField]
    private string itemPath;

    [Header("Common Assets")]
    [SerializeField]
    private GameObject farmPlotPrefab;
    public GameObject FarmPlotPrefab => farmPlotPrefab;
    [SerializeField]
    private GameObject itemReplicaPrefab;
    public GameObject ItemReplicaPrefab => itemReplicaPrefab;
    [SerializeField]
    private GameObject projectilePrefab;
    public GameObject ProjectilePrefab => projectilePrefab;

    [Header("Scriptable Object Assets")]
    [SerializeField]
    private List<ScriptableObject> scriptableObjectList = new List<ScriptableObject>();
    private Dictionary<string, ScriptableObject> nameToScriptableObject = new Dictionary<string, ScriptableObject>();

    [Header("Item Properties")]
    [SerializeField]
    private List<ItemID> itemIds = new List<ItemID>();
    [SerializeField]
    private List<SpawnableID> spawnableIds = new List<SpawnableID>();

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

#if UNITY_EDITOR
    [ContextMenu("Fetch Assets")]
    public void FetchAssets()
    {
        scriptableObjectList.Clear();
        itemIds.Clear();
        spawnableIds.Clear();

        var assets = LoadAllScriptableObjectsInFolder<ScriptableObject>(assetPath);
        string assetNames = "Fetched assets:";
        foreach (var asset in assets)
        {
            scriptableObjectList.Add(asset);
            assetNames += "\n" + asset.name;
        }
        if (showDebugs) Debug.Log(assetNames);

        var items = LoadAllScriptableObjectsInFolder<ItemProperty>(itemPath);
        string itemNames = "Fetched items:";
        foreach (var item in items)
        {
            itemIds.Add(new ItemID(itemIds.Count + ITEM_ID_OFFSET, item));
            itemNames += "\n" + item.name;
        }
        if (showDebugs) Debug.Log(itemNames);

        var prefabs = LoadAllPrefabsInFolder(spawnablePath);
        string prefabNames = "Fetched prefabs:";
        foreach (var prefab in prefabs)
        {
            spawnableIds.Add(new SpawnableID(spawnableIds.Count + SPAWNABLE_ID_OFFSET, prefab));
            prefabNames += "\n" + prefab.name;
        }
        if (showDebugs) Debug.Log(prefabNames);

        // Force the list to register update: https://docs.unity3d.com/ScriptReference/EditorUtility.SetDirty.html
        EditorUtility.SetDirty(this);
    }

    public static T[] LoadAllScriptableObjectsInFolder<T>(string folderPath) where T : ScriptableObject
    {
        // Find all asset GUIDs in the specified folder
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });

        // Convert GUIDs to asset paths
        string[] assetPaths = guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

        // Load all assets and filter them by type
        T[] scriptableObjects = assetPaths
            .Select(path => AssetDatabase.LoadAssetAtPath<T>(path))
            .Where(asset => asset != null)
            .ToArray();

        return scriptableObjects;
    }

    public List<GameObject> LoadAllPrefabsInFolder(string folderPath)
    {
        // Find all asset GUIDs that are prefabs in the specified directory and subdirectories
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        List<GameObject> prefabs = new List<GameObject>();

        foreach (string guid in guids)
        {
            // Convert the GUID to a full asset path
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // Load the asset as a GameObject (prefab)
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            prefabs.Add(prefab);
        }

        return prefabs;
    }
#endif


    private void BuildAssetDictionary()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Asset Dictionary:");
        foreach (var asset in scriptableObjectList)
        {
            nameToScriptableObject[asset.name] = asset;
            builder.Append($"\n{asset.name}");
        }
        if (showDebugs) Debug.Log(builder);
    }

    public T GetScriptableObjectByName<T>(string name) where T : ScriptableObject
    {
        try
        {
            if (!nameToScriptableObject.ContainsKey(name))
            {
                Debug.Log("Asset does not contains " + name);
                return null;
            }
            else
            {
                return (T)nameToScriptableObject[name];
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while fetching asset: {name} of type {nameToScriptableObject[name].GetType().Name}\n{e}");
            return null;
        }

    }

    #region Print Methods

    public void PrintItemIDs()
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < itemIds.Count; i++)
        {
            string paddedId = itemIds[i].id.ToString().PadLeft(4, '0');
            sb.AppendLine($"{paddedId} - {itemIds[i].itemProperty.Name}");
        }

        Debug.Log(sb.ToString());
    }

    public void PrintSpawnableIDs()
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < spawnableIds.Count; i++)
        {
            string paddedId = spawnableIds[i].id.ToString().PadLeft(4, '0');
            sb.AppendLine($"{paddedId} - {spawnableIds[i].prefab.name.Replace(" Variant", "")}");
        }

        Debug.Log(sb.ToString());
    }

    #endregion

    #region Item Spawning
    public void SpawnItem(ItemProperty itemProperty, Vector2 position, NetworkObjectReference preferRef = default, NetworkObjectReference ignoreRef = default, float randomRange = 0f, bool randomForce = true)
    {
        SpawnItemRpc(itemProperty, position, preferRef, ignoreRef, randomRange, randomForce);
    }

    [Rpc(SendTo.Server)]
    public void SpawnItemRpc(ItemProperty itemProperty, Vector2 position, NetworkObjectReference preferRef, NetworkObjectReference ignoreRef, float randomRange, bool randomForce)
    {
        var itemReplica = SpawnItemOnServer(itemProperty, position, randomRange, randomForce);

        if (preferRef.TryGet(out var preferNetObj))
        {
            itemReplica.PreferPickerOnServer(preferNetObj.transform);
        }
        else if (ignoreRef.TryGet(out var ignoreNetObj))
        {
            itemReplica.IgnorePickerOnServer(ignoreNetObj.transform);
        }
    }

    public ItemReplica SpawnItemOnServer(ItemProperty itemProperty, Vector2 position, float randomRange = 2f, bool randomForce = true)
    {
        var randomPos = randomRange * (Vector2)Random.onUnitSphere;
        //position = position == default ? transform.position + (Vector3)randomPos : position + randomPos;
        position += randomPos;
        var itemReplicaObj = NetworkObjectPool.Main.Spawn(itemReplicaPrefab, position);
        var itemReplica = itemReplicaObj.GetComponent<ItemReplica>();
        itemReplica.SetProperty(itemProperty);

        return itemReplica;
    }

    public void SpawnByID(int id, Vector2 position, int count = 1, bool log = false, float randomRange = 2f, bool randomForce = true)
    {
        if (id < SPAWNABLE_ID_OFFSET)
        {
            ItemProperty property = null;
            for (int i = 0; i < itemIds.Count; i++)
            {
                if (itemIds[i].id == id)
                {
                    property = itemIds[i].itemProperty;
                    break;
                }
            }

            if (property != null)
            {
                for (int i = 0; i < count; i++)
                {
                    SpawnItem(property, position, default, default, randomRange, randomForce);
                }
                if (log) Debug.Log("Spawned " + count + " " + property.Name + " around " + position);
            }
            else
            {
                Debug.LogError("Item ID not found: " + id);
            }
        }
        else if (id < 30000)
        {
            SpawnPrefab(id, position, count, randomRange);

            if (log) Debug.Log("Spawned " + count + " " + spawnableIds[id - SPAWNABLE_ID_OFFSET].prefab.name.Replace(" Variant", "") + " around " + position);
        }
    }

    #endregion

    #region Prefab Spawning

    public void SpawnPrefab(int id, Vector2 position, int spawnCount = 1, float randomRange = 1, float spawnDelay = 0.25f)
    {
        SpawnPrefabRpc(id, position, spawnCount, randomRange, spawnDelay);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPrefabRpc(int id, Vector2 position, int spawnCount, float randomRange, float spawnDelay)
    {
        GameObject prefab = null;
        for (int i = 0; i < spawnableIds.Count; i++)
        {
            if (spawnableIds[i].id == id)
            {
                prefab = spawnableIds[i].prefab;
                break;
            }
        }

        if (prefab == null)
        {
            Debug.LogError("Prefab ID not found: " + id);
            return;
        }

        var positionOffset = position + MiscUtility.RandomPointInRange(randomRange);
        if (spawnCount == 1)
        {
            SpawnPrefabOnServer(prefab, positionOffset);
        }
        else if (spawnCount > 1)
        {
            StartCoroutine(SpawnPrefabCoroutine(prefab, positionOffset, spawnCount, spawnDelay));
        }
        else
        {
            Debug.LogError("Invalid spawn count: " + spawnCount);
            return;
        }
    }

    private IEnumerator SpawnPrefabCoroutine(GameObject prefab, Vector2 position, int spawnCount, float spawnDelay)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnPrefabOnServer(prefab, position);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    public void SpawnPrefabOnServer(GameObject prefab, Vector2 position)
    {
        if (!IsServer) return;
        GameObject go = Instantiate(prefab, position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }

    #endregion
}
