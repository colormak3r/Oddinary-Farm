using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public enum ActiveTime
{
    Neutral,
    Day,
    Night
}


public class TargetDetector : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool raycastTarget = true;
    [SerializeField]
    private float detectRange = 5f;
    [SerializeField]
    private float escapeRange = 7f;
    [Tooltip("Detector get penaly range outsize of active time")]
    [SerializeField]
    private ActiveTime activeTime;
    [SerializeField]
    private float penaltyRange = 2f;
    [SerializeField]
    private Hostility targetHostility;
    [SerializeField]
    private LayerMask targetMask;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private Transform currentTarget;
    [SerializeField]
    private float distanceToTarget;

    [HideInInspector]
    public UnityEvent<Transform> OnTargetDetected;
    [HideInInspector]
    public UnityEvent<Transform> OnTargetEscaped;

    private DistanceComparer distanceComparer;

    public Transform CurrentTarget => currentTarget;
    public float DistanceToTarget
    {
        get
        {
            distanceToTarget = currentTarget ? (transform.position - currentTarget.position).magnitude : float.PositiveInfinity;
            return distanceToTarget;
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
        if (activeTime == ActiveTime.Day) currentTarget = null;
    }

    private void HandleOnDayStart()
    {
        if (activeTime == ActiveTime.Night) currentTarget = null;
    }

    private void Update()
    {
        if (!IsServer) return;

        TrackTarget();
    }

    private void TrackTarget()
    {
        if (currentTarget == null)
        {
            currentTarget = ScanForTarget(transform.position, out var targetStatus);
            if (currentTarget != null)
            {
                targetStatus.OnDeathOnServer.AddListener(HandleOnTargetDie);

                if (showDebugs) Debug.Log("New target detected: " + currentTarget);
                OnTargetDetected?.Invoke(currentTarget);
            }
        }
        else
        {
            if (Vector3.Distance(currentTarget.position, transform.position) > escapeRange)
            {
                OnTargetEscaped?.Invoke(currentTarget);
                currentTarget = null;
            }
        }
    }

    private Transform ScanForTarget(Vector3 position, out EntityStatus targetStatus)
    {
        targetStatus = null;
        var range = detectRange;
        if (activeTime == ActiveTime.Day && TimeManager.Main.IsNight || activeTime == ActiveTime.Night && TimeManager.Main.IsDay)
            range = penaltyRange;

        var hits = Physics2D.OverlapCircleAll(position, range, targetMask);
        if (hits.Length > 0)
        {
            Transform candidate = null;
            for (int i = 0; i < hits.Length; i++)
            {
                candidate = hits[i].transform;
                if (ValidateValidTarget(candidate, out targetStatus))
                {
                    // Raycast to check for more desireable prey in line of sight
                    if (raycastTarget)
                    {
                        var hit = Physics2D.Raycast(transform.position, candidate.position - transform.position, detectRange, targetMask);

                        if (hit.transform == candidate) break;
                        if (hit.transform != null && ValidateValidTarget(candidate, out targetStatus))
                            candidate = hit.transform;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return candidate;
        }
        else
        {
            return null;
        }
    }

    protected virtual bool ValidateValidTarget(Transform target, out EntityStatus targetStatus)
    {
        targetStatus = target.GetComponent<EntityStatus>();
        return targetStatus.Hostility == targetHostility && targetStatus != null;
    }

    private void HandleOnTargetDie()
    {
        if (showDebugs) Debug.Log($"{currentTarget} died");
        currentTarget = null;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (TimeManager.Main == null || !TimeManager.Main.IsInitialized) return;

        var detectRange = this.detectRange;
        if (activeTime == ActiveTime.Day && TimeManager.Main.IsNight || activeTime == ActiveTime.Night && TimeManager.Main.IsDay)
            detectRange = 2;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        if (currentTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, escapeRange);
    }
}