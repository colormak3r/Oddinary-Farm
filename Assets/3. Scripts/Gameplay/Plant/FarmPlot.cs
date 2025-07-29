/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/24/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class FarmPlot : NetworkBehaviour, IWaterable
{
    [Header("Settings")]
    [SerializeField]
    private float duration = 50f;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<bool> IsWatered;
    public bool IsWateredValue => IsWatered.Value;
    [SerializeField]
    private NetworkVariable<Vector2> Size;

    private SpriteRenderer spriteRenderer;
    private SpriteBlender spriteBlender;
    private BoxCollider2D interactionCollider;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteBlender = GetComponent<SpriteBlender>();
        interactionCollider = GetComponent<BoxCollider2D>();
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

    public override void OnNetworkDespawn()
    {
        IsWatered.OnValueChanged -= HandleIsWateredChanged;

        if (IsServer)
        {
            WeatherManager.Main.OnRainStarted.AddListener(HandleRainStarted);
            WeatherManager.Main.OnRainStopped.AddListener(HandleRainStopped);
        }

        interactionCollider.enabled = false;
        spriteBlender.ReblendNeighbors();
    }

    private void HandleIsWateredChanged(bool previousValue, bool newValue)
    {
        var colorValue = newValue ? 0.5f : 1f;
        spriteRenderer.color = new Color(colorValue, colorValue, colorValue);
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
