using UnityEngine;

public class ElevationMap : PerlinNoiseGenerator
{
    protected override float GetValue(float x, float y, Vector2Int mapSize)
    {
        // Generate the island shape
        var noise = base.GetValue(x, y, mapSize);
        var nx = 2 * x / mapSize.x - 1;
        var ny = 2 * y / mapSize.y - 1;
        var d = 1 - (1 - nx * nx) * (1 - ny * ny);
        return Mathf.Clamp01(Mathf.Lerp(noise, 1 - d, 0.5f));
    }
}
