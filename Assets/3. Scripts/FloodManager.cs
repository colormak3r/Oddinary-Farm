using System;
using System.Collections;
using Unity.Jobs;
using Unity.Netcode;
using UnityEngine;

public class FloodManager : NetworkBehaviour
{
    public static FloodManager Main { get; private set; }

    [Header("Settings")]
    [SerializeField]
    private bool canFlood = true;
    [SerializeField]
    private bool asap;
    [SerializeField]
    private float baseFloodLevel = 0.4f;
    [SerializeField]
    private float safeMultiplier = 3f;
    [SerializeField]
    private float depthMultiplier = 3f;
    [SerializeField]
    private int floodStartDate = 7;
    [SerializeField]
    private float floodCompleteDuration = 1;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<float> CurrentFloodLevel = new NetworkVariable<float>();
    [SerializeField]
    private float floodLevelChangePerHour;
    public float FloodLevelChangePerHour => floodLevelChangePerHour;
    public float CurrentFloodLevelValue => CurrentFloodLevel.Value;
    public float CurrentSafeLevel => Mathf.Clamp01(CurrentFloodLevel.Value + safeMultiplier * floodLevelChangePerHour);
    public float CurrentDepthLevel => Mathf.Clamp01(CurrentFloodLevel.Value - depthMultiplier * floodLevelChangePerHour);
    public float DepthRange => depthMultiplier * floodLevelChangePerHour;
    public float WaterRange => (safeMultiplier + depthMultiplier) * floodLevelChangePerHour;
    public float BaseFloodLevel => baseFloodLevel;
    public Action<float, float, float> OnFloodLevelChanged;

    private Coroutine floodCoroutine;
    private void Awake()
    {
        if (Main)
            Destroy(gameObject);
        else
            Main = this;
    }

    protected override void OnNetworkPostSpawn()
    {
        CurrentFloodLevel.OnValueChanged += HandleCurrentFloodLevelChanged;
        HandleCurrentFloodLevelChanged(0, CurrentFloodLevelValue);

        if (IsServer)
        {
            CurrentFloodLevel.Value = baseFloodLevel;
            TimeManager.Main.OnDateChanged.AddListener(HandleDayChanged);
        }
    }


    public override void OnNetworkDespawn()
    {
        CurrentFloodLevel.OnValueChanged -= HandleCurrentFloodLevelChanged;
        if (IsServer)
        {
            TimeManager.Main.OnDateChanged.RemoveListener(HandleDayChanged);
        }
    }

    private void HandleDayChanged(int currentDay)
    {
        if (currentDay >= floodStartDate)
        {
            if (floodCoroutine == null && canFlood)
                floodCoroutine = StartCoroutine(FloodCoroutine());
        }
    }

    private void HandleCurrentFloodLevelChanged(float previousValue, float newValue)
    {
        OnFloodLevelChanged?.Invoke(newValue, CurrentSafeLevel, CurrentDepthLevel);
    }

    public void Initialize(float highestElevation)
    {
        floodLevelChangePerHour = (highestElevation - baseFloodLevel) / (floodCompleteDuration * 24);
    }
    private IEnumerator FloodCoroutine()
    {
        while (CurrentFloodLevel.Value < 1.01f)
        {
            CurrentFloodLevel.Value += floodLevelChangePerHour;
            if (asap)
                yield return new WaitForSeconds(0.1f);
            else
                yield return new WaitForSeconds(TimeManager.Main.HourDuration);
        }
    }

    [ContextMenu("Start Normal Flood")]
    public void StartNormalFlood()
    {
        StartFloodingRpc(false);
    }

    [ContextMenu("Start Instant Flood")]
    public void StartInstantFlood()
    {
        StartFloodingRpc(true);
    }

    [Rpc(SendTo.Server)]
    private void StartFloodingRpc(bool asap)
    {
        this.asap = asap;
        if (floodCoroutine == null)
            floodCoroutine = StartCoroutine(FloodCoroutine());
    }

    public void SetCanFlood(bool canFlood)
    {
        SetCanFloodRpc(canFlood);
    }

    [Rpc(SendTo.Server)]
    private void SetCanFloodRpc(bool canFlood)
    {
        this.canFlood = canFlood;
        CurrentFloodLevel.Value = baseFloodLevel;
    }

    public void SetFloodLevel(float floodLevel)
    {
        SetFloodLevelRpc(floodLevel);
    }

    [Rpc(SendTo.Server)]
    private void SetFloodLevelRpc(float floodLevel)
    {
        if (floodCoroutine != null) StopCoroutine(floodCoroutine);
        floodCoroutine = null;

        CurrentFloodLevel.Value = floodLevel;
    }
}
