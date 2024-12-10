using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ResourceGenerator : PerlinNoiseGenerator
{
    [Header("Resource Settings")]
    [SerializeField]
    private GameObject[] resourcePrefabs;
    [SerializeField]
    private int renderDistance = 20;
    [SerializeField]
    private int rarity = 4;

    private HashSet<Vector2Int> resourcePositions = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> ResourcePositions => resourcePositions;

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            StartCoroutine(GenerateResourceCoroutine(Vector2.zero));
        }
    }

    protected override void OnMapGenerated()
    {
        var resourceMapTexture = new Texture2D(mapSize.x, mapSize.y);

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                resourceMapTexture.SetPixel(x, y, Color.clear);
                float maxValue = 0;
                // there are more efficient algorithms than this
                for (int xx = -rarity; xx <= rarity; xx++)
                {
                    for (int yy = -rarity; yy <= rarity; yy++)
                    {
                        int xn = xx + x;
                        int yn = yy + y;
                        // optionally check that (dx*dx + dy*dy <= rarity * (rarity + 1))
                        if (0 <= xn && xn < mapSize.x && 0 <= yn && yn < mapSize.y)
                        {
                            float e = map[xn, yn];
                            if (e > maxValue) { maxValue = e; }
                        }
                    }
                }
                if (map[x, y] == maxValue)
                {
                    resourcePositions.Add(new Vector2Int(x, y));
                    if (WorldGenerator.Main.IsValidResourcePosition(x, y))
                        resourceMapTexture.SetPixel(x, y, Color.red);
                }
            }
        }


        resourceMapTexture.Apply();
        var resourceMapSprite = Sprite.Create(resourceMapTexture, new Rect(0, 0, mapSize.x, mapSize.y), Vector2.zero);
        resourceMapSprite.texture.filterMode = FilterMode.Point;

        MapUI.Main.UpdateResourceMap(resourceMapSprite);
    }

    public IEnumerator GenerateResourceCoroutine(Vector2 position)
    {
        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);

        position = position.SnapToGrid();
        for (int x = -renderDistance; x < renderDistance; x++)
        {
            for (int y = -renderDistance; y < renderDistance; y++)
            {
                var pos = new Vector2Int(x + halfMapSizeX, y + halfMapSizeY);
                if (resourcePositions.Contains(pos) && WorldGenerator.Main.IsValidResourcePosition(x, y))
                {
                    var res = Instantiate(resourcePrefabs.GetRandomElement(), new Vector3(x, y - 0.5f, 0), Quaternion.identity);
                    res.GetComponent<NetworkObject>().Spawn();
                }
            }
        }

        yield return null;
    }
}
