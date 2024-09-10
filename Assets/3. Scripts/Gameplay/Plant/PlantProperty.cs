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
public class PlantProperty : ScriptableObject
{
    [SerializeField]
    private PlantStage[] stages;

    public PlantStage[] Stages => stages;
}
