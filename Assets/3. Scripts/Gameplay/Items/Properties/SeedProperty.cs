using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Seed")]
public class SeedProperty : ItemProperty
{
    [Header("Seed Property")]
    [SerializeField]
    private GameObject plantPrefab;
    [SerializeField]
    private PlantProperty plantProperty;
    [SerializeField]
    private LayerMask farmPlotLayer;
    [SerializeField]
    private LayerMask plantLayer;

    public GameObject PlantPrefab => plantPrefab;
    public PlantProperty PlantProperty => plantProperty;
    public LayerMask FarmPlotLayer => farmPlotLayer;
    public LayerMask PlantLayer => plantLayer;
}
