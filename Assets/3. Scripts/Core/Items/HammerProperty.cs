using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hammer Property", menuName = "Scriptable Objects/Item/Hammer")]
public class HammerProperty : ItemProperty
{
    [Header("Hammer Settings")]
    [SerializeField]
    private int ongoboingo = 10;
}
