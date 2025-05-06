using UnityEngine;

public class MoveTowardStimulus : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool isGuardMode;
    public bool IsGuardMode => isGuardMode;
    [SerializeField]
    private Vector2 targetPosition;
    public Vector2 TargetPosition => targetPosition;
    [SerializeField]
    private float guardRadius = 5f;
    public float GuardRadius => guardRadius;
    [SerializeField]
    private float reachRadius = 5f;
    public float ReachRadius => reachRadius;

    [Header("Debugs")]
    [SerializeField]
    private bool reachedTarget;
    public bool ReachedTarget => reachedTarget;

    private void Update()
    {
        if (!isGuardMode && !reachedTarget)
        {
            if (((Vector2)transform.position - targetPosition).SqrMagnitude() < reachRadius * reachRadius)
            {
                reachedTarget = true;
            }
        }
    }

    public void SetTargetPositionOnServer(Vector2 newTargetPosition)
    {
        reachedTarget = false;
        targetPosition = newTargetPosition;
    }

    public Vector2 GetRandomGuardLocation()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector2 randomPosition = targetPosition + randomDirection * guardRadius;
        return randomPosition;
    }

    public void SetGuardMode(bool guardMode)
    {
        isGuardMode = guardMode;
        if (isGuardMode)
        {
            targetPosition = GetRandomGuardLocation();
            reachedTarget = false;
        }
    }
}
