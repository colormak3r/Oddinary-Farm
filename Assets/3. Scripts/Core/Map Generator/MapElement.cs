using Unity.Netcode;
using UnityEngine;
using ColorMak3r.Utility;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;

public class MapElement : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Vector2[] positions;
    [SerializeField]
    private Color color;
    [SerializeField]
    private bool showGizmos = false;

    private Vector2Int[] snappedPositions;

    private void Awake()
    {
        List<Vector2Int> snappedPositions = new List<Vector2Int>();
        foreach (var position in positions)
        {
            var localPos = ((Vector2)transform.position + position).SnapToGrid();
            snappedPositions.Add(new Vector2Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y)));
        }
        this.snappedPositions = snappedPositions.ToArray();
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(WaitWorldGeneratorLoad());
    }

    public override void OnNetworkDespawn()
    {
        WorldGenerator.Main.ResetMinimap(snappedPositions);
    }

    private IEnumerator WaitWorldGeneratorLoad()
    {
        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);
        WorldGenerator.Main.UpdateMinimap(snappedPositions, color);
    }

    private void ResetMapColor()
    {
        WorldGenerator.Main.ResetMinimap(snappedPositions);
    }

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            Gizmos.color = color;
            foreach (var position in positions)
            {
                var localPos = ((Vector2)transform.position + position).SnapToGrid();
                Gizmos.DrawCube(localPos, Vector3.one * 1f);
            }
        }
    }
}
