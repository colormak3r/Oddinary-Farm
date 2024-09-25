using ColorMak3r.Utility;
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
        // Run on the server only
        if(!IsServer) return;

        var hits = Physics2D.OverlapCircleAll(transform.PositionHalfUp(), pickupRadius, itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out ItemReplica itemReplica))
                {
                    if (!itemReplica.CanBePickedUpValue) continue;

                    itemReplica.PickUpItem(transform, NetworkObject);
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
