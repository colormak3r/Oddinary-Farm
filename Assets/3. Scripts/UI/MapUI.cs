using System.Collections;
using System.Collections.Generic;
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
    }

    [Header("Settings")]
    [SerializeField]
    private Image elevationMapImage;
    [SerializeField]
    private Image resourceMapImage;
    [SerializeField]
    private Image playerIcon;

    public void UpdateElevationMap(Sprite mapSprite)
    {
        elevationMapImage.sprite = mapSprite;
    }

    public void UpdatePlayerPosition(Vector2 position, Vector2Int mapSize)
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
        playerIcon.rectTransform.anchoredPosition = new Vector2(uiX, uiY);
    }

    public void UpdateResourceMap(Sprite mapSprite)
    {
        resourceMapImage.sprite = mapSprite;
    }
}
