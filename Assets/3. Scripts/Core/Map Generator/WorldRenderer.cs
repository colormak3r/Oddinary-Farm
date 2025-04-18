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

        if (position_cached != (Vector2)transform.position)
        {
            position_cached = transform.position;
            StartCoroutine(worldGenerator.BuildWorld(transform.position));
        }
    }
}
