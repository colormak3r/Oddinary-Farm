using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[System.Flags]
public enum PreyFilter : byte
{
    None = 0,
    Plant = 1,
    Animal = 2,
    Player = 4,
    Any = Plant | Animal | Player,
}

[System.Flags]
public enum PlantFilter : byte
{
    None = 0,
    Harvestable = 1,
    NotHarvestable = 2,
    Any = Harvestable | NotHarvestable
}


public class PreyDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float range = 5f;
    [SerializeField]
    private LayerMask preyMask;
    [SerializeField]
    private PreyFilter preyFilter;
    [SerializeField]
    private PlantFilter plantFilter;

    [Header("Debugs")]
    [SerializeField]
    private Transform currentPrey;
    [SerializeField]
    private bool showDebug = false;

    [HideInInspector]
    public UnityEvent<Transform> OnPreyDetected;
    [HideInInspector]
    public UnityEvent<Transform> OnPreyExited;

    public Transform CurrentPrey => currentPrey;

    private bool preyDetected;

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
        if (currentPrey == null)
        {
            var preys = ScanForPrey(transform.position);
            if (preys.Length > 0)
            {
                currentPrey = preys[0];
                preyDetected = true;
                if(showDebug) Debug.Log($"{currentPrey.gameObject.name} detected");
                OnPreyDetected?.Invoke(currentPrey);
            }
            else
            {
                if (preyDetected)
                {
                    preyDetected = false;
                    if (showDebug) Debug.Log("Prey exited");
                    OnPreyExited?.Invoke(null);
                }
            }
        }
        else
        {
            if (Vector3.Distance(currentPrey.position, transform.position) > range)
            {
                currentPrey = null;
                preyDetected = false;
                if (showDebug) Debug.Log("Prey exited");
                OnPreyExited?.Invoke(null); // Pass null or the specific prey if needed
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
        List<Transform> result = new List<Transform>();
        result.Capacity = hits.Length;

        for (int i = 0; i < hits.Length; i++)
        {
            if (preyFilter.HasFlag(PreyFilter.Plant))
            {
                if (hits[i].TryGetComponent<IHarvestable>(out var harvestable))
                {
                    if (plantFilter.HasFlag(PlantFilter.Harvestable))
                    {
                        if (harvestable.IsHarvestable()) result.Add(hits[i].transform);
                    }
                    else if (plantFilter.HasFlag(PlantFilter.NotHarvestable))
                    {
                        if (!harvestable.IsHarvestable()) result.Add(hits[i].transform);
                    }
                }
            }

            if (preyFilter.HasFlag(PreyFilter.Animal))
            {
                if (hits[i].TryGetComponent<Animal>(out var animal))
                {
                    result.Add(hits[i].transform);
                }
            }

            if (preyFilter.HasFlag(PreyFilter.Player))
            {
                if (hits[i].TryGetComponent<PlayerStatus>(out var player))
                {
                    result.Add(hits[i].transform);
                }
            }
        }

        return result.ToArray();
    }
}
