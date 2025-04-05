using UnityEngine;

public class FollowStimulus : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Rigidbody2D targetRbody;
    [SerializeField] private float aheadDistance = 2f;
    [SerializeField] private float followDistance = 5f;
    [SerializeField] private float minPredictionTime = 0.1f;
    [SerializeField] private float maxPredictionTime = 0.7f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float smoothingFactor = 0.2f;

    private Vector2 smoothedAheadPosition;

    public Rigidbody2D TargetRBody => targetRbody;

    private void Awake()
    {
        if (targetRbody != null)
            smoothedAheadPosition = targetRbody.position;
    }

    public Vector2 GetAheadPosition(Vector2 petPosition)
    {
        if (targetRbody == null)
            return petPosition;

        Vector2 playerPos = targetRbody.position;
        Vector2 playerVelocity = targetRbody.linearVelocity;

        float distance = Vector2.Distance(petPosition, playerPos);
        float dynamicPredictionTime = Mathf.Lerp(minPredictionTime, maxPredictionTime, distance / maxDistance);

        Vector2 predictedPosition;

        if (playerVelocity.magnitude > 0.1f)
        {
            // Position ahead of player in the direction of velocity
            predictedPosition = playerPos + playerVelocity.normalized * aheadDistance;
        }
        else
        {
            // Player stopped; just maintain a default offset position
            predictedPosition = playerPos + (petPosition - playerPos).normalized * aheadDistance;
        }

        // Smooth prediction
        smoothedAheadPosition = Vector2.Lerp(smoothedAheadPosition, predictedPosition, smoothingFactor);
        return smoothedAheadPosition;
    }

    public bool IsOutsideFollowDistance(Vector2 petPosition)
    {
        return Vector2.Distance(petPosition, targetRbody.position) > followDistance;
    }
}
