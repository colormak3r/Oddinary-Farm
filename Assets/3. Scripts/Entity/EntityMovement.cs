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

    [Header("Debugs")]
    private Vector2 movementDirection;
    private Vector2 movementDirection_cached;
    private Vector2 velocity;

    private Rigidbody2D rbody;

    private void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        var targetVelocity = speed * movementDirection * Time.deltaTime;
        rbody.velocity = Vector2.SmoothDamp(rbody.velocity, targetVelocity, ref velocity, smoothTime);
    }

    public void SetDirection(Vector2 newMovementDirection)
    {
        if (movementDirection != Vector2.zero)
            movementDirection_cached = movementDirection;

        movementDirection = newMovementDirection;
    }
}
