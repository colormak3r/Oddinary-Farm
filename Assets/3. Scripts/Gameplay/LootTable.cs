using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemCountProbability
{
    public ItemProperty item;
    [Range(0, 1)]
    public float probability;
    public MinMaxInt minMaxCount;
}


[CreateAssetMenu(fileName = " LootTable", menuName = "Scriptable Objects/LootTable")]
public class LootTable : ScriptableObject
{
    public ItemCountProbability[] Table;
}
