using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hammer Property", menuName = "Scriptable Objects/Item/Hammer")]
public class HammerProperty : ToolProperty
{
    [Header("Hammer Settings")]
    [SerializeField]
    private GameObject[] structures;
    [SerializeField]
    private LayerMask invalidLayers;
    [SerializeField]
    private LayerMask terrainLayers;
    [SerializeField]
    private LayerMask structureLayers;

    public GameObject[] Structures => structures;
    public LayerMask InvalidLayers => invalidLayers;
    public LayerMask TerrainLayers => terrainLayers;
    public LayerMask StructureLayers => structureLayers;
}
