using System;
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

    [Header("Debugs")]
    [SerializeField]
    private float velocity;

    private Vector2 movementDirection;

    private Rigidbody2D rbody;
    private Vector2 dummy;

    private void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (movementDirection != Vector2.zero)
        {
            var targetDirection = Vector2.Lerp(movementDirection, rbody.velocity.normalized, smoothTime * Time.deltaTime);
            rbody.AddForce(targetDirection * moveSpeed * Time.deltaTime);
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
        movementDirection = newMovementDirection.normalized;
    }

    public void Knockback(float knockbackForce, Transform source)
    {
        rbody.AddForce(knockbackForce * (transform.position - source.position).normalized, ForceMode2D.Impulse);
    }
}
