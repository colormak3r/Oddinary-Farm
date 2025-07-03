/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

using Unity.Netcode;
using UnityEngine;
using ColorMak3r.Utility;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;
using System;

public class MapElement : NetworkBehaviour
{
    [Header("Map Settings")]
    [SerializeField]
    private Vector2[] positions;
    [SerializeField]
    private Color color;

    [Header("Heat Settings")]
    [SerializeField]
    [Range(0f, 1f)]
    private float defaultHeatValue = 0.5f;
    [SerializeField]
    [Range(0f, 1f)]
    private float heatDecayPerDay = 0.1f;

    [Header("Debugs")]
    [SerializeField]
    private bool showGizmos = false;
    [SerializeField]
    private NetworkVariable<float> CurrentHeat = new NetworkVariable<float>();
    public float CurrentHeatValue => CurrentHeat.Value;

    private float heatDecayPerHour => heatDecayPerDay / 24f;

    private Vector2Int[] snappedPositions;

    private void Awake()
    {
        List<Vector2Int> snappedPositions = new List<Vector2Int>();
        foreach (var position in positions)
        {
            var localPos = ((Vector2)transform.position + position).SnapToGrid();
            snappedPositions.Add(new Vector2Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y)));
        }
        this.snappedPositions = snappedPositions.ToArray();
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(WaitWorldGeneratorLoad());
        CurrentHeat.OnValueChanged += HandleOnCurrentHeatChanged;

        if (IsServer)
        {
            TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
            CurrentHeat.Value = defaultHeatValue;
        }
    }

    public override void OnNetworkDespawn()
    {
        WorldGenerator.Main.ResetMinimap(snappedPositions);

        if (IsServer)
        {
            TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
            CurrentHeat.Value = 0f; // Reset heat value on despawn
        }

        CurrentHeat.OnValueChanged -= HandleOnCurrentHeatChanged;
    }

    private void HandleOnCurrentHeatChanged(float previousValue, float newValue)
    {
        HeatMapManager.Main.UpdateHeatMap(snappedPositions, newValue);
    }

    private void HandleOnHourChanged(int currentHour)
    {
        /*if (CurrentHeatValue > minHeat)
        {
            float newHeatValue = Mathf.Max(CurrentHeatValue - heatDecayPerHour, minHeat);
            CurrentHeat.Value = newHeatValue;
        }*/
    }

    private IEnumerator WaitWorldGeneratorLoad()
    {
        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);
        WorldGenerator.Main.UpdateMinimap(snappedPositions, color);
    }

    public void ResetHeatValue()
    {
        if (!IsServer) return;
        CurrentHeat.Value = defaultHeatValue;
    }

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            Gizmos.color = color;
            foreach (var position in positions)
            {
                var localPos = ((Vector2)transform.position + position).SnapToGrid();
                Gizmos.DrawCube(localPos, Vector3.one * 1f);
            }
        }
    }
}
