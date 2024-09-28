using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Properties;
using ColorMak3r.Utility;
using System;

public class FarmPlot : NetworkBehaviour, IWaterable
{
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
    }

    public override void OnNetworkDespawn()
    {
        IsWatered.OnValueChanged -= HandleIsWateredChanged;
    }

    private void HandleIsWateredChanged(bool previousValue, bool newValue)
    {
        var colorValue = newValue ? 0.5f : 1f;
        spriteRenderer.color = new Color(colorValue, colorValue, colorValue);
    }


    public void GetWatered(float duration)
    {
        GetWateredRpc(duration);
    }

    [Rpc(SendTo.Server)]
    private void GetWateredRpc(float duration)
    {
        if (IsWatered.Value) StopAllCoroutines();
        IsWatered.Value = true;

        StartCoroutine(WateredCoroutine(duration));
    }

    private IEnumerator WateredCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        IsWatered.Value = false;
    }
}
