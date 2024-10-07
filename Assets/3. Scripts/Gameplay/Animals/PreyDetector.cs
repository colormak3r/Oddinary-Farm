using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PreyDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float range = 5f;
    [SerializeField]
    private LayerMask preyMask;

    [HideInInspector]
    public UnityEvent<Transform> OnPreyDetected;
    [HideInInspector]
    public UnityEvent<Transform> OnPreyExited;

    private Transform currentPrey;

    public Transform CurrentPrey => currentPrey;

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        TrackPrey();
    }

    /// <summary>
    /// Scans for prey and detects new entries and exits.
    /// </summary>
    private void TrackPrey()
    {
        if(currentPrey == null)
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
            if(Vector3.Distance(currentPrey.position, transform.position) > range)
            {
                currentPrey = null;
                OnPreyExited?.Invoke(currentPrey);
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, range, preyMask);
        Transform[] result = new Transform[hits.Length];
        for (int i = 0; i < hits.Length; i++)
        {
            result[i] = hits[i].transform;
        }

        return result;
    }
}
