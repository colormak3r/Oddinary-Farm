using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " Outfit", menuName = "Scriptable Objects/Appearance/Outfit")]
public class Outfit : AppearanceData
{
    [Header("Settings")]
    [SerializeField]
    private Sprite torsoSprite;
    [SerializeField]
    private Sprite leftArmSprite;
    [SerializeField]
    private Sprite rightArmSprite;
    [SerializeField]
    private Sprite leftLegSprite;
    [SerializeField]
    private Sprite rightLegSprite;

    public Sprite TorsoSprite => torsoSprite;
    public Sprite LeftArmSprite => leftArmSprite;
    public Sprite RightArmSprite => rightArmSprite;
    public Sprite LeftLegSprite => leftLegSprite;
    public Sprite RightLegSprite => rightLegSprite;
}
