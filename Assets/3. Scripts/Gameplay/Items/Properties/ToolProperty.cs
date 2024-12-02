using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ToolProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private float radius = 0.5f;

    [Header("Tool Sound Settings")]
    [SerializeField]
    private AudioClip hitSound;


    public float Radius => radius;
}
