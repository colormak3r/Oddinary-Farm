/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   06/27/2025 (Ryan)
 * Notes:           Handles all mount actions including
 *                  Player movement input
*/
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MountInteraction))]
public abstract class MountController : NetworkBehaviour
{
    [SerializeField] protected float speedMultiplier = 1f;
    [SerializeField] protected bool debug = false;

    protected EntityMovement movement { get; private set; }
    protected MountInteraction mountInteraction { get; private set; }

    public bool CanMove { get; set; } = true;

    // TODO: Network variable boolean 'IsBeingControlled'

    public override void OnNetworkSpawn()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        movement = GetComponent<EntityMovement>();
        mountInteraction = GetComponent<MountInteraction>();

        movement.SetDirection(Vector2.zero);
        movement.SetSpeedMultiplier(speedMultiplier);

        mountInteraction.OnMount += HandleOnMount;
        mountInteraction.OnDismount += HandleOnDismount;
    }

    protected virtual void HandleOnMount(Transform source)
    {
        if (source.TryGetComponent<PlayerController>(out var pc))
        {
            pc.SetIsMounting(true, this);
            Debug.Log("Player Controller attached.");
        }
        else
        {
            Debug.LogError("Mount Controller Error: Cannot find player controller for mount.");
        }
    }

    protected virtual void HandleOnDismount(Transform source)
    {
        if (source.TryGetComponent<PlayerController>(out var pc))
        {
            pc.SetIsMounting(false, null);
            Debug.Log("Player Controller released.");
        }
        else
        {
            Debug.LogError("Mount Controller Error: Cannot find player controller for mount.");
        }
    }

    // Must include a Move Method
    public abstract void Move(Vector2 direction);
}

