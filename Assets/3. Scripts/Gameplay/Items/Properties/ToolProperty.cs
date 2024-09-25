using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ToolProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private int range = 10;
    [SerializeField]
    private float radius = 0.5f;

    public int Range => range;
    public float Radius => radius;
}
