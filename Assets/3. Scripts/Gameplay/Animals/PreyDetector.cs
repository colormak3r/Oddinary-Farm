using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PreyDetector : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float detectRange = 5f;
    [SerializeField]
    private float escapeRange = 7f;
    [SerializeField]
    private LayerMask preyMask;

    [Header("Debugs")]
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private Transform currentPrey;

    [HideInInspector]
    public UnityEvent<Transform> OnPreyDetected;
    [HideInInspector]
    public UnityEvent<Transform> OnPreyExited;

    public Transform CurrentPrey => currentPrey;
    public float DistanceToPrey => currentPrey ? (transform.position - currentPrey.position).magnitude : float.PositiveInfinity;

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
            var preys = ScanForPrey(transform.position);
            if (preys.Length <= 0) return;

            currentPrey = ScanForPrey(transform.position)[0];
            if (currentPrey != null)
            {
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
    private Transform[] ScanForPrey(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, detectRange, preyMask);
        Transform[] result = new Transform[hits.Length];
        for (int i = 0; i < hits.Length; i++)
        {
            result[i] = hits[i].transform;
        }

        return result;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, escapeRange);
    }
}