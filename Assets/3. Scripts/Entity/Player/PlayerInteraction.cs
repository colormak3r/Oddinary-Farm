using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour, IControllable
{
    [Header("Pickup Settings")]
    [SerializeField]
    private float pickupRadius = 6f;
    [SerializeField]
    private Vector3 pickupOffset = new Vector3(0, 0.75f);
    [SerializeField]
    private LayerMask itemLayer;

    [Header("Interaction Settings")]
    [SerializeField]
    private float interactionRadius = 2f;
    [SerializeField]
    private Vector3 interactionOffset = new Vector3(0, 0.75f);
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
        distanceComparer = new DistanceComparer(transform, interactionOffset);
    }

    private void Update()
    {
        if (IsOwner) ScanClosetInteractable();

        if (!isControllable) return;

        // Run on the server only
        if (!IsServer) return;

        var hits = Physics2D.OverlapCircleAll(transform.position + pickupOffset, pickupRadius, itemLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out ItemReplica itemReplica))
                {
                    if (!itemReplica.CanBePickupValue) continue;

                    itemReplica.Pickup(transform);
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
        var hits = Physics2D.OverlapCircleAll(transform.position + interactionOffset, interactionRadius, interactableLayer);

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

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.blue;
        var pickupPos = transform.position + pickupOffset;
        Gizmos.DrawWireSphere(pickupPos, pickupRadius);
        Handles.Label(pickupPos.Add(pickupRadius), "Pickup\nRadius");

        var interactionPos = transform.position + interactionOffset;
        Gizmos.DrawWireSphere(interactionPos, interactionRadius);
        Handles.Label(interactionPos.Add(interactionRadius), "Interaction\nRadius");
    }

#endif

}

public class DistanceComparer : IComparer<Collider2D>
{
    private Transform target;
    private Vector3 offset;

    public DistanceComparer(Transform target, Vector3 offset)
    {
        this.target = target;
        this.offset = offset;
    }

    public int Compare(Collider2D a, Collider2D b)
    {
        var targetPosition = target.position + offset;
        return Vector3.SqrMagnitude(a.transform.position - targetPosition).CompareTo(Vector3.SqrMagnitude(b.transform.position - targetPosition));
    }
}
