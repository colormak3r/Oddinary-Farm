using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestAction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float radius = 0.5f;
    [SerializeField]
    private LayerMask plantLayer;

    public void Execute()
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
