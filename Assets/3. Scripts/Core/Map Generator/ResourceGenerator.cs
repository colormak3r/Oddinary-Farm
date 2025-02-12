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

    protected override IEnumerator BuildMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                /*float maxValue = 0;
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

                if (map[x, y] == maxValue)*/
                if (Random.value < chance)
                {
                    resourcePositions.Add(new Vector2Int(x, y));
                    if (WorldGenerator.Main.IsValidResourcePosition(x, y))
                    {
                        var i = x - halfMapSize.x;
                        var j = y - halfMapSize.y;
                        //Debug.Log($"Resource at {x}, {y}, {i}, {j}");
                        SpawnResource(i, j);
                        yield return null;
                    }
                }
            }
        }
    }

    private void SpawnResource(int i, int j)
    {
        var res = Instantiate(resourcePrefabs.GetRandomElement(), new Vector3(i, j - 0.5f, 0), Quaternion.identity);
        res.GetComponent<NetworkObject>().Spawn();
    }
}
