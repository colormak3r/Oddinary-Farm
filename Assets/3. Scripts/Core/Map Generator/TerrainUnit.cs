using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUnit : MonoBehaviour
{
    [Header("Debugs")]
    [SerializeField]
    private TerrainProperty property;

    private SpriteRenderer spriteRenderer;

    public TerrainProperty Property => property;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    public void Initialize(TerrainProperty property)
    {
        this.property = property;
        spriteRenderer.sprite = this.property.Sprite;
    }
}
