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
    //[SerializeField]
    //private int rarity = 4;
    [SerializeField]
    private float chance = 0.05f;

    private HashSet<Vector2Int> resourcePositions = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> ResourcePositions => resourcePositions;

    /*protected override IEnumerator BuildMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        for (int x = -halfMapSize.x; x < halfMapSize.y; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                *//*float maxValue = 0;
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

                if (map[x, y] == maxValue)*//*
                if (Random.value < chance)
                {
                    resourcePositions.Add(new Vector2Int(x, y));
                    if (WorldGenerator.Main.IsValidResourcePosition(x, y))
                    {
                        SpawnResource(x, y);
                        yield return null;
                    }
                }
            }
        }
    }
*/
    private void SpawnResource(int x, int y)
    {
        var res = Instantiate(resourcePrefabs.GetRandomElement(), new Vector3(x, y - 0.5f, 0), Quaternion.identity, transform);
        var resNetObject = res.GetComponent<NetworkObject>();
        resNetObject.TrySetParent(transform);
        resNetObject.Spawn();
    }
}
