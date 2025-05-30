using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour, IControllable
{
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

    private IInteractable currentInteractable;
    private DistanceComparer distanceComparer;
    private Vector3 interactablePosition_cached;

    private bool isControllable = true;

    private void Start()
    {
        distanceComparer = new DistanceComparer(transform, interactionOffset);
    }

    private void FixedUpdate()
    {
        if (IsOwner) ScanClosetInteractable();

        if (!isControllable) return;

        // Run on the server only
        // ScanItemPickupOnServer();
    }

    public void Interact()
    {
        currentInteractable?.Interact(transform);
    }

    [SerializeField]
    private Collider2D[] cachedcolliders;

    private void ScanClosetInteractable()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position + interactionOffset, interactionRadius, interactableLayer);
        cachedcolliders = hits;

        if (hits.Length > 0)
        {
            Array.Sort(hits, distanceComparer);
            Array.Sort(cachedcolliders, distanceComparer);

            //colliders.OrderBy((collider) => (collider.transform.position - transform.position).sqrMagnitude).ToArray();
            //cachedcolliders = colliders;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].TryGetComponent(out IInteractable closetInteractable))
                {
                    if (closetInteractable != currentInteractable ||
                        (closetInteractable == currentInteractable && hits[i].transform.position != interactablePosition_cached))
                    {
                        currentInteractable = closetInteractable;
                        interactablePosition_cached = hits[i].transform.position;
                        Selector.Main.Select(hits[i].gameObject);
                    }
                    break;
                }
            }
        }
        else
        {
            if (currentInteractable != null)
            {
                currentInteractable = null;
            }

            Selector.Main.Show(false);
        }
    }

    public void SetControllable(bool value)
    {
        isControllable = value;

        if (!isControllable && currentInteractable != null)
        {
            currentInteractable = null;
            Selector.Main.Show(false);
        }
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.blue;
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
