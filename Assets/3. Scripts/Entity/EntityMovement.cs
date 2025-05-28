using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class EntityMovement : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float moveSpeed = 2000f;
    [SerializeField]
    private float maxSpeed = 10f;
    [SerializeField]
    private float smoothTime = 0.1f;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private float speedMultiplier = 1f;
    [SerializeField]
    private float velocity;

    private NetworkVariable<bool> CanBeKnockback = new NetworkVariable<bool>(true, default, NetworkVariableWritePermission.Server);
    private Vector2 movementDirection;

    private Rigidbody2D rbody;
    private Vector2 dummy;
    private ClientNetworkTransform clientNetworkTransform;

    private void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        if (showDebugs) Debug.Log(gameObject.name + " ClientNetworkTransform: " + clientNetworkTransform);
    }

    private void FixedUpdate()
    {
        if (movementDirection != Vector2.zero)
        {
            var targetDirection = Vector2.Lerp(movementDirection, rbody.linearVelocity.normalized, smoothTime * Time.deltaTime);
            rbody.AddForce(targetDirection * moveSpeed * speedMultiplier * Time.deltaTime);
        }

        velocity = rbody.linearVelocity.magnitude;

        // Clamp the velocity to the maximum speed
        if (rbody.linearVelocity.magnitude > maxSpeed)
        {
            rbody.linearVelocity = rbody.linearVelocity.normalized * maxSpeed;
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

    #region SetSpeedMultiplier
    public void SetSpeedMultiplier(float speedMultiplier)
    {
        SetSpeedMultiplierRpc(speedMultiplier);
    }

    [Rpc(SendTo.Owner)]
    private void SetSpeedMultiplierRpc(float speedMultiplier)
    {
        this.speedMultiplier = speedMultiplier;
    }
    #endregion

    #region Knockback

    public void SetCanBeKnockback(bool value)
    {
        SetCanBeKnockBackRpc(value);
    }

    [Rpc(SendTo.Server)]
    private void SetCanBeKnockBackRpc(bool value)
    {
        CanBeKnockback.Value = value;
    }

    public void Knockback(float force, Vector2 sourcePosition)
    {
        if (!CanBeKnockback.Value)
        {
            if (showDebugs) Debug.Log(gameObject.name + " Can't be knockbacked");
            return;
        }

        KnockbackClientRpc(force, ((Vector2)transform.position - sourcePosition).normalized);
    }

    public void KnockbackDirection(float force, Vector2 direction)
    {
        if (!CanBeKnockback.Value)
        {
            if (showDebugs) Debug.Log(gameObject.name + " Can't be knockbacked");
            return;
        }

        KnockbackClientRpc(force, direction);
    }

    [Rpc(SendTo.Owner)]
    private void KnockbackClientRpc(float force, Vector2 direction)
    {
        if (showDebugs) Debug.Log(gameObject.name + " KnockbackClientRpc recieved");
        KnockbackInternal(force, direction);
    }

    private void KnockbackInternal(float force, Vector2 direction)
    {
        rbody.AddForce(direction * force, ForceMode2D.Impulse);
    }

    #endregion
}
