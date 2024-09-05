using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float pickupRadius = 4f;
    [SerializeField]
    private LayerMask itemLayer;

    private void Update()
    {
        if (!IsServer) return;

        var hits = Physics2D.OverlapCircleAll(transform.PositionHalfUp(), pickupRadius, itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out Item item))
                {
                    if (!item.CanBePickedUpValue) continue;

                    item.PickUpItem(transform);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.PositionHalfUp(), pickupRadius);
    }
}
