using UnityEngine;
using UnityEngine.Rendering;

// Referencing https://www.redblobgames.com/maps/terrain-from-noise/
/// <summary>
/// Mixes Noise function from PerlinNoiseGenerator with a distance function to make an island
/// </summary>
public class ElevationMap : PerlinNoiseGenerator
{
    /*[Header("Elevation Map Settings")]
    [SerializeField]
    private float islandSharpness = 2f; // Adjust this value to change the island shape sharpness*/
    private float maxValue;
    public float MaxValue => maxValue;

    // NOTE: In the future you may want to make an enum and encapulate different distance functions like SquareBump, EuclideanSquared, Diagonal, etc.
    protected override float GetValue(float x, float y, Vector2Int mapSize)
    {
        // Generate the island shape
        var noise = base.GetValue(x, y, mapSize);       // Base map coordinate generator; raw perlin noise generator
        var nx = 2 * x / mapSize.x - 1;     // Convert x and y to a range of [0, 1]
        var ny = 2 * y / mapSize.y - 1;
        // NOTE: Consider using mathf.pow(x, 2) for clarity, or add a Square() function to a utility class
        var d = 1 - (1 - nx * nx) * (1 - ny * ny);      // Apply radial mask using inverted bell curve; square bump formula
        var value = Mathf.Clamp01(Mathf.Lerp(noise, 1 - d, 0.5f)); // Mix in falloff with the perlin noise detail; e = lerp(e, 1 - d, mix)
        if (value > maxValue)
            maxValue = value;

        return value;

        /* // Original perlin noise value
         var noise = base.GetValue(x, y, mapSize);

         // Normalize coordinates to range [-1, 1]
         var nx = (2f * x / mapSize.x) - 1f;
         var ny = (2f * y / mapSize.y) - 1f;

         // Calculate distance from center
         var distance = Mathf.Sqrt(nx * nx + ny * ny);

         // Island mask (adjustable power changes island sharpness)
         var mask = Mathf.Clamp01(1 - Mathf.Pow(distance, islandSharpness));

         // Blend the noise with the island mask
         return Mathf.Clamp01(noise * mask);*/
    }
}
