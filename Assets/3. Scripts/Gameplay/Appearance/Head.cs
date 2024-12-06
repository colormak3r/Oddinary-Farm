using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Head", menuName = "Scriptable Objects/Appearance/Head")]
public class Head : AppearanceData, IEquatable<Head>
{
    public bool Equals(Head other)
    {
        return other == this;
    }
}