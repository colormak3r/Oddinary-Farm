using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class EntityMovement : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float moveSpeed = 5f;
    [SerializeField]
    private float maxSpeed = 10f;
    [SerializeField]
    private float smoothTime = 0.1f;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
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
            rbody.AddForce(targetDirection * moveSpeed * Time.deltaTime);
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

    public void SetCanBeKnockback(bool value)
    {
        SetCanBeKnockBackRpc(value);
    }

    [Rpc(SendTo.Server)]
    private void SetCanBeKnockBackRpc(bool value)
    {
        CanBeKnockback.Value = value;
    }

    public void Knockback(float knockbackForce, Transform source)
    {
        if (!CanBeKnockback.Value)
        {
            if (showDebugs) Debug.Log(gameObject.name + " Can't be knockbacked");
            return;
        }

        // TODO: client authoritative, server side prediction
        if (!clientNetworkTransform)
        {
            // Server authoritative movement
            if (showDebugs) Debug.Log(gameObject.name + " KnockbackServerRpc");
            KnockbackServerRpc(knockbackForce, source.position);
        }
        else
        {
            // Client authoritative movement
            if (showDebugs) Debug.Log(gameObject.name + " KnockbackClientRpc");
            KnockbackClientRpc(knockbackForce, source.position);
        }
    }

    [Rpc(SendTo.Server)]
    private void KnockbackServerRpc(float knockbackForce, Vector2 sourcePosition)
    {
        if (showDebugs) Debug.Log(gameObject.name + " KnockbackServerRpc recieved");
        KnockbackInternal(knockbackForce, sourcePosition);
    }

    [Rpc(SendTo.Owner)]
    private void KnockbackClientRpc(float knockbackForce, Vector2 sourcePosition)
    {
        if (showDebugs) Debug.Log(gameObject.name + " KnockbackClientRpc recieved");
        KnockbackInternal(knockbackForce, sourcePosition);
    }

    private void KnockbackInternal(float knockbackForce, Vector2 sourcePosition)
    {
        // Calculate the knockback direction as a normalized 2D vector
        Vector2 knockbackDirection = ((Vector2)transform.position - sourcePosition).normalized;

        // Apply the force to the Rigidbody2D
        rbody.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
    }
}
