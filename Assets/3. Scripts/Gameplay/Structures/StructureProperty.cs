using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure Property", menuName = "Scriptable Objects/Structure Property")]
public class StructureProperty : ScriptableObject
{
    [Header("Structure Properties")]
    [SerializeField]
    private StructureItemProperty structureItemProperty;
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private Vector2 offset;
    [SerializeField]
    private Vector2 size = Vector2.one * 3;

    public StructureItemProperty StructureItemProperty => structureItemProperty;
    public Sprite Sprite => sprite;
    public Vector2 Offset => offset;
    public Vector2 Size => size;
}
