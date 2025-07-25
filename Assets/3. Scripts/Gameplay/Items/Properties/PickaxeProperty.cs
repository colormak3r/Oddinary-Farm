using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pickaxe Property", menuName = "Scriptable Objects/Item/Pickaxe")]
public class PickaxeProperty : MeleeWeaponProperty
{
    [Header("Pickaxe Settings")]
    [SerializeField]
    private Vector2 size = Vector2.one;
    public Vector2 Size => size;

    [Header("Preview Properties")]
    [SerializeField]
    private Sprite previewIconSprite;
    [SerializeField]
    private Vector2 previewIconOffset;
    [SerializeField]
    private Color previewValidColor = new Color(113, 170, 52);
    [SerializeField]
    private Color previewInvalidColor = new Color(230, 72, 46);
    public Sprite PreviewIconSprite => previewIconSprite;
    public Vector2 PreviewIconOffset => previewIconOffset;
    public Color PreviewValidColor => previewValidColor;
    public Color PreviewInvalidColor => previewInvalidColor;
}
