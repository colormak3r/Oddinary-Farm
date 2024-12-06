using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAppearance : NetworkBehaviour
{
    public static PlayerAppearance Owner;

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

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

    private NetworkVariable<Face> CurrentFace = new NetworkVariable<Face>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Head> CurrentHead = new NetworkVariable<Head>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Hat> CurrentHat = new NetworkVariable<Hat>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Outfit> CurrentOutfit = new NetworkVariable<Outfit>(default, default, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (CurrentFace.Value != null)
            HandleFaceChanged(default, CurrentFace.Value);
        if (CurrentHead.Value != null)
            HandleHeadChanged(default, CurrentHead.Value);
        if (CurrentHat.Value != null)
            HandleHatChanged(default, CurrentHat.Value);
        if (CurrentOutfit.Value != null)
            HandleOutfitChanged(default, CurrentOutfit.Value);

        CurrentFace.OnValueChanged += HandleFaceChanged;
        CurrentHead.OnValueChanged += HandleHeadChanged;
        CurrentHat.OnValueChanged += HandleHatChanged;
        CurrentOutfit.OnValueChanged += HandleOutfitChanged;

        if (IsOwner)
        {
            Owner = this;

            UpdateFace(AppearanceUI.Main.CurrentFace);
            UpdateHead(AppearanceUI.Main.CurrentHead);
            UpdateHat(AppearanceUI.Main.CurrentHat);
            UpdateOutfit(AppearanceUI.Main.CurrentOutfit);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentFace.OnValueChanged -= HandleFaceChanged;
        CurrentHead.OnValueChanged -= HandleHeadChanged;
        CurrentHat.OnValueChanged -= HandleHatChanged;
        CurrentOutfit.OnValueChanged -= HandleOutfitChanged;
    }

    private void HandleFaceChanged(Face previousValue, Face newValue)
    {
        if (showDebugs) Debug.Log($"Face Changed: {previousValue} -> {newValue}");
        faceRenderer.sprite = newValue.DisplaySprite;
    }

    private void HandleHeadChanged(Head previousValue, Head newValue)
    {
        if (showDebugs) Debug.Log($"Head Changed: {previousValue} -> {newValue}");
        headRenderer.sprite = newValue.DisplaySprite;
    }

    private void HandleHatChanged(Hat previousValue, Hat newValue)
    {
        if (showDebugs) Debug.Log($"Hat Changed: {previousValue} -> {newValue}");
        if (newValue.name == "No Hat")
            hatRenderer.sprite = null;
        else
            hatRenderer.sprite = newValue.DisplaySprite;
    }

    private void HandleOutfitChanged(Outfit previousValue, Outfit newValue)
    {
        if (showDebugs) Debug.Log($"Outfit Changed: {previousValue} -> {newValue}");
        torsoRenderer.sprite = newValue.TorsoSprite;
        leftArmRenderer.sprite = newValue.LeftArmSprite;
        rightArmRenderer.sprite = newValue.RightArmSprite;
        leftLegRenderer.sprite = newValue.LeftLegSprite;
        rightLegRenderer.sprite = newValue.RightLegSprite;
    }

    public void UpdateFace(Face face)
    {
        UpdateFaceRpc(face);
    }

    [Rpc(SendTo.Owner)]
    private void UpdateFaceRpc(Face face)
    {
        CurrentFace.Value = face;
    }

    public void UpdateHead(Head head)
    {
        UpdateHeadRpc(head);
    }

    [Rpc(SendTo.Owner)]
    private void UpdateHeadRpc(Head head)
    {
        CurrentHead.Value = head;
    }

    public void UpdateHat(Hat hat)
    {
        UpdateHatRpc(hat);
    }

    [Rpc(SendTo.Owner)]
    private void UpdateHatRpc(Hat hat)
    {
        CurrentHat.Value = hat;
    }

    public void UpdateOutfit(Outfit outfit)
    {
        UpdateOutfitRpc(outfit);
    }

    [Rpc(SendTo.Owner)]
    private void UpdateOutfitRpc(Outfit outfit)
    {
        CurrentOutfit.Value = outfit;
    }
}
