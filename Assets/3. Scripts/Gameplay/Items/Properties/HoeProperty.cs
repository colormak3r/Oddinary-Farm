using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Hoe Property", menuName = "Scriptable Objects/Item/Hoe")]
public class HoeProperty : ToolProperty
{
    [Header("Hoe Settings")]
    [SerializeField]
    private LayerMask farmPlotLayer;
    [SerializeField]
    private LayerMask plantLayer;

    public LayerMask FarmPlotLayer => farmPlotLayer;
    public LayerMask PlantLayer => plantLayer;
}
