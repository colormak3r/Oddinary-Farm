using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Terrain Property", menuName = "Scriptable Objects/Map Generator/Terrain Property")]
public class TerrainProperty : ScriptableObject
{
    [Header("Settings")]
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private MinMaxFloat elevation;

    public Sprite Sprite => sprite;
    public MinMaxFloat Elevation => elevation;
    public bool Match(float elevation)
    {
        return elevation >= this.elevation.min && elevation < this.elevation.max;
    }
}
