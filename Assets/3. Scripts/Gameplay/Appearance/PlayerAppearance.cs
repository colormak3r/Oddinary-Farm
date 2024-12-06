using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAppearance : NetworkBehaviour
{
    public static PlayerAppearance Owner;

    [Header("Settings")]
    [SerializeField]
    private Face defaultFace;
    [SerializeField]
    private Head defaultHead;
    [SerializeField]
    private Hat defaultHat;
    [SerializeField]
    private Outfit defaultOutfit;
    [SerializeField]
    private Outfit alternateOutfit;

    [Header("Required Components")]
    [SerializeField]
    private SpriteRenderer faceRenderer;
    [SerializeField]
    private SpriteRenderer headRenderer;
    [SerializeField]
    private SpriteRenderer hatRenderer;
    [SerializeField]
    private SpriteRenderer torsoRenderer;
    [SerializeField]
    private SpriteRenderer leftArmRenderer;
    [SerializeField]
    private SpriteRenderer rightArmRenderer;
    [SerializeField]
    private SpriteRenderer leftLegRenderer;
    [SerializeField]
    private SpriteRenderer rightLegRenderer;

    private Face currentFace;
    private Head currentHead;
    private Hat currentHat;
    private Outfit currentOutfit;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Owner = this;
            UpdateFace(defaultFace);
            UpdateHead(defaultHead);
            UpdateHat(defaultHat);
            UpdateOutfit(defaultOutfit);

            AppearanceUI.Main.Initialize(defaultFace, defaultHead, defaultHat, defaultOutfit);
        }
    }

    public void UpdateFace(Face face)
    {
        UpdateFaceRpc(face);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateFaceRpc(Face face)
    {
        currentFace = face;
        faceRenderer.sprite = face.DisplaySprite;
    }

    public void UpdateHead(Head head)
    {
        UpdateHeadRpc(head);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateHeadRpc(Head head)
    {
        currentHead = head;
        headRenderer.sprite = head.DisplaySprite;
    }

    public void UpdateHat(Hat hat)
    {
        UpdateHatRpc(hat);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateHatRpc(Hat hat)
    {
        currentHat = hat;
        if (hat.name == "No Hat")
            hatRenderer.sprite = null;
        else
            hatRenderer.sprite = hat.DisplaySprite;
    }

    [ContextMenu("Update Outfit")]
    public void UpdateOutfit()
    {
        if (currentOutfit == defaultOutfit)
            UpdateOutfitRpc(alternateOutfit);
        else
            UpdateOutfitRpc(defaultOutfit);
    }

    public void UpdateOutfit(Outfit outfit)
    {
        UpdateOutfitRpc(outfit);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateOutfitRpc(Outfit outfit)
    {
        currentOutfit = outfit;
        torsoRenderer.sprite = outfit.TorsoSprite;
        leftArmRenderer.sprite = outfit.LeftArmSprite;
        rightArmRenderer.sprite = outfit.RightArmSprite;
        leftLegRenderer.sprite = outfit.LeftLegSprite;
        rightLegRenderer.sprite = outfit.RightLegSprite;
    }
}
