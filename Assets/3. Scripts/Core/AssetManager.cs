using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
        PopulateDictionary();
    }

    [Header("Settings")]
    [SerializeField]
    private string assetPath;

    [Header("Common Assets")]
    [SerializeField]
    private GameObject farmPlotPrefab;
    [SerializeField]
    private GameObject itemReplicaPrefab;
    /*[SerializeField]
    private ItemProperty unidentifiedItemProperty;
    [SerializeField]
    private PlantProperty unidentifiedPlantProperty;*/
    [Header("Debugs")]
    [SerializeField]
    private List<ScriptableObject> scriptableObjectList = new List<ScriptableObject>();
    private Dictionary<string, ScriptableObject> nameToScriptableObject = new Dictionary<string, ScriptableObject>();

    /*public ItemProperty UnidentifiedItemProperty => unidentifiedItemProperty; 
    public PlantProperty UnidentifiedPlantProperty => unidentifiedPlantProperty;*/
    public GameObject FarmPlotPrefab => farmPlotPrefab;
    public GameObject ItemReplicaPrefab => itemReplicaPrefab;

#if UNITY_EDITOR
    [ContextMenu("Fetch Assets")]
    public void FetchAssets()
    {
        scriptableObjectList.Clear();
        
        var assets = LoadAllScriptableObjectsInFolder<ScriptableObject>(assetPath);
        string assetNames = "Fetched assets:";
        foreach (var asset in assets)
        {
            scriptableObjectList.Add(asset);
            assetNames += "\n" + asset.name;
        }
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

    private void PopulateDictionary()
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
}
