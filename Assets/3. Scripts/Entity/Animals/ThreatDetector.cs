using System;
using UnityEngine;

public class ThreatDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float detectRange = 7f;
    [SerializeField]
    private float escapeRange = 9f;
    [Tooltip("Detector get penaly range outsize of active time")]
    [SerializeField]
    private float penaltyRange = 2f;
    [SerializeField]
    private ActiveTime activeTime = ActiveTime.Neutral;
    [SerializeField]
    private LayerMask threatLayer;
    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool ignoreThreat;

    private float nextScan;

    private Transform currentThreat;
    public Transform CurrentThreat => currentThreat;

    private EntityStatus entityStatus;


    private void Start()
    {
        entityStatus = GetComponent<EntityStatus>();
    }

    private void Update()
    {
        if (ignoreThreat)
        {
            currentThreat = null;
            return;
        }

        if (Time.time < nextScan) return;
        nextScan = Time.time + 0.1f;

        TrackThreat();
    }

    private void TrackThreat()
    {
        if (currentThreat == null)
        {
            currentThreat = ScanForThreat(transform.position, out var threatStatus);
            if (showDebugs && currentThreat != null) Debug.Log($"Threat Detected: {currentThreat}");
        }
        else
        {
            if ((currentThreat.position - transform.position).magnitude > escapeRange)
            {
                currentThreat = null;
            }
        }
    }

    private Transform ScanForThreat(Vector3 position, out EntityStatus threatStatus)
    {
        threatStatus = null;
        var range = detectRange;
        if (activeTime == ActiveTime.Day && TimeManager.Main.IsNight || activeTime == ActiveTime.Night && TimeManager.Main.IsDay)
            range = penaltyRange;

        var hits = Physics2D.OverlapCircleAll(position, range, threatLayer);
        foreach (var hit in hits)
        {
            if (hit && hit.TryGetComponent(out threatStatus) && threatStatus.Hostility != entityStatus.Hostility)
            {
                return hit.transform;
            }
        }
        return null;
    }
}
