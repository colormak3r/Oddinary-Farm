using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppearanceData : ScriptableObject, IEquatable<AppearanceData>
{
    [Header("Settings")]
    [SerializeField]
    private Sprite displaySprite;

    public Sprite DisplaySprite => displaySprite;

    public bool Equals(AppearanceData other)
    {
        return other == this;
    }
}
