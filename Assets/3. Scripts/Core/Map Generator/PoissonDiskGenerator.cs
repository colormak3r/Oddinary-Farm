using UnityEngine;

public class PoissonDiskGenerator : MapGenerator
{
    [Header("Map Settings")]
    [SerializeField]
    private float minDistance = 1.0f;
    [SerializeField]
    private int newPointsCount = 30;

    public override void GenerateMap(Vector2Int mapSize)
    {
        var halfMapSize = mapSize / 2;
        mapTexture = new Texture2D(mapSize.x, mapSize.y);
        mapTexture.filterMode = FilterMode.Point;

        var mapColorLength = mapColors.Length;

        // Generate the map
        rawMap = new Offset2DArray<float>(-halfMapSize.x, halfMapSize.x, -halfMapSize.y, halfMapSize.y);
        for (int x = -halfMapSize.x; x < halfMapSize.x; x++)
        {
            for (int y = -halfMapSize.y; y < halfMapSize.y; y++)
            {
                //Pseudo-randomly generate points
            }
        }

        mapTexture.Apply();
    }
}