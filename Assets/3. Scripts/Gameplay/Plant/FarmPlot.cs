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
    private NetworkVariable<bool> IsWatered;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        HandleIsWateredChanged(IsWatered.Value, IsWatered.Value);
        IsWatered.OnValueChanged += HandleIsWateredChanged;

        if (IsServer)
        {
            WeatherManager.Main.OnRainStarted.AddListener(HandleRainStarted);
            WeatherManager.Main.OnRainStopped.AddListener(HandleRainStopped);
            if (WeatherManager.Main.IsRainning) HandleRainStarted();
        }
    }

    private void HandleRainStarted()
    {
        GetWateredRpc();
    }

    private void HandleRainStopped()
    {
        GetWateredRpc();
    }

    public override void OnNetworkDespawn()
    {
        IsWatered.OnValueChanged -= HandleIsWateredChanged;

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

    public void GetWatered()
    {
        GetWateredRpc();
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
