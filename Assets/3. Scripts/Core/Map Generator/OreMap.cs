/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/24/2025 (Khoa)
 * Notes:           Vibecoded with chatGPT
*/

using System.Collections.Generic;
using UnityEngine;

public class OreMap : PerlinNoiseGenerator
{
    [Header("Ore Map Settings")]
    [SerializeField]
    private float oreThreshold = 0.80f;                                 // “ore” if raw > 0.8
    [SerializeField]
    private int minCluster = 5;                                         // smallest legal patch
    [SerializeField]
    private int maxCluster = 10;                                        // absolute upper-bound on cluster size
    [SerializeField]
    private float deathzoneRatio = 0.25f;                               // dead-zone radius is 25% of map size (100 for 400×400 map)
    [SerializeField]
    private int maxExtraRadius = 3;                                     // extra dilation at rim

    protected override void TransformMap(Vector2Int mapSize)
    {
        Vector2Int half = mapSize / 2;                                  // 200,200 for 400×400
        Vector2 centre = Vector2.zero;                                  // rawMap is indexed –half ~ half
        float maxDist = half.magnitude;                                 // 283 for 400×400
        float innerRadius = mapSize.x * deathzoneRatio * 0.5f;          // true radius of the dead-zone
        float distRangeInv = 1f / (maxDist - innerRadius);

        // 1st pass – decide where ore is allowed to exist
        // We cache the decision in a temp bool array so the 2nd pass (dilation) is simpler.
        var oreMask = new bool[mapSize.x, mapSize.y];

        for (int x = -half.x; x < half.x; ++x)
        {
            for (int y = -half.y; y < half.y; ++y)
            {
                int px = x + half.x;              // texture indices (0 ~ size-1)
                int py = y + half.y;

                // radial distance and 0-1 gradient from innerRadius ~ edge
                float dist = Vector2.Distance(new Vector2(x, y), centre);
                if (dist < innerRadius)
                {
                    rawMap[x, y] = 0f;            // inside dead-zone, force “no ore”
                    continue;
                }

                float r01 = (dist - innerRadius) * distRangeInv;     // 0 at inner edge, 1 at map edge
                float localThr = Mathf.Lerp(0.95f, oreThreshold, r01);    // stricter near centre

                if (rawMap[x, y] >= localThr)
                {
                    oreMask[px, py] = true;       // mark as an ore seed
                }
                else
                {
                    rawMap[x, y] = 0f;            // wipe non-ore noise now, keeps later passes simple
                }
            }
        }

        // 2nd pass – grow every seed outward; cluster radius grows with r01
        int[,] extraRadiusLUT = new int[mapSize.x, mapSize.y];  // pre-compute radius per cell once
        for (int x = -half.x; x < half.x; ++x)
        {
            for (int y = -half.y; y < half.y; ++y)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centre);
                if (dist < innerRadius) continue;

                float r01 = (dist - innerRadius) * distRangeInv;
                extraRadiusLUT[x + half.x, y + half.y] = Mathf.RoundToInt(r01 * maxExtraRadius);
            }
        }

        // 3rd pass – remove tiny clusters; the minimum size grows toward the edge
        var visited = new bool[mapSize.x, mapSize.y];
        var stack = new Stack<Vector2Int>();

        for (int px = 0; px < mapSize.x; ++px)
        {
            for (int py = 0; py < mapSize.y; ++py)
            {
                if (visited[px, py] || !oreMask[px, py]) continue;

                // gather a cluster with DFS
                stack.Clear();
                var cluster = new List<Vector2Int>();
                stack.Push(new Vector2Int(px, py));
                while (stack.Count > 0)
                {
                    var p = stack.Pop();
                    if (visited[p.x, p.y]) continue;
                    visited[p.x, p.y] = true;
                    cluster.Add(p);

                    // 4-way neighbours
                    foreach (var n in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
                    {
                        int nx = p.x + n.x;
                        int ny = p.y + n.y;
                        if (nx < 0 || nx >= mapSize.x || ny < 0 || ny >= mapSize.y) continue;
                        if (!visited[nx, ny] && oreMask[nx, ny]) stack.Push(new Vector2Int(nx, ny));
                    }
                }

                // work out radial factor from cluster centre to decide minimum size
                Vector2 avg = Vector2.zero;
                foreach (var p in cluster) avg += new Vector2(p.x - half.x, p.y - half.y);
                avg /= cluster.Count;
                float dist01 = (avg.magnitude - innerRadius) * distRangeInv;
                int minSz = minCluster + Mathf.RoundToInt(minCluster * dist01);   // grows to edge

                // ---- NEW LOGIC ----------------------------------------------------
                if (cluster.Count < minSz)
                {
                    // too small -> erase entirely
                    foreach (var p in cluster) oreMask[p.x, p.y] = false;
                }
                else if (cluster.Count > maxCluster)
                {
                    // too big -> trim down to maxCluster cells
                    // keep the cells closest to the centroid so trimming looks natural
                    cluster.Sort((a, b) =>
                        ((a.x - avg.x) * (a.x - avg.x) + (a.y - avg.y) * (a.y - avg.y))
                        .CompareTo(
                        ((b.x - avg.x) * (b.x - avg.x) + (b.y - avg.y) * (b.y - avg.y))));

                    for (int i = maxCluster; i < cluster.Count; ++i)
                        oreMask[cluster[i].x, cluster[i].y] = false;
                }
                // -------------------------------------------------------------------

            }
        }

        // 4th pass – write the mask back to rawMap (ore = 1, non-ore = 0 for clarity)
        for (int x = -half.x; x < half.x; ++x)
        {
            for (int y = -half.y; y < half.y; ++y)
            {
                rawMap[x, y] = oreMask[x + half.x, y + half.y] ? 1f : 0f;
            }
        }
    }
}
