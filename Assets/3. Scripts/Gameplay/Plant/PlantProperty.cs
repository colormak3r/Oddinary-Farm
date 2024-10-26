using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlantStage
{
    public Sprite sprite;
    public float duration;
    public bool isHarvestStage;
    public int nextStage;
}

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Plant/Plant(Test Only)")]
public class PlantProperty : ScriptableObject, IEquatable<PlantProperty>
{
    [SerializeField]
    private PlantStage[] stages;
    [SerializeField]
    private LootTable lootTable;

    public PlantStage[] Stages => stages;
    public LootTable LootTable => lootTable;

    public bool Equals(PlantProperty other)
    {
        return this == other;
    }
}
