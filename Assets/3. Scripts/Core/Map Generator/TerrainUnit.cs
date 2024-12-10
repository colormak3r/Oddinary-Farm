using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.VolumeComponent;
using Random = UnityEngine.Random;

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

    private const int UP = 0;
    private const int RIGHT = 1;
    private const int DOWN = 2;
    private const int LEFT = 3;

    [Header("Settings")]
    [SerializeField]
    private BoxCollider2D movementBlocker;
    [SerializeField]
    private BoxCollider2D interactionCollider;
    [SerializeField]
    private Sprite[] outlineSprites;
    [SerializeField]
    private SpriteRenderer folliageRenderer;
    [SerializeField]
    private SpriteRenderer outlineRenderer;
    [SerializeField]
    private SpriteRenderer spillOverRenderer;
    [SerializeField]
    private SpriteRenderer overlayRenderer;
    [SerializeField]
    private SpriteRenderer baseRenderer;
    [SerializeField]
    private SpriteRenderer underlayRenderer;

    [Header("Debugs")]
    [SerializeField]
    private TerrainUnitProperty property;
    [SerializeField]
    private int initCount;
    [SerializeField]
    private int spillOverCount;

    public TerrainUnitProperty Property => property;

    private WorldGenerator worldGenerator;

    private void Start()
    {
        worldGenerator = WorldGenerator.Main;
    }

    public void Initialize(TerrainUnitProperty property, bool canSpawnFolliage)
    {
        initCount++;
        this.property = property;

        // Folliage
        var folliage = property.FolliageSprite;
        if (canSpawnFolliage && folliage != null && property.FolliageChance > Random.value)
            folliageRenderer.sprite = folliage;
        else
            folliageRenderer.sprite = null;

        // Overlay
        overlayRenderer.sprite = property.OverlayChance > Random.value ? property.OverlaySprite : null;

        // Base
        baseRenderer.sprite = property.BaseSprite;

        // Outline
        outlineRenderer.sprite = null;
        if (property.DrawOutline)
        {
            outlineRenderer.transform.rotation = Quaternion.identity;
            outlineRenderer.color = property.OutlineColor;
        }

        // Spill Over
        spillOverRenderer.sprite = null;
        //underlayRenderer.sprite = property.UnderlaySprite;

        // Render outline and spillover
        var unmatchedNeighbor = new bool[4];
        var spillOver = false;
        for (int i = 0; i < SCAN_POSITION.Length; i++)
        {
            var position = (Vector2)transform.position + SCAN_POSITION[i];
            var mappedProperty = WorldGenerator.Main.GetMappedProperty((int)position.x, (int)position.y);
            var isNeighborHigher = mappedProperty.Elevation.min >= property.Elevation.max;
            if (mappedProperty != property)
            {
                if (!spillOver && isNeighborHigher)
                {
                    spillOverCount++;
                    spillOver = true;
                    spillOverRenderer.sprite = Random.value < mappedProperty.SpillOverChance ? mappedProperty.OverlaySprite : null;
                }

                if (property.DrawOutline)
                {
                    switch (i)
                    {
                        case 1: // Up
                            unmatchedNeighbor[UP] = !isNeighborHigher;
                            break;
                        case 5: // Right
                            unmatchedNeighbor[RIGHT] = !isNeighborHigher;
                            break;
                        case 7: // Down
                            unmatchedNeighbor[DOWN] = !isNeighborHigher;
                            break;
                        case 3: // Left
                            unmatchedNeighbor[LEFT] = !isNeighborHigher;
                            break;
                    }
                }
                else if (spillOver)
                {
                    // break early if spill over is already rendered and not drawing outline
                    break;
                }
            }
        }

        // Render outline between this terrain unit and its neighbors that has different property
        if (property.DrawOutline)
        {
            RenderOutline(unmatchedNeighbor);
        }

        // Block movement if not accessible
        movementBlocker.enabled = !property.IsAccessible;
    }

    public void BreakFolliage()
    {
        folliageRenderer.sprite = null;
    }

    private void RenderOutline(bool[] unmatchedNeighbor)
    {
        int unmatchedCount = unmatchedNeighbor.Count(n => n);
        if (unmatchedCount == 1)
        {
            int index = Array.IndexOf(unmatchedNeighbor, true);
            outlineRenderer.sprite = outlineSprites[0];
            outlineRenderer.transform.localRotation = Quaternion.Euler(0, 0, -90 * index);
        }
        else if (unmatchedCount == 2)
        {
            var index1 = -1;
            var index2 = -1;
            for (int i = 0; i < 4; i++)
            {
                if (unmatchedNeighbor[i])
                {
                    if (index1 < 0)
                        index1 = i;
                    else
                        index2 = i;
                }
            }

            if (index2 - index1 == 1)
            {
                outlineRenderer.sprite = outlineSprites[1];
                outlineRenderer.transform.localRotation = Quaternion.Euler(0, 0, -90 * index1);
            }
            else if (index1 == 0 && index2 == 3)
            {
                outlineRenderer.sprite = outlineSprites[1];
                outlineRenderer.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            else
            {
                outlineRenderer.sprite = outlineSprites[2];
                outlineRenderer.transform.localRotation = Quaternion.Euler(0, 0, -90 * index1);
            }
        }
        else if (unmatchedCount == 3)
        {
            int index = Array.IndexOf(unmatchedNeighbor, false);
            outlineRenderer.sprite = outlineSprites[3];
            outlineRenderer.transform.localRotation = Quaternion.Euler(0, 0, -90 * index);
        }
        else if (unmatchedCount == 4)
        {
            outlineRenderer.sprite = outlineSprites[4];
        }
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
