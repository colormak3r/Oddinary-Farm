using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class SpriteBlender : MonoBehaviour
{
    private static Vector3[] SCAN_POSITION = new Vector3[]
    {
        new Vector3(-1,1),
        new Vector3(0,1),
        new Vector3(1,1),
        new Vector3(-1,0),
        new Vector3(0,0),   // #4
        new Vector3(1,0),
        new Vector3(-1,-1),
        new Vector3(0,-1),
        new Vector3(1,-1),
    };

    [Header("Settings")]
    [SerializeField]
    private BlendRules rules;
    [SerializeField]
    private LayerMask blendLayer;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private PolygonCollider2D collider2D;

    public BlendRules Rules => rules;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        collider2D = GetComponentInParent<PolygonCollider2D>();
    }

    [ContextMenu("Blend")]
    public void Blend(bool reblend = false)
    {
        IBool[] neighbors = new IBool[9];
        SpriteBlender[] neigborBlenders = new SpriteBlender[9];

        for (int i = 0; i < SCAN_POSITION.Length; i++)
        {
            if (i == 4) continue;

            var hit = Physics2D.OverlapPoint(transform.position + SCAN_POSITION[i], blendLayer);
            if (hit)
            {
                var neighbor = hit.GetComponentInChildren<SpriteBlender>();
                if (neighbor != null)
                {
                    if (neighbor.Rules == rules)
                    {
                        neigborBlenders[i] = neighbor;
                        neighbors[i] = IBool.Yes;
                    }
                    else
                    {
                        neighbors[i] = IBool.No;
                    }
                }
                else
                {
                    neighbors[i] = IBool.No;
                }
            }
            else
            {
                {
                    neighbors[i] = IBool.No;
                }
            }
        }

        /*var builder = "Neighbor Actual:\n";
        for (int i = 0; i < neighbors.Length; i++)
        {
            builder += neighbors[i].ToSymbol(true) + " ";
            if (i == 2 || i == 5) builder += "\n";
        }
        Debug.Log(builder);*/

        // Match and set the new sprite
        spriteRenderer.sprite = rules.GetMatchingSprite(neighbors);

        if (spriteRenderer.sprite == null)
        {
            Debug.Log("Cannot find matching sprite", this);
        }
        else if (reblend)
        {
            StartCoroutine(DelayBlendNeighbor(neigborBlenders));
        }
        else if (collider2D)
        {
            // Reshape the collider
            List<Vector2> physicsShape = new List<Vector2>();
            spriteRenderer.sprite.GetPhysicsShape(0, physicsShape);
            collider2D.SetPath(0, physicsShape);
        }
    }
    private IEnumerator DelayBlendNeighbor(SpriteBlender[] neigborBlenders)
    {
        yield return null;

        foreach (var neighbor in neigborBlenders)
        {
            if (neighbor != null) neighbor.Blend(false);
        }
    }

    public void ReblendNeighbors()
    {
        SpriteBlender[] neigborBlenders = new SpriteBlender[9];

        for (int i = 0; i < SCAN_POSITION.Length; i++)
        {
            if (i == 4) continue;

            var hit = Physics2D.OverlapPoint(transform.position + SCAN_POSITION[i], blendLayer);
            if (hit)
            {
                var neighbor = hit.GetComponentInChildren<SpriteBlender>();
                if (neighbor != null && neighbor.Rules == rules) neighbor.Blend();
            }
        }
    }
}
