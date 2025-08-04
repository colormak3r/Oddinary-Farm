/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   08/03/2025 (Khoa)
 * Notes:           Handles all mount actions including
 *                  Player movement input
*/

using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MountInteraction))]
public abstract class MountController : NetworkBehaviour
{
    private static Vector3 LEFT_DIRECTION = new Vector3(-1, 1, 1);
    private static Vector3 RIGHT_DIRECTION = new Vector3(1, 1, 1);

    [Header("Mount Controller Settings")]
    [SerializeField]
    private bool spriteFacingRight;
    [SerializeField]
    private bool faceMovementDirection;
    [SerializeField]
    private Transform graphicTransform;
    [SerializeField]
    private Transform mountingPoint;
    public Transform MountingPoint => mountingPoint;
    [SerializeField]
    private Transform cameraPoint;
    public Transform CameraPoint => cameraPoint;
    [SerializeField]
    protected float speedMultiplier = 1f;

    [Header("Mount Controller Debugs")]
    [SerializeField]
    protected bool showDebugs = false;

    // Because mount can have specialized movement behaviour when not mounting, 
    // we need to keep track of whether the mount is being controlled by the player
    protected NetworkVariable<bool> IsBeingControlled = new NetworkVariable<bool>(false);

    protected EntityMovement mountMovement { get; private set; }
    protected Animator mountAnimator { get; private set; }

    private void Awake()
    {
        // These can be set without network ownership
        mountMovement = GetComponent<EntityMovement>();
        mountMovement.SetDirection(Vector2.zero);
        mountMovement.SetSpeedMultiplier(speedMultiplier);

        mountAnimator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        IsBeingControlled.OnValueChanged += HandleIsBeingControlledChanged;
        HandleIsBeingControlledChanged(false, IsBeingControlled.Value); // Initialize state
    }

    public override void OnNetworkDespawn()
    {
        IsBeingControlled.OnValueChanged -= HandleIsBeingControlledChanged;
    }

    protected virtual void HandleIsBeingControlledChanged(bool previousValue, bool newValue)
    {
        if (IsOwner)
        {
            if (mountAnimator) mountAnimator.SetBool("IsMoving", false);
            if (mountMovement) mountMovement.SetDirection(Vector2.zero);
        }
    }

    // Always hide an RPC behind a public method to add sanity check before calling RPCs, or add thortling in the future.
    public void SetIsBeingControlled(bool isBeingControlled)
    {
        SetIsBeingControlledRpc(isBeingControlled);
    }

    [Rpc(SendTo.Server)]
    protected void SetIsBeingControlledRpc(bool isBeingControlled)
    {
        IsBeingControlled.Value = isBeingControlled;
    }

    protected virtual void Update()
    {
        UpdateFacing();
    }

    private float x_cached;
    protected void UpdateFacing()
    {
        if (faceMovementDirection && mountMovement && graphicTransform)
        {
            if ((transform.position.x - x_cached) > 0.001f)
            {
                // If the mount is moving right, we want to face right
                graphicTransform.localScale = spriteFacingRight ? RIGHT_DIRECTION : LEFT_DIRECTION;
            }
            else if ((transform.position.x - x_cached) < -0.001f)
            {
                // If the mount is moving left, we want to face left
                graphicTransform.localScale = spriteFacingRight ? LEFT_DIRECTION : RIGHT_DIRECTION;
            }
            x_cached = transform.position.x;    // Cache the x position for next frame comparison
        }
    }

    // Must include a Move Method
    public abstract void Move(Vector2 direction);
}

