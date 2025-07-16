/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/16/2025 (Khoa)
 * Notes:           <write here>
*/

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
    public BlendRules Rules => rules;
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
    private bool firstBlend = true;
    private List<Vector2> scannedPosition = new List<Vector2>();
    private float timeSinceLastBlend;

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
        // Extra logging. TODO: Remove in production
        if (timeSinceLastBlend != 0 && Time.time - timeSinceLastBlend < 0.1f)
        {
            Debug.LogWarning($"Blend called too frequently: {transform.root.name}/{name}", this);
        }
        timeSinceLastBlend = Time.time;

        // Initialize variables
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
            Debug.Log(PrintNeighbor(neighbors));
        }

        // Match sprite
        var sprite_cached = spriteRenderer.sprite;
        var sprite = rules.GetMatchingSprite(neighbors);
        if (sprite == null) Debug.LogError("Cannot find matching sprite:\n" + PrintNeighbor(neighbors), this);
        spriteRenderer.sprite = sprite;

        // Reblend neighbors if needed
        if (reblendNeighbor)
        {
            if (showDebugs) Debug.Log("Reblending neighbors", this);
            foreach (var neighbor in neighborBlenders)
            {
                if (neighbor != null) neighbor.RequestReblend();
            }
        }

        // Collider reshaping
        if (movementBlocker && (sprite != sprite_cached || firstBlend))
        {
            if (showDebugs) Debug.Log("Reshaping collider", this);
            ReshapeCollider(sprite);
        }
    }

    // Request a reblend of this sprite
    // Sent to SpriteBlenderManager to handle blending in a single pass
    public void RequestReblend() => SpriteBlenderManager.RequestBlend(this);

    /*[ContextMenu("ReblendNeighbors")]
    public void ReblendNeighbors()
    {
        // Use when a structure get removed => Neighbors need to be reblended
        for (int i = 0; i < SCAN_POSITION.Length; i++)
        {
            if (i == 4) continue;

            var hit = Physics2D.OverlapPoint(position + SCAN_POSITION[i], blendLayer);
            if (hit)
            {
                var neighbor = hit.GetComponentInChildren<SpriteBlender>();
                if (neighbor != null && neighbor.Rules == rules) neighbor.Blend(false);
            }
        }
    }*/

    [ContextMenu("ReblendNeighbors")]
    public void ReblendNeighbors()
    {
        // Centre of this tile in world space
        position = (Vector2)transform.position + offset;

        // Grab every collider in a 3×3 neighbourhood with one call
        const float radius = 1.5f;                           // covers ±1 tile at default scale
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, blendLayer);

        foreach (var hit in hits)
        {
            // Skip self and anything that is not a SpriteBlender of the same rule set
            var neighbor = hit.GetComponentInChildren<SpriteBlender>();
            if (neighbor == null || neighbor == this || neighbor.Rules != rules) continue;

            // Filter out colliders that are outside the 3×3 grid (OverlapCircle can pick up extras)
            Vector2 delta = (Vector2)neighbor.transform.position + neighbor.offset - position;
            if (Mathf.Abs(delta.x) > 1 || Mathf.Abs(delta.y) > 1) continue;

            neighbor.RequestReblend();
        }
    }


    private static readonly Dictionary<Sprite, Vector2[]> shapeCache = new Dictionary<Sprite, Vector2[]>();
    private List<Vector2> physicsShape = new List<Vector2>();
    [ContextMenu("Reshape Collider")]
    private void ReshapeCollider(Sprite sprite)
    {
        if (!shapeCache.TryGetValue(sprite, out var verts))
        {
            physicsShape.Clear();
            sprite.GetPhysicsShape(0, physicsShape);
            verts = physicsShape.ToArray();
        }
        movementBlocker.SetPath(0, verts);
    }

    private string PrintNeighbor(IBool[] neighbors)
    {
        var builder = position + "\nNeighbor Actual:\n";
        for (int i = 0; i < neighbors.Length; i++)
        {
            builder += neighbors[i].ToSymbol(true) + " ";
            if (i == 2 || i == 5) builder += "\n";
        }
        return builder;
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
