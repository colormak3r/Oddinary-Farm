using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pickaxe Property", menuName = "Scriptable Objects/Item/Pickaxe")]
public class PickaxeProperty : ItemProperty
{
    [Header("Pickaxe Settings")]
    [SerializeField]
    private int inibini = 10;
}