using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hand Property", menuName = "Scriptable Objects/Item/Hand")]
public class HandProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private int range = 10;
    [SerializeField]
    private float radius = 0.5f;

    public int Range => range;
    public float Radius => radius;
}