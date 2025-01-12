using UnityEngine;

public abstract class ToolProperty : ItemProperty
{
    [Header("Tool Settings")]
    [SerializeField]
    private Vector2 size = Vector2.one;
    [SerializeField]
    private LayerMask terrainLayer;
    [SerializeField]
    private LayerMask validLayers;
    [SerializeField]
    private LayerMask invalidLayers;

    [Header("Preview Properties")]
    [SerializeField]
    private Sprite previewIconSprite;
    [SerializeField]
    private Vector2 previewIconOffset;
    [SerializeField]
    private Color previewValidColor = new Color(113, 170, 52);
    [SerializeField]
    private Color previewInvalidColor = new Color(230, 72, 46);

    public Vector2 Size => size;
    public LayerMask TerrainLayer => terrainLayer;
    public LayerMask ValidLayers => validLayers;
    public LayerMask InvalidLayers => invalidLayers;

    public Sprite PreviewIconSprite => previewIconSprite;
    public Vector2 PreviewIconOffset => previewIconOffset;
    public Color PreviewValidColor => previewValidColor;
    public Color PreviewInvalidColor => previewInvalidColor;
}
