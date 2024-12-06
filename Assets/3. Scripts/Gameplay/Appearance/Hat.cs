using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Hat", menuName = "Scriptable Objects/Appearance/Hat")]
public class Hat : AppearanceData, IEquatable<Hat>
{
    public bool Equals(Hat other)
    {
        return other == this;
    }
}
