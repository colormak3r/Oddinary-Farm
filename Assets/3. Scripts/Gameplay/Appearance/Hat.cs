using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Hat", menuName = "Scriptable Objects/Appearance/Hat")]
public class Hat : ScriptableObject, IEquatable<Hat>
{
    [Header("Settings")]
    [SerializeField]
    private Sprite sprite;

    public Sprite Sprite => sprite;

    public bool Equals(Hat other)
    {
        return other == this;
    }
}
