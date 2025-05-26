using System.IO.Ports;
using UnityEngine;

// Allows you to use static members of a class without prefixing them with the class name
using static UnityEngine.Rendering.PostProcessing.HistogramMonitor;

public class ResourceMap : PerlinNoiseGenerator
{
    [Header("Resource Settings")]
    [SerializeField]
    private int rarity = 4;     // defines the density of loot spawns
    [SerializeField]
    private int noResourceZone = 10;        // Amount of tiles to exclude from loot; player start area

    protected override void TransformMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)        // Loop over each pixel on the map/grid
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                float maxValue = 0;

                // Find local maximum value of map
                // there are more efficient algorithms than this
                for (int xx = -rarity; xx <= rarity; xx++)
                {
                    for (int yy = -rarity; yy <= rarity; yy++)
                    {
                        int xn = xx + x;
                        int yn = yy + y;
                        // optionally check that (dx*dx + dy*dy <= rarity * (rarity + 1))
                        if (-halfMapSize.x <= xn && xn < halfMapSize.x && -halfMapSize.y <= yn && yn < halfMapSize.y)
                        {
                            float e = rawMap[xn, yn];
                            if (e > maxValue)
                                maxValue = e;
                        }
                    }
                }

                if (rawMap[x, y] == maxValue)
                {
                    if (Mathf.Abs(x) < noResourceZone && Mathf.Abs(y) < noResourceZone)
                    {
                        rawMap[x, y] = 0.0f; // No resource zone
                    }
                    else
                    {
                        rawMap[x, y] = 1.0f;
                    }
                }
            }
        }

        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                if (rawMap[x, y] != 1.0f) rawMap[x, y] = 0.0f;
                mapTexture.SetPixel(x + halfMapSize.x, y + halfMapSize.y, GetColor(rawMap[x, y]));
            }
        }
    }
}
