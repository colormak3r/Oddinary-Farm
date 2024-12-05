using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Face", menuName = "Scriptable Objects/Appearance/Face")]
public class Face : ScriptableObject, IEquatable<Face>
{
    [Header("Settings")]
    [SerializeField]
    private Sprite sprite;

    public Sprite Sprite => sprite;

    public bool Equals(Face other)
    {
        return other == this;
    }
}
