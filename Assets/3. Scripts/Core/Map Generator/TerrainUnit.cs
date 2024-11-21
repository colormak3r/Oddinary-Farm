using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUnit : MonoBehaviour, ILocalObjectPoolingBehaviour
{
    private static Vector2[] SCAN_POSITION = new Vector2[]
    {
        new Vector2(-1,1),
        new Vector2(0,1),
        new Vector2(1,1),
        new Vector2(-1,0),
        new Vector2(0,0),   // #4
        new Vector2(1,0),
        new Vector2(-1,-1),
        new Vector2(0,-1),
        new Vector2(1,-1),
    };

    [Header("Settings")]
    [SerializeField]
    private BoxCollider2D movementBlocker;
    [SerializeField]
    private BoxCollider2D interactionCollider;
    [SerializeField]
    private SpriteRenderer outlineRenderer;
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

    private WorldGenerator worldGenerator;

    private void Start()
    {
        worldGenerator = WorldGenerator.Main;
    }

    public void Initialize(TerrainUnitProperty property)
    {
        this.property = property;

        overlayRenderer.sprite = Random.value < property.OverlaySpriteChance ? property.OverlaySprite : null;
        baseRenderer.sprite = property.BaseSprite;
        outlineRenderer.sprite = null;
        //underlayRenderer.sprite = property.UnderlaySprite;

        for (int i = 0; i < SCAN_POSITION.Length; i++)
        {
            var position = (Vector2)transform.position + SCAN_POSITION[i];
            var mappedProperty = WorldGenerator.Main.GetMappedProperty((int)position.x, (int)position.y);
            if (mappedProperty != property)
            {
                outlineRenderer.sprite = Random.value < mappedProperty.OutlineSpriteChance ? mappedProperty.OverlaySprite : null;
                break;
            }
        }

        movementBlocker.enabled = !property.IsAccessible;
    }

    public void LocalSpawn()
    {
        /*overlayRenderer.enabled = true;
        baseRenderer.enabled = true;

        movementBlocker.enabled = true;*/
    }

    public void LocalDespawn()
    {
        /*overlayRenderer.enabled = false;
        baseRenderer.enabled = false;

        movementBlocker.enabled = false;*/
    }
}
