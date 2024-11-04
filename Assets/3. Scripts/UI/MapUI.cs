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
    private Image mapImage;

    public void UpdateMap(Sprite mapSprite)
    {
        mapImage.sprite = mapSprite;
    }
}
