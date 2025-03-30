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
    private float baseFloodLevel = 0.4f;
    [SerializeField]
    private int floodStartDate = 7;
    [SerializeField]
    private float floodCompleteDuration = 1;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<float> CurrentFloodLevel = new NetworkVariable<float>();
    public float CurrentFloodLevelValue => CurrentFloodLevel.Value;
    public float CurrentWaterLevel => CurrentFloodLevel.Value + 3 * floodLevelChangePerHour;
    public float BaseFloodLevel => baseFloodLevel;

    public Action<float, float> OnFloodLevelChanged;

    private float floodLevelChangePerHour;
    public float FloodLevelChangePerHour => floodLevelChangePerHour;

    private Coroutine floodCoroutine;
    private void Awake()
    {
        if (Main)
            Destroy(gameObject);
        else
            Main = this;
    }

    public override void OnNetworkSpawn()
    {
        CurrentFloodLevel.OnValueChanged += HandleCurrentFloodLevelChanged;
        HandleCurrentFloodLevelChanged(0, CurrentFloodLevelValue);

        if (IsServer)
        {
            CurrentFloodLevel.Value = baseFloodLevel;
            TimeManager.Main.OnDayChanged.AddListener(HandleDayChanged);
        }
    }


    public override void OnNetworkDespawn()
    {
        CurrentFloodLevel.OnValueChanged -= HandleCurrentFloodLevelChanged;
        if (IsServer)
        {
            TimeManager.Main.OnDayChanged.RemoveListener(HandleDayChanged);
        }
    }

    private void HandleDayChanged(int currentDay)
    {
        if (currentDay == floodStartDate)
        {
            if (floodCoroutine == null)
                floodCoroutine = StartCoroutine(FloodCoroutine());
        }
    }

    private void HandleCurrentFloodLevelChanged(float previousValue, float newValue)
    {
        OnFloodLevelChanged?.Invoke(newValue, newValue + floodLevelChangePerHour);
    }

    public void Initialize()
    {
        floodLevelChangePerHour = (WorldGenerator.Main.HighestElevation - baseFloodLevel) / (floodCompleteDuration * 24);
    }

    [ContextMenu("Flood")]
    public void Flood()
    {
        if (!IsServer) return;
        if (floodCoroutine == null)
            floodCoroutine = StartCoroutine(FloodCoroutine());
    }

    private IEnumerator FloodCoroutine()
    {
        while (CurrentFloodLevel.Value < 1.01f)
        {
            CurrentFloodLevel.Value += floodLevelChangePerHour;
            yield return new WaitForSeconds(TimeManager.Main.HourDuration);
        }
    }
}
