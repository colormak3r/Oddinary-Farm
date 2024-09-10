using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUnit : MonoBehaviour
{
    [Header("Debugs")]
    [SerializeField]
    private TerrainProperty property;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    public void Initialize(TerrainProperty property)
    {
        spriteRenderer.sprite = property.Sprite;
    }
}
