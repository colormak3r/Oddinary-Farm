using UnityEngine;
using ColorMak3r.Utility;
using Unity.Netcode;
using Unity.Netcode.Components;

public class FollowStimulus : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float aheadDistance = 2f;
    [SerializeField]
    private float minOffsetTime = 3f;
    [SerializeField]
    private float maxOffsetTime = 5f;
    [SerializeField]
    private bool canTeleportToTarget = true;
    [SerializeField]
    private float maxDistanceFromTarget = 20f;

    [Header("Debugs")]
    [SerializeField]
    private Vector2 aheadPosition;
    public Vector2 AheadPosition => aheadPosition;
    [SerializeField]
    private Vector2 randomAheadOffset;
    [SerializeField]
    private Rigidbody2D targetRbody;
    public Rigidbody2D TargetRBody => targetRbody;
    public Transform Owner => targetRbody.transform;
    private NetworkTransform networkTransform;
    private TargetDetector targetDetector;

    private float nextOffset;
    private float nextTeleportCheck;

    private void Awake()
    {
        networkTransform = GetComponent<NetworkTransform>();
        targetDetector = GetComponent<TargetDetector>();
    }

    private void Update()
    {
        if (targetRbody == null) return;

        var targetPosition = targetRbody.position;
        var targetVelocity = targetRbody.linearVelocity.normalized;

        if (Time.time > nextOffset)
        {
            randomAheadOffset = targetVelocity == Vector2.zero ? MiscUtility.RandomPointInRange(aheadDistance / 2f, aheadDistance) : MiscUtility.RandomPointInRange(0.5f, aheadDistance / 2f);
            nextOffset = Time.time + (targetVelocity == Vector2.zero ? 10f * Random.Range(minOffsetTime, maxOffsetTime) : Random.Range(minOffsetTime, maxOffsetTime));
        }

        aheadPosition = targetPosition + targetVelocity * aheadDistance + randomAheadOffset;

        if (canTeleportToTarget && IsServer && Time.time > nextTeleportCheck)
        {
            nextTeleportCheck = Time.time + 5f;
            if (Vector3.Distance(targetPosition, transform.position) > maxDistanceFromTarget)
            {
                networkTransform.Teleport(aheadPosition, Quaternion.identity, Vector3.one);
                if (targetDetector != null) targetDetector.DeselectTarget($"Teleported to {targetRbody.name}");
            }
        }
    }

    public bool IsNotAtAheadPosition(Vector2 petPosition)
    {
        return (petPosition - aheadPosition).sqrMagnitude > 0.04f;
    }

    public void SetTargetRbody(Rigidbody2D rigidbody2D)
    {
        targetRbody = rigidbody2D;
    }
}
