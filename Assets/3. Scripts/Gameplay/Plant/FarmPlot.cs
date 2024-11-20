using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Properties;
using ColorMak3r.Utility;
using System;

public class FarmPlot : NetworkBehaviour, IWaterable
{
    [Header("Settings")]
    [SerializeField]
    private float duration = 50f;
    [SerializeField]
    private Sprite singleSprite;
    [SerializeField]
    private Sprite multiSprite;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<bool> IsWatered;
    [SerializeField]
    private NetworkVariable<Vector2> Size;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D interactionCollider;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        interactionCollider = GetComponent<BoxCollider2D>();
    }

    public override void OnNetworkSpawn()
    {
        HandleIsWateredChanged(IsWatered.Value, IsWatered.Value);
        IsWatered.OnValueChanged += HandleIsWateredChanged;
        Size.OnValueChanged += HandleSizeChanged;

        if (IsServer)
        {
            WeatherManager.Main.OnRainStarted.AddListener(HandleRainStarted);
            WeatherManager.Main.OnRainStopped.AddListener(HandleRainStopped);
            if (WeatherManager.Main.IsRainning) HandleRainStarted();
        }
    }

    public override void OnNetworkDespawn()
    {
        IsWatered.OnValueChanged -= HandleIsWateredChanged;
        Size.OnValueChanged -= HandleSizeChanged;

        if (IsServer)
        {
            WeatherManager.Main.OnRainStarted.AddListener(HandleRainStarted);
            WeatherManager.Main.OnRainStopped.AddListener(HandleRainStopped);
        }
    }

    private void HandleIsWateredChanged(bool previousValue, bool newValue)
    {
        var colorValue = newValue ? 0.5f : 1f;
        spriteRenderer.color = new Color(colorValue, colorValue, colorValue);
    }

    private void HandleSizeChanged(Vector2 previousValue, Vector2 newValue)
    {
        spriteRenderer.sprite = newValue == Vector2.one ? singleSprite : multiSprite;
        spriteRenderer.size = newValue;
        interactionCollider.size = newValue;
        interactionCollider.offset = newValue == Vector2.one ? TransformUtility.HALF_UNIT_Y_V2 : Vector2.zero;
    }

    private void HandleRainStarted()
    {
        GetWateredRpc();
    }

    private void HandleRainStopped()
    {
        GetWateredRpc();
    }

    public void GetWatered()
    {
        GetWateredRpc();
    }

    public void ChangeSizeOnServer(Vector2 value)
    {
        if (!IsServer) return;

        Size.Value = value;
    }

    public void GetDriedOnServer()
    {
        if (IsWatered.Value) StopAllCoroutines();
        IsWatered.Value = WeatherManager.Main.IsRainning;
    }

    [Rpc(SendTo.Server)]
    private void GetWateredRpc()
    {
        if (IsWatered.Value) StopAllCoroutines();
        IsWatered.Value = true;

        StartCoroutine(WateredCoroutine(duration));
    }

    private IEnumerator WateredCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        GetDriedOnServer();
    }
}
