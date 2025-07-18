using UnityEngine;

public class RarityMap : PerlinNoiseGenerator
{
    [Header("Rarity Settings")]
    [SerializeField]
    private int rarity = 4;

    protected override void TransformMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                float maxValue = 0;
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
                            if (e > maxValue) { maxValue = e; }
                        }
                    }
                }

                if (rawMap[x, y] == maxValue)
                {
                    rawMap[x, y] = 1.0f;
                }
            }
        }

        var count = 0;
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                if (rawMap[x, y] != 1.0f) rawMap[x, y] = 0.0f;
                mapTexture.SetPixel(x + halfMapSize.x, y + halfMapSize.y, GetColor(rawMap[x, y]));
                if (rawMap[x, y] == 1.0f)
                {
                    count++;
                }
            }
        }
    }
}
