using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ObjectTargetRotation : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Transform objectToRotate;
    [SerializeField]
    private float rotationLerpSpeed = 10f; // higher = snappier

    private Rigidbody2D rb;
    private TargetDetector targetDetector;

    private float nextRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        targetDetector = GetComponent<TargetDetector>();
    }

    Vector2? direction = null;
    private void Update()
    {
        if (!IsServer) return;

        if (Time.time > nextRotation)
        {
            nextRotation = Time.time + 0.1f;

            if (targetDetector.CurrentTarget != null)
            {
                direction = targetDetector.CurrentTarget.position - transform.position;
            }
            else
            {
                var velocity = rb.linearVelocity;
                if (velocity.sqrMagnitude > 0.0001f)
                {
                    direction = velocity;
                }
            }
        }

        if (direction.HasValue)
        {
            float angle = Mathf.Atan2(direction.Value.y, direction.Value.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            objectToRotate.rotation = Quaternion.Lerp(
                objectToRotate.rotation,
                targetRotation,
                rotationLerpSpeed * Time.deltaTime
            );
        }
    }
}
