using UnityEngine;

public class RarityMap : PerlinNoiseGenerator
{
    [Header("Rarity Settings")]
    [SerializeField]
    private int rarity = 4;

    protected override void TransformMap(Vector2Int mapSize)
    {
        // constants & scratch buffers
        int halfX = mapSize.x / 2;          // 200 for a 400 × 400 map
        int halfY = mapSize.y / 2;

        // one byte per tile that says “this is a rarity peak”
        bool[,] isPeak = new bool[mapSize.x, mapSize.y];

        // pass 1 – detect local maxima
        for (int x = rawMap.minX; x < rawMap.maxX; ++x)
            for (int y = rawMap.minY; y < rawMap.maxY; ++y)
            {
                float candidate = rawMap[x, y];
                float maxInNeighbourhood = candidate;

                for (int dx = -rarity; dx <= rarity; ++dx)
                    for (int dy = -rarity; dy <= rarity; ++dy)
                    {
                        if (dx == 0 && dy == 0) continue;                 // skip self

                        if (rawMap.GetElementSafe(x + dx, y + dy, out var v))
                            if (v > maxInNeighbourhood) maxInNeighbourhood = v;
                    }

                if (Mathf.Approximately(candidate, maxInNeighbourhood))
                    isPeak[x + halfX, y + halfY] = true;              // mark peak
            }

        // pass 2 – write back to rawMap
        for (int x = rawMap.minX; x < rawMap.maxX; ++x)
            for (int y = rawMap.minY; y < rawMap.maxY; ++y)
                rawMap[x, y] = isPeak[x + halfX, y + halfY] ? 1f : 0f;
    }
}
