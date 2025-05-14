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
    private bool destroyOnHarvest;
    [SerializeField]
    private SeedProperty seedProperty;
    [SerializeField]
    private FoodType foodType;
    [SerializeField]
    private FoodColor foodColor;
    [SerializeField]
    private PlantStage[] stages;
    [SerializeField]
    private LootTable lootTable;

    public bool DestroyOnHarvest => destroyOnHarvest;
    public SeedProperty SeedProperty => seedProperty;
    public FoodType FoodType => foodType;
    public FoodColor FoodColor => foodColor;
    public PlantStage[] Stages => stages;
    public LootTable LootTable => lootTable;

    public bool Equals(PlantProperty other)
    {
        return this == other;
    }
}
