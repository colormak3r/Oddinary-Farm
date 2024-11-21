using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Terrain Unit Property", menuName = "Scriptable Objects/Map Generator/Terrain Unit Property")]
public class TerrainUnitProperty : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField]
    private Color mapColor;
    [SerializeField]
    private MinMaxFloat elevation;
    [SerializeField]
    private bool isAccessible = true;
    [SerializeField]
    private TerrainBlockProperty blockProperty;

    [Header("Graphics")]
    [SerializeField]
    private float overlaySpriteChance = 0.5f;
    [SerializeField]
    private float outlineSpriteChance = 0.5f;
    [SerializeField]
    private Sprite[] overlaySprite;
    [SerializeField]
    private Sprite[] baseSprite;
    [SerializeField]
    private Sprite[] underlaySprite;


    public Color MapColor => mapColor;
    public MinMaxFloat Elevation => elevation;
    public bool IsAccessible => isAccessible;
    public TerrainBlockProperty BlockProperty => blockProperty;

    public float OverlaySpriteChance => overlaySpriteChance;
    public float OutlineSpriteChance => outlineSpriteChance;
    public Sprite OverlaySprite => overlaySprite.GetRandomElement();
    public Sprite BaseSprite => baseSprite.GetRandomElement();
    public Sprite UnderlaySprite => underlaySprite.GetRandomElement();


    public bool Match(float elevation)
    {
        return elevation >= this.elevation.min && elevation <= this.elevation.max;
    }
}
