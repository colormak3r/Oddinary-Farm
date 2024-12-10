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
    public int stageIncrement;
}

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Plant")]
public class PlantProperty : ScriptableObject, IEquatable<PlantProperty>
{
    [Header("Plant Property")]
    [SerializeField]
    private SeedProperty seedProperty;
    [SerializeField]
    private PlantStage[] stages;
    [SerializeField]
    private LootTable lootTable;

    public SeedProperty SeedProperty => seedProperty;
    public PlantStage[] Stages => stages;
    public LootTable LootTable => lootTable;

    public bool Equals(PlantProperty other)
    {
        return this == other;
    }
}
