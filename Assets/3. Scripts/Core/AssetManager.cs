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

        FetchAssets();
    }

    [Header("Settings")]
    [SerializeField]
    private string assetPath;

    [Header("Common Assets")]
    [SerializeField]
    private GameObject farmPlotPrefab;
    /*[SerializeField]
    private ItemProperty unidentifiedItemProperty;
    [SerializeField]
    private PlantProperty unidentifiedPlantProperty;*/

    private Dictionary<string, ScriptableObject> nameToScriptableObject = new Dictionary<string, ScriptableObject>();

    /*public ItemProperty UnidentifiedItemProperty => unidentifiedItemProperty; 
    public PlantProperty UnidentifiedPlantProperty => unidentifiedPlantProperty;*/
    public GameObject FarmPlotPrefab => farmPlotPrefab;


    [ContextMenu("Fetch Assets")]
    public void FetchAssets()
    {
        var assets = LoadAllScriptableObjectsInFolder<ScriptableObject>(assetPath);
        string assetNames = "Loaded assets:";
        foreach (var asset in assets)
        {
            nameToScriptableObject[asset.name] = asset;
            assetNames += "\n" + asset.name;
        }
    }

    public T GetScriptableObjectByName<T>(string name) where T : ScriptableObject
    {
        if (!nameToScriptableObject.ContainsKey(name))
        {
            return null;
        }
        else
        {
            return (T)nameToScriptableObject[name];
        }
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
}
