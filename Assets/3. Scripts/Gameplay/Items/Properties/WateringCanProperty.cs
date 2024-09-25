using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Watering Can Property", menuName = "Scriptable Objects/Item/Watering Can")]
public class WateringCanProperty : ToolProperty
{
    [Header("Watering Can Settings")]
    [SerializeField]
    private int maxCharge = 3;
    [SerializeField]
    private LayerMask waterableLayer;

    public int MaxCharge => maxCharge;
    public LayerMask WaterableLayer => waterableLayer;
}
