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

    public void Blend(bool reblend = false)
    {
        IBool[] neighbors = new IBool[9];
        SpriteBlender[] neigborBlenders = new SpriteBlender[9];
        if (showGizmos) scannedPosition.Clear();
        position = (Vector2)transform.position + offset;

        for (int i = 0; i < SCAN_POSITION.Length; i++)
        {
            if (i == 4) continue;

            if (showGizmos) scannedPosition.Add(position + SCAN_POSITION[i]);

            var hit = Physics2D.OverlapPoint(position + SCAN_POSITION[i], blendLayer);
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

        // Match and set the new sprite
        spriteRenderer.sprite = rules.GetMatchingSprite(neighbors);

        if (spriteRenderer.sprite == null)
        {
            if (showDebugs) Debug.Log("Cannot find matching sprite", this);
        }
        else if (reblend)
        {
            if (showDebugs) Debug.Log("Reblending neighbors", this);
            StartCoroutine(DelayBlendNeighbor(neigborBlenders));
        }

        if (movementBlocker)
        {
            if (showDebugs) Debug.Log("Reshaping collider", this);
            // Reshape the collider
            List<Vector2> physicsShape = new List<Vector2>();
            spriteRenderer.sprite.GetPhysicsShape(0, physicsShape);
            movementBlocker.SetPath(0, physicsShape);
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

            var hit = Physics2D.OverlapPoint(position + SCAN_POSITION[i], blendLayer);
            if (hit)
            {
                var neighbor = hit.GetComponentInChildren<SpriteBlender>();
                if (neighbor != null && neighbor.Rules == rules) neighbor.Blend();
            }
        }
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
