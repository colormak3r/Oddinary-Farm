using UnityEngine;
using UnityEngine.UI;

public class MapUI : UIBehaviour
{
    public static MapUI Main;
    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }

        elevationMapImage.rectTransform.sizeDelta = new Vector2(currentZoomLevel, currentZoomLevel);
        heatMapImage.rectTransform.sizeDelta = new Vector2(currentZoomLevel, currentZoomLevel);
    }

    [Header("Map Settings")]
    [SerializeField]
    private Image elevationMapImage;
    [SerializeField]
    private Image heatMapImage;

    [Header("Map Debugs")]
    [SerializeField]
    private int currentZoomLevel = 200;
    [SerializeField]
    private int minZoomLevel = 100;
    [SerializeField]
    private int maxZoomLevel = 600;
    [SerializeField]
    private int zoomStep = 50;
    [SerializeField]
    private float lerpSpeed = 10f;
    [SerializeField]
    private Vector2 playerPositionOffset = new Vector2(0, 0.5f);

    private Vector2 position_cached;
    private Vector2Int mapSize_cached;

    private Vector2 mapPosition;

    public void UpdateElevationMap(Sprite mapSprite)
    {
        elevationMapImage.sprite = mapSprite;
    }

    public void UpdateHeatMap(Sprite heatMapSprite)
    {
        heatMapImage.sprite = heatMapSprite;
    }

    public void UpdatePlayerPosition(Vector2 position, Vector2Int mapSize)
    {
        position = position + playerPositionOffset;

        position_cached = position;
        mapSize_cached = mapSize;

        CalculateMapPosition(position_cached, mapSize_cached);
    }

    private void CalculateMapPosition(Vector2 position, Vector2Int mapSize)
    {
        // Calculate half map dimensions in world units (assuming map is centered at (0,0))
        float halfMapWidth = mapSize.x / 2f;
        float halfMapHeight = mapSize.y / 2f;

        // Normalize the player's world position to the range [-1, 1]
        float normalizedX = position.x / halfMapWidth;
        float normalizedY = position.y / halfMapHeight;

        // Get the RectTransform of the elevation map image
        RectTransform mapRect = elevationMapImage.rectTransform;

        // Map the normalized coordinates to UI coordinates.
        // With a centered pivot, a normalized value of 1 corresponds to half the image's width/height.
        float uiX = normalizedX * (mapRect.rect.width / 2f);
        float uiY = normalizedY * (mapRect.rect.height / 2f);

        // Update the player icon's anchored position on the map UI
        mapPosition = new Vector2(-uiX, -uiY);
    }

    private void Update()
    {
        elevationMapImage.rectTransform.anchoredPosition = Vector2.Lerp(elevationMapImage.rectTransform.anchoredPosition, mapPosition, lerpSpeed * Time.deltaTime);
        heatMapImage.rectTransform.anchoredPosition = Vector2.Lerp(heatMapImage.rectTransform.anchoredPosition, mapPosition, lerpSpeed * Time.deltaTime);
    }

    public void ZoomIn()
    {
        currentZoomLevel += zoomStep;
        if (currentZoomLevel > maxZoomLevel)
        {
            currentZoomLevel = maxZoomLevel;
        }

        elevationMapImage.rectTransform.sizeDelta = new Vector2(currentZoomLevel, currentZoomLevel);
        heatMapImage.rectTransform.sizeDelta = new Vector2(currentZoomLevel, currentZoomLevel);
        CalculateMapPosition(position_cached, mapSize_cached);
        elevationMapImage.rectTransform.anchoredPosition = mapPosition;
        heatMapImage.rectTransform.anchoredPosition = mapPosition;
    }
    public void ZoomOut()
    {
        currentZoomLevel -= zoomStep;
        if (currentZoomLevel < minZoomLevel)
        {
            currentZoomLevel = minZoomLevel;
        }
        elevationMapImage.rectTransform.sizeDelta = new Vector2(currentZoomLevel, currentZoomLevel);
        heatMapImage.rectTransform.sizeDelta = new Vector2(currentZoomLevel, currentZoomLevel);
        CalculateMapPosition(position_cached, mapSize_cached);
        elevationMapImage.rectTransform.anchoredPosition = mapPosition;
        heatMapImage.rectTransform.anchoredPosition = mapPosition;
    }
}
