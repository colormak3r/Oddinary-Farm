using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StructureItem Property", menuName = "Scriptable Objects/Item/Structure Item")]
public class StructureItemProperty : SpawnerProperty
{
    [Header("Structure Item Settings")]
    [SerializeField]
    private LayerMask structureLayer;

    public LayerMask StructureLayer => structureLayer;
}
