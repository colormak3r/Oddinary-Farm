using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUnit : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private BoxCollider2D collider2D;
    [SerializeField]
    private SpriteRenderer overlayRenderer;
    [SerializeField]
    private SpriteRenderer baseRenderer;
    [SerializeField]
    private SpriteRenderer underlayRenderer;

    [Header("Debugs")]
    [SerializeField]
    private TerrainProperty property;

    public TerrainProperty Property => property;

    public void Initialize(TerrainProperty property)
    {
        this.property = property;
        
        overlayRenderer.sprite = property.OverlaySprite;
        baseRenderer.sprite = property.BaseSprite;
        //underlayRenderer.sprite = property.UnderlaySprite;

        collider2D.enabled = !property.IsAccessible;
    }
}
