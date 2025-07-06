using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class ElevationMap : PerlinNoiseGenerator
{
    /*[Header("Elevation Map Settings")]
    [SerializeField]
    private float islandSharpness = 2f; // Adjust this value to change the island shape sharpness*/
    private float highestElevaionValue;
    public float HighestElevationValue
    {
        get
        {
            if (showDebugs) Debug.Log($"Highest Elevation Value: {highestElevaionValue} at Point: {highestElevationPoint}");
            return highestElevaionValue;
        }
    }
    private Vector2 highestElevationPoint;
    public Vector2 HighestElevationPoint => highestElevationPoint;

    protected override float GetValue(float x, float y, Vector2Int mapSize)
    {
        // Generate the island shape
        var noise = base.GetValue(x, y, mapSize);
        var nx = 2 * x / mapSize.x - 1;
        var ny = 2 * y / mapSize.y - 1;
        var d = 1 - (1 - nx * nx) * (1 - ny * ny);
        var value = Mathf.Clamp01(Mathf.Lerp(noise, 1 - d, 0.5f));

        return value;
    }

    protected override void TransformMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                var value = rawMap[x, y];
                // Find the highest elevation point
                if (value > highestElevaionValue)
                {
                    highestElevaionValue = value;
                    highestElevationPoint = new Vector2(x, y);
                }
            }
        }
    }
}
