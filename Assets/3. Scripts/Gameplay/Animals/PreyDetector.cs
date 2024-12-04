using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public enum HuntingTime
{
    Neutral,
    Day,
    Night
}


public class PreyDetector : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool raycastPrey = true;
    [SerializeField]
    private float detectRange = 5f;
    [SerializeField]
    private float escapeRange = 7f;
    [SerializeField]
    private HuntingTime huntingTime;
    [SerializeField]
    private LayerMask preyMask;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private Transform currentPrey;
    [SerializeField]
    private float distanceToPrey;

    [HideInInspector]
    public UnityEvent<Transform> OnPreyDetected;
    [HideInInspector]
    public UnityEvent<Transform> OnPreyExited;

    private DistanceComparer distanceComparer;

    public Transform CurrentPrey => currentPrey;
    public float DistanceToPrey
    {
        get
        {
            distanceToPrey = currentPrey ? (transform.position - currentPrey.position).magnitude : float.PositiveInfinity;
            return distanceToPrey;
        }
    }

    private void Start()
    {
        distanceComparer = new DistanceComparer(transform, Vector2.zero);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            TimeManager.Main.OnDayStart.AddListener(HandleOnDayStart);
            TimeManager.Main.OnDayStart.AddListener(HandleOnNightStart);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            TimeManager.Main.OnDayStart.RemoveListener(HandleOnDayStart);
            TimeManager.Main.OnDayStart.RemoveListener(HandleOnNightStart);
        }
    }

    private void HandleOnNightStart()
    {
        if (huntingTime == HuntingTime.Day) currentPrey = null;
    }

    private void HandleOnDayStart()
    {
        if (huntingTime == HuntingTime.Night) currentPrey = null;
    }

    private void Update()
    {
        if (!IsServer) return;

        TrackPrey();
    }

    /// <summary>
    /// Scans for prey and detects new entries and exits.
    /// </summary>
    private void TrackPrey()
    {
        if (currentPrey == null)
        {
            var preys = ScanForPreys(transform.position);
            if (preys.Length <= 0) return;

            currentPrey = preys[Random.Range(0, preys.Length - 1)];

            if (currentPrey != null)
            {
                // Raycast to check for more desireable prey in line of sight
                if (raycastPrey)
                {
                    var hit = Physics2D.Raycast(transform.position, currentPrey.position - transform.position, detectRange, preyMask);
                    if (hit.transform != null) currentPrey = hit.transform;
                }

                if (currentPrey.TryGetComponent<EntityStatus>(out var entityStatus))
                {
                    entityStatus.OnDeathOnServer.AddListener(HandleOnPreyDie);
                }

                if (showDebugs) Debug.Log("New prey detected: " + currentPrey);
                OnPreyDetected?.Invoke(currentPrey);
            }
        }
        else
        {
            if (Vector3.Distance(currentPrey.position, transform.position) > escapeRange)
            {
                OnPreyExited?.Invoke(currentPrey);
                currentPrey = null;
            }
        }
    }


    /// <summary>
    /// Scans for prey within a specified range from a given position.
    /// </summary>
    /// <param name="position">The position to scan from.</param>
    /// <returns>An array of Transforms representing detected prey.</returns>
    private Transform[] ScanForPreys(Vector3 position)
    {
        var detectRange = this.detectRange;
        if (huntingTime == HuntingTime.Day && TimeManager.Main.IsNight || huntingTime == HuntingTime.Night && TimeManager.Main.IsDay)
            detectRange = 2;

        var hits = Physics2D.OverlapCircleAll(position, detectRange, preyMask);
        var result = new List<Transform>();
        result.Capacity = hits.Length;

        for (int i = 0; i < hits.Length; i++)
        {
            var prey = hits[i].transform;
            if (ValidateValidPrey(prey))
            {
                result.Add(prey);
            }
        }

        return result.ToArray();
    }

    protected virtual bool ValidateValidPrey(Transform prey)
    {
        return true;
    }

    private void HandleOnPreyDie()
    {
        if (showDebugs) Debug.Log($"{currentPrey} died");
        currentPrey = null;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (TimeManager.Main == null || !TimeManager.Main.IsInitialized) return;

        var detectRange = this.detectRange;
        if (huntingTime == HuntingTime.Day && TimeManager.Main.IsNight || huntingTime == HuntingTime.Night && TimeManager.Main.IsDay)
            detectRange = 2;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        if (currentPrey)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentPrey.position);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, escapeRange);
    }
}