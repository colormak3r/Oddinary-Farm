using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ToolProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private LayerMask terrainLayer;
    [SerializeField]
    private Sprite iconSprite;
    [SerializeField]
    private int size = 1;

    public LayerMask TerrainLayer => terrainLayer;
    public Sprite IconSprite => iconSprite;
    public int Size => size;
}
