using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Head", menuName = "Scriptable Objects/Appearance/Head")]
public class Head : ScriptableObject, IEquatable<Head>
{
    [Header("Settings")]
    [SerializeField]
    private Sprite sprite;

    public Sprite Sprite => sprite;

    public bool Equals(Head other)
    {
        return other == this;
    }
}