using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shovel Property", menuName = "Scriptable Objects/Item/Shovel")]
public class ShovelProperty : ToolProperty
{
    [Header("Shovel Settings")]
    [SerializeField]
    private LayerMask diggableLayer;
    [SerializeField]
    private LayerMask undiggableLayer;

    public LayerMask DiggableLayer => diggableLayer;
    public LayerMask UndiggableLayer => undiggableLayer;
}
