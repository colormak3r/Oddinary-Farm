using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Face", menuName = "Scriptable Objects/Appearance/Face")]
public class Face : AppearanceData, IEquatable<Face>
{
    public bool Equals(Face other)
    {
        return other == this;
    }
}