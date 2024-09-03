using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float pickupRadius;
    [SerializeField]
    private LayerMask itemLayer;

    private void Update()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position,pickupRadius,itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if(hit.TryGetComponent(out Item item))
                {
                    if (!item.CanBePickedUpValue) continue;

                    item.PickUpItemRpc(this);
                }
            }
        }
    }
}
