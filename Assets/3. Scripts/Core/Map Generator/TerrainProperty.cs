using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Terrain Property", menuName = "Scriptable Objects/Map Generator/Terrain Property")]
public class TerrainProperty : ScriptableObject
{
    [Header("Settings")]
    [SerializeField]
    private MinMaxFloat elevation;
    [SerializeField]
    private bool isAccessible = true;
    [SerializeField]
    private Sprite[] overlaySprite;
    [SerializeField]
    private Sprite[] baseSprite;
    [SerializeField]
    private Sprite[] underlaySprite;

    public MinMaxFloat Elevation => elevation;
    public bool IsAccessible => isAccessible;

    public Sprite OverlaySprite => overlaySprite.GetRandomElement();
    public Sprite BaseSprite => baseSprite.GetRandomElement();
    public Sprite UnderlaySprite => underlaySprite.GetRandomElement();


    public bool Match(float elevation)
    {
        return elevation >= this.elevation.min && elevation <= this.elevation.max;
    }
}
