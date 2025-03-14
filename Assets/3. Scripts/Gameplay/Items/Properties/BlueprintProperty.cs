using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Blueprint Property", menuName = "Scriptable Objects/Item/Blueprint")]
public class BlueprintProperty : SpawnerProperty
{
    [Header("Blueprint Settings")]
    [SerializeField]
    private LayerMask structureLayer;

    public LayerMask StructureLayer => structureLayer;
}
