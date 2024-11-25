using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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

public class AssetManager : MonoBehaviour
{
    public static AssetManager Main;

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
        PopulateAssetDictionary();
        //PopulateCurrencyDictionary();
    }

    [Header("Settings")]
    [SerializeField]
    private string assetPath;
    [SerializeField]
    private string itemPath;

    [Header("Common Assets")]
    [SerializeField]
    private GameObject farmPlotPrefab;
    public GameObject FarmPlotPrefab => farmPlotPrefab;
    [SerializeField]
    private GameObject itemReplicaPrefab;
    public GameObject ItemReplicaPrefab => itemReplicaPrefab;

    [Header("Scriptable Object Assets")]
    [SerializeField]
    private List<ScriptableObject> scriptableObjectList = new List<ScriptableObject>();
    private Dictionary<string, ScriptableObject> nameToScriptableObject = new Dictionary<string, ScriptableObject>();

    [Header("Item Properties")]
    [SerializeField]
    private List<ItemID> itemIds = new List<ItemID>();

    /*[SerializeField]
    private ItemProperty unidentifiedItemProperty;
    [SerializeField]
    private PlantProperty unidentifiedPlantProperty;*/
    /*[SerializeField]
    private List<CurrencyTypeProperty> currencyProperties = new List<CurrencyTypeProperty>();
    private Dictionary<CurrencyType, CurrencyProperty> currencyTypeToProperty = new Dictionary<CurrencyType, CurrencyProperty>();
    private Dictionary<CurrencyProperty, CurrencyType> currencyPropertyToType = new Dictionary<CurrencyProperty, CurrencyType>();*/

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

#if UNITY_EDITOR
    [ContextMenu("Fetch Assets")]
    public void FetchAssets()
    {
        scriptableObjectList.Clear();
        itemIds.Clear();

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
        int id = 1;
        foreach (var item in items)
        {
            itemIds.Add(new ItemID(id, item));
            itemNames += "\n" + item.name;
            id++;
        }
        if (showDebugs) Debug.Log(itemNames);

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
#endif

    /*private void PopulateCurrencyDictionary()
    {
        foreach (var currencyProperty in currencyProperties)
        {
            currencyTypeToProperty[currencyProperty.currencyType] = currencyProperty.currencyProperty;
            currencyPropertyToType[currencyProperty.currencyProperty] = currencyProperty.currencyType;
        }
    }*/

    private void PopulateAssetDictionary()
    {
        //Debug.Log("scriptableObjectList = " + scriptableObjectList.Count);
        //string assetNames = "Loaded Assets:";
        foreach (var asset in scriptableObjectList)
        {
            nameToScriptableObject[asset.name] = asset;
            //assetNames += "\n" + asset.name;
        }
        //Debug.Log(assetNames);
    }

    public T GetScriptableObjectByName<T>(string name) where T : ScriptableObject
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

    /*public CurrencyProperty GetCurrencyPropertyFromType(CurrencyType currencyType)
    {
        return currencyTypeToProperty[currencyType];
    }

    public CurrencyType GetCurrencyTypeFromProperty(CurrencyProperty currencyProperty)
    {
        return currencyPropertyToType[currencyProperty];
    }*/

    public void PrintItemIDs()
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < itemIds.Count; i++)
        {
            string paddedId = itemIds[i].id.ToString().PadLeft(4, '0');
            sb.AppendLine($"{paddedId} : {itemIds[i].itemProperty.Name}");
        }

        Debug.Log(sb.ToString());
    }

    public void SpawnByID(int id, Vector2 position, int count = 1, bool log = false, float randomRange = 2f, bool randomForce = true)
    {
        if (id < 1000)
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
                    SpawnItem(property, position, randomRange, randomForce);
                }
                if (log) Debug.Log("Spawned " + count + " " + property.Name + " around " + position);
            }
            else
            {
                Debug.LogError("Item ID not found: " + id);
            }
        }
    }

    public void SpawnItem(ItemProperty itemProperty, Vector2 position, float randomRange = 2f, bool randomForce = true)
    {
        SpawnItemRpc(itemProperty, position, randomRange, randomForce);
    }

    [Rpc(SendTo.Server)]
    private void SpawnItemRpc(ItemProperty itemProperty, Vector2 position, float randomRange, bool randomForce)
    {
        var randomPos = randomRange * (Vector2)Random.onUnitSphere;
        position = position == default ? transform.position + (Vector3)randomPos : position + randomPos;
        GameObject go = Instantiate(itemReplicaPrefab, position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var itemReplica = go.GetComponent<ItemReplica>();
        itemReplica.SetProperty(itemProperty);
        itemReplica.AddRandomForce();
    }
}
