using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppearanceData : ScriptableObject, IEquatable<Hat>
{
    [Header("Settings")]
    [SerializeField]
    private Sprite displaySprite;

    public Sprite DisplaySprite => displaySprite;

    public bool Equals(Hat other)
    {
        return other == this;
    }
}
