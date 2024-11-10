using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour, IControllable
{
    [Header("Pickup Settings")]
    [SerializeField]
    private float pickupRadius = 4f;
    [SerializeField]
    private LayerMask itemLayer;

    [Header("Pickup Settings")]
    [SerializeField]
    private float interactionRadius = 2f;
    [SerializeField]
    private LayerMask interactableLayer;

    [Header("Debugs")]
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private IInteractable currentInteractable;
    private DistanceComparer distanceComparer;

    private bool isControllable = true;

    private void Start()
    {
        distanceComparer = new DistanceComparer(transform);
    }


    private void Update()
    {
        if (IsOwner) ScanClosetInteractable();

        if (!isControllable) return; 

        // Run on the server only
        if (!IsServer) return;

        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, itemLayer);
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

    public void Interact()
    {
        currentInteractable?.Interact(transform);
    }

    private void ScanClosetInteractable()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayer);


        if (hits.Length > 0)
        {
            Array.Sort(hits, distanceComparer);

            //colliders.OrderBy((collider) => (collider.transform.position - transform.position).sqrMagnitude).ToArray();
            //cachedcolliders = colliders;
            if (hits[0].TryGetComponent(out IInteractable closetInteractable))
            {
                if (closetInteractable != currentInteractable)
                {
                    currentInteractable = closetInteractable;
                    Selector.main.Select(hits[0].gameObject);
                }
            }
        }
        else
        {
            if (currentInteractable != null)
            {
                currentInteractable = null;
                Selector.main.Show(false);
            }
        }
    }

    public void SetControllable(bool value)
    {
        isControllable = value;

        if (!isControllable && currentInteractable != null)
        {
            currentInteractable = null;
            Selector.main.Show(false);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}

public class DistanceComparer : IComparer<Collider2D>
{
    private Transform target;

    public DistanceComparer(Transform target)
    {
        this.target = target;
    }

    public int Compare(Collider2D a, Collider2D b)
    {
        var targetPosition = target.position;
        return Vector3.SqrMagnitude(a.transform.position - targetPosition).CompareTo(Vector3.SqrMagnitude(b.transform.position - targetPosition));
    }
}
