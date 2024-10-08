using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float moveSpeed = 5f;
    [SerializeField]
    private float maxSpeed = 10f;
    [SerializeField]
    private float smoothTime = 0.1f;
    [SerializeField]
    private float arrivalThreshold = 0.1f; // Distance threshold to consider as "arrived"
    [SerializeField]
    private float knockback = 100f;
    [SerializeField]
    private float knockbackCdr = 3f;

    [Header("Debugs")]
    [SerializeField]
    private float velocity;

    private Vector2 movementDirection;
    private Vector2 movementDirection_cached;

    private Rigidbody2D rbody;

    // New Fields
    private Vector2? targetPosition = null; // Nullable to indicate no target
    private bool isMovingToTarget = false;

    // Callback to be invoked upon arrival
    private Action onArrivalCallback = null;

    private void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
    }

    float nextKnockback;
    private void Update()
    {
        if (Time.time > nextKnockback)
        {
            nextKnockback = Time.time + knockbackCdr;
            rbody.AddForce(knockback * UnityEngine.Random.insideUnitCircle.normalized, ForceMode2D.Impulse);
        }
    }

    private void FixedUpdate()
    {
        // If moving towards a target, check if arrived
        if (isMovingToTarget && targetPosition.HasValue)
        {
            Vector2 currentPosition = rbody.position;
            float distance = Vector2.Distance(currentPosition, targetPosition.Value);

            if (distance <= arrivalThreshold)
            {
                // Arrived at target
                StopMovement();
            }
        }

        if (movementDirection != Vector2.zero)
        {
            rbody.AddForce(movementDirection * moveSpeed * Time.deltaTime);
        }

        velocity = rbody.velocity.magnitude;

        // Clamp the velocity to the maximum speed
        if (rbody.velocity.magnitude > maxSpeed)
        {
            rbody.velocity = rbody.velocity.normalized * maxSpeed;
        }
    }

    /// <summary>
    /// Sets the movement direction of the entity.
    /// </summary>
    /// <param name="newMovementDirection">The new direction vector.</param>
    public void SetDirection(Vector2 newMovementDirection)
    {
        if (newMovementDirection != Vector2.zero && movementDirection == Vector2.zero)
        {
            // Starting to move, cache the previous direction
            movementDirection_cached = movementDirection;
        }

        movementDirection = newMovementDirection.normalized;

        // If movement direction is zero and not moving to a target, restore cached direction
        if (movementDirection == Vector2.zero && !isMovingToTarget)
        {
            movementDirection = movementDirection_cached;
        }
    }

    /// <summary>
    /// Moves the entity towards the specified position.
    /// The entity will stop upon reaching the position and invoke the provided callback.
    /// </summary>
    /// <param name="position">The target position to move towards.</param>
    /// <param name="onArrival">An optional callback to be invoked when the target is reached.</param>
    public void MoveTo(Vector2 position, Action onArrival = null)
    {
        targetPosition = position;
        isMovingToTarget = true;
        onArrivalCallback = onArrival;

        // Calculate the direction vector from current position to target position
        Vector2 currentPosition = rbody.position;
        Vector2 direction = (position - currentPosition).normalized;

        // Set the movement direction using the existing SetDirection method
        SetDirection(direction);
    }

    /// <summary>
    /// Stops the entity's movement and invokes the arrival callback if set.
    /// </summary>
    public void StopMovement()
    {
        SetDirection(Vector2.zero);
        isMovingToTarget = false;
        targetPosition = null;

        // Invoke the callback if it exists
        if (onArrivalCallback != null)
        {
            onArrivalCallback.Invoke();
            onArrivalCallback = null; // Clear the callback after invoking
        }
    }
}
