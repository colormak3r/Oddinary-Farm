/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using UnityEngine;

public class HeatMapManager : MonoBehaviour
{
    public static HeatMapManager Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    [Header("Heat Map Settings")]
    [SerializeField]
    private Color heatColor = Color.red;
    [SerializeField]
    private GameObject heatMapCenterObject;
    [SerializeField]
    private float heatMapExclusionDistance = 50f;


    [Header("Heat Map Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool showHeatMapCenter = false;
    [SerializeField]
    private Vector2 heatCenter;
    public Vector2 HeatCenter => heatCenter;

    private Texture2D heatMapTexture;
    private float[,] heatMapData;

    public void Initialize(Vector2Int mapSize)
    {
        heatMapTexture = new Texture2D(mapSize.x, mapSize.y);
        heatMapData = new float[mapSize.x, mapSize.y];
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                heatMapTexture.SetPixel(i, j, Color.clear);
                heatMapData[i, j] = 0f; // Initialize heat map data
            }
        }
        heatMapTexture.Apply();

        ShowHeatMapCenter(showHeatMapCenter);
    }

    public void UpdateHeatMap(Vector2Int[] positions, float intensity)
    {
        // TODO: Make a coroutine to update the heat map in batch
        var mapSize = WorldGenerator.Main.MapSize;
        var halfMapSize = mapSize / 2;

        foreach (var position in positions)
        {
            if ((position - heatCenter).sqrMagnitude > heatMapExclusionDistance * heatMapExclusionDistance && intensity != 0f)
            {
                if (showDebugs) Debug.Log($"Position {position} is too far from heat center {heatCenter}, skipping update.");
                continue;
            }
            heatMapTexture.SetPixel(position.x + halfMapSize.x, position.y + halfMapSize.y, heatColor.SetAlpha(intensity));
            heatMapData[position.y + halfMapSize.y, position.x + halfMapSize.x] = intensity; // Update heat map data
        }

        heatCenter = GetHeatCenter(heatMapData, 0.4f).center - halfMapSize;
        heatMapCenterObject.transform.position = new Vector3(heatCenter.x, heatCenter.y, heatMapCenterObject.transform.position.z);

        heatMapTexture.Apply();
        UpdateMapTexture(heatMapTexture, mapSize);
    }

    private void UpdateMapTexture(Texture2D texture, Vector2Int mapSize)
    {
        var heatMapSprite = Sprite.Create(texture, new Rect(0, 0, mapSize.x, mapSize.y), new Vector2(0.5f, 0.5f));
        heatMapSprite.texture.filterMode = FilterMode.Point;
        MapUI.Main.UpdateHeatMap(heatMapSprite);
    }

    private (Vector2 center, float totalHeat) GetHeatCenter(float[,] heatMap, float threshold = 0f)
    {
        float sumX = 0;
        float sumY = 0;
        float total = 0;

        int width = heatMap.GetLength(1);
        int height = heatMap.GetLength(0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float heat = heatMap[y, x];
                if (heat < threshold) continue; // Filter out noise

                sumX += x * heat;
                sumY += y * heat;
                total += heat;
            }
        }

        if (total == 0) return (Vector2.zero, 0);

        float centerX = sumX / total;
        float centerY = sumY / total;

        return (new Vector2(centerX, centerY), total);
    }

    public void ShowHeatMapCenter(bool show)
    {
        showHeatMapCenter = show;
        heatMapCenterObject.SetActive(show);
    }
}
