using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float speed = 300f;
    [SerializeField]
    private float smoothTime = 0.01f;
    [SerializeField]
    private float arrivalThreshold = 0.1f; // Distance threshold to consider as "arrived"

    [Header("Debugs")]
    private Vector2 movementDirection;
    private Vector2 movementDirection_cached;
    private Vector2 velocity;

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

        // Apply movement
        var targetVelocity = speed * movementDirection * Time.deltaTime;
        rbody.velocity = Vector2.SmoothDamp(rbody.velocity, targetVelocity, ref velocity, smoothTime);
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

        movementDirection = newMovementDirection;

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
