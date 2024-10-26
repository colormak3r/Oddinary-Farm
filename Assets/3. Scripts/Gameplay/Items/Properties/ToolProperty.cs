using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ToolProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private float radius = 0.5f;

    public float Radius => radius;
}
