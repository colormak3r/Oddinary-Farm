using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpriteBlender : MonoBehaviour
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
    private BlendRules rules;
    [SerializeField]
    private Vector2 offset;
    [SerializeField]
    private LayerMask blendLayer;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private PolygonCollider2D movementBlocker;

    [Header("Debugs")]
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private bool showDebugs;

    private Vector2 position;

    public BlendRules Rules => rules;

    private List<Vector2> scannedPosition = new List<Vector2>();

    private void Awake()
    {
        scannedPosition.Capacity = SCAN_POSITION.Length;
    }

    [ContextMenu("Blend")]
    private void Blend()
    {
        Blend(false);
    }

    public void Blend(bool reblendNeighbor = false)
    {
        IBool[] neighbors = new IBool[9];
        SpriteBlender[] neighborBlenders = new SpriteBlender[9];

        position = (Vector2)transform.position + offset;

        if (showGizmos) scannedPosition.Clear();

        // Single scan capturing all nearby colliders
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 1.5f, blendLayer);

        foreach (var hit in hits)
        {
            var neighbor = hit.GetComponentInChildren<SpriteBlender>();
            if (neighbor != null && neighbor != this && neighbor.Rules == rules)
            {
                Vector2 delta = (Vector2)neighbor.transform.position + neighbor.offset - position;

                // Convert relative position to index
                int x = Mathf.RoundToInt(delta.x);
                int y = Mathf.RoundToInt(delta.y);
                if (Mathf.Abs(x) <= 1 && Mathf.Abs(y) <= 1)
                {
                    int index = (1 - y) * 3 + (x + 1); // Mapping positions (-1,-1)->8, (0,0)->4, (1,1)->0, etc.
                    if (index == 4) continue; // Skip center (current object)

                    neighbors[index] = IBool.Yes;
                    neighborBlenders[index] = neighbor;

                    if (showGizmos) scannedPosition.Add(position + new Vector2(x, y));
                }
            }
        }

        // Set IBool.No for empty neighbors
        for (int i = 0; i < neighbors.Length; i++)
            if (i != 4 && neighbors[i] != IBool.Yes)
                neighbors[i] = IBool.No;

        // Debug logging (optional)
        if (showDebugs)
        {
            var builder = position + "\nNeighbor Actual:\n";
            for (int i = 0; i < neighbors.Length; i++)
            {
                builder += neighbors[i].ToSymbol(true) + " ";
                if (i == 2 || i == 5) builder += "\n";
            }
            Debug.Log(builder);
        }

        // Match sprite
        spriteRenderer.sprite = rules.GetMatchingSprite(neighbors);

        if (spriteRenderer.sprite == null && showDebugs)
            Debug.Log("Cannot find matching sprite", this);

        // Reblend neighbors if needed
        if (reblendNeighbor)
        {
            if (showDebugs) Debug.Log("Reblending neighbors", this);
            StartCoroutine(DelayBlendNeighbor(neighborBlenders));
        }

        // Collider reshaping
        if (movementBlocker && spriteRenderer.sprite)
        {
            if (showDebugs) Debug.Log("Reshaping collider", this);
            ReshapeCollider();
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

    [ContextMenu("ReblendNeighbors")]
    public void ReblendNeighbors()
    {
        SpriteBlender[] neigborBlenders = new SpriteBlender[9];

        for (int i = 0; i < SCAN_POSITION.Length; i++)
        {
            if (i == 4) continue;

            var hit = Physics2D.OverlapPoint(position + SCAN_POSITION[i], blendLayer);
            if (hit)
            {
                var neighbor = hit.GetComponentInChildren<SpriteBlender>();
                if (neighbor != null && neighbor.Rules == rules) neighbor.Blend();
            }
        }
    }

    [ContextMenu("Reshape Collider")]
    private void ReshapeCollider()
    {
        List<Vector2> physicsShape = new List<Vector2>();
        spriteRenderer.sprite.GetPhysicsShape(0, physicsShape);
        movementBlocker.SetPath(0, physicsShape);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        foreach (var pos in scannedPosition)
        {
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }

}
