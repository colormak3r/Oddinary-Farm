using Unity.Netcode;
using UnityEngine;
using ColorMak3r.Utility;
using System.Collections;

public class MapElement : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Color color;

    public override void OnNetworkSpawn()
    {
        StartCoroutine(WaitWorldGeneratorLoad());
    }

    private IEnumerator WaitWorldGeneratorLoad()
    {
        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);

        // Update the minimap with the snapped position and color
        var positions = new Vector2Int[]
        {
            new Vector2Int((int)((Vector2)transform.position).SnapToGrid().x,
                           (int)((Vector2)transform.position).SnapToGrid().y)
        };
        WorldGenerator.Main.UpdateMinimap(positions, color);
    }
}
