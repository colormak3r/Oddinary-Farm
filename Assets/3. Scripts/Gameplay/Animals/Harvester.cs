using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Harvester : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float radius = 0.5f;
    [SerializeField]
    private LayerMask plantLayer;

    private void Update()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, radius);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IHarvestable>(out var harvestable))
                {
                    harvestable.GetHarvested();
                }
            }
        }
    }
}
