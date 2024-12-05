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

    public void UpdateElevationMap(Sprite mapSprite)
    {
        elevationMapImage.sprite = mapSprite;
    }

    public void UpdateResourceMap(Sprite mapSprite)
    {
        resourceMapImage.sprite = mapSprite;
    }
}
