using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LocalObjectController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private string guid;

    private ILocalObjectPoolingBehaviour[] behaviours;

    public string Guid => guid;
#if UNITY_EDITOR
    [ContextMenu("Generate GUID")]
    public void GenerateGUID()
    {
        guid = System.Guid.NewGuid().ToString();

        // Record object state for undo functionality and mark as dirty
        Undo.RecordObject(this, "Generate GUID");
        EditorUtility.SetDirty(this);
    }
#endif

    private void Awake()
    {
        behaviours = GetComponentsInChildren<ILocalObjectPoolingBehaviour>(true);
        if (guid == "") Debug.LogError("GUID is empty.", gameObject);
    }

    public void LocalSpawn()
    {
        foreach (var localObjectPoolingBehaviour in behaviours)
        {
            localObjectPoolingBehaviour.LocalSpawn();
        }
    }

    public void LocalDespawn()
    {
        foreach (var localObjectPoolingBehaviour in behaviours)
        {
            localObjectPoolingBehaviour.LocalDespawn();
        }
    }
}
