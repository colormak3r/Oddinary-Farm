using ColorMak3r.Utility;
using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    private WorldGenerator worldGenerator;

    private Vector2 position_cached;

    private void Start()
    {
        worldGenerator = WorldGenerator.Main;
    }

    private void Update()
    {
        if (!GameManager.Main.IsInitialized) return;
        var currentPosition = ((Vector2)transform.position).SnapToGrid();
        if (position_cached != currentPosition)
        {
            position_cached = currentPosition;
            StartCoroutine(worldGenerator.BuildWorld(currentPosition));
        }
    }
}
