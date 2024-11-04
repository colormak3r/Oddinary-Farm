using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUnit : MonoBehaviour, ILocalObjectPoolingBehaviour
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
    private TerrainUnitProperty property;

    public TerrainUnitProperty Property => property;

    public void Initialize(TerrainUnitProperty property)
    {
        this.property = property;

        overlayRenderer.sprite = property.OverlaySprite;
        baseRenderer.sprite = property.BaseSprite;
        //underlayRenderer.sprite = property.UnderlaySprite;

        collider2D.enabled = !property.IsAccessible;
    }

    public void LocalSpawn()
    {
        overlayRenderer.enabled = true;
        baseRenderer.enabled = true;

        collider2D.enabled = true;
    }

    public void LocalDespawn()
    {
        overlayRenderer.enabled = false;
        baseRenderer.enabled = false;

        collider2D.enabled = false;
    }
}
