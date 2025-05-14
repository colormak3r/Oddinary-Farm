using UnityEngine;
using ColorMak3r.Utility;

public class FollowStimulus : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Rigidbody2D targetRbody;
    [SerializeField] private float aheadDistance = 2f;
    [SerializeField] private float minOffsetTime = 3f;
    [SerializeField] private float maxOffsetTime = 5f;
    [SerializeField]
    private Vector2 aheadPosition;
    public Vector2 AheadPosition => aheadPosition;
    [SerializeField]
    private Vector2 randomAheadOffset;
    private float nextOffset;

    public Rigidbody2D TargetRBody => targetRbody;

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
