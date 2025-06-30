/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   06/27/2025 (Ryan)
 * Notes:           Handles all mount actions including
 *                  Player movement input
*/
using Unity.IO.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(MountInteraction))]
public abstract class MountController : NetworkBehaviour, DefaultInputActions.IGameplayActions, IControllable, IMoveable
{
    [SerializeField] protected bool debug = false;

    private EntityMovement movement;
    private MountInteraction mountInteraction;
    private bool isControllable = false;
    private bool isMoveable = false;

    public override void OnNetworkSpawn()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        movement = GetComponent<EntityMovement>();
        mountInteraction = GetComponent<MountInteraction>();

        mountInteraction.OnMount += HandleOnMount;
        mountInteraction.OnDismount += HandleOnDismount;
    }

    protected virtual void HandleOnMount(Transform source)
    {
        SetControllable(true);
    }

    protected virtual void HandleOnDismount(Transform source)
    {
        SetControllable(false);
    }

    public void SetControllable(bool value)
    {
        isControllable = value;
        Debug.Log($"Controllable = {value}");
    }

    public void SetMoveable(bool moveable)
    {
        isMoveable = moveable;
        Debug.Log($"Moveable = {moveable}");
    }

    #region Input Actions
    public virtual void OnAlternative(InputAction.CallbackContext context) { }

    public virtual void OnDrop(InputAction.CallbackContext context) { }

    public virtual void OnHand(InputAction.CallbackContext context) { }

    public virtual void OnHotbar(InputAction.CallbackContext context) { }

    public virtual void OnHotbarDown(InputAction.CallbackContext context) { }

    public virtual void OnHotbarScroll(InputAction.CallbackContext context) { }

    public virtual void OnHotbarUp(InputAction.CallbackContext context) { }

    public virtual void OnInteract(InputAction.CallbackContext context) { }

    public virtual void OnInventory(InputAction.CallbackContext context) { }

    public virtual void OnLookDirection(InputAction.CallbackContext context) { }

    public virtual void OnLookPosition(InputAction.CallbackContext context) { }

    public virtual void OnMap(InputAction.CallbackContext context) { }

    public virtual void OnMove(InputAction.CallbackContext context)
    {
        if (isControllable && isMoveable)
        {
            var direction = context.ReadValue<Vector2>().normalized;
            movement.SetDirection(direction);
        }
    }

    public virtual void OnOpenConsole(InputAction.CallbackContext context) { }

    public virtual void OnPause(InputAction.CallbackContext context) { }

    public virtual void OnPrimary(InputAction.CallbackContext context) { }

    public virtual void OnSecondary(InputAction.CallbackContext context) { }

    public virtual void OnToggleUI(InputAction.CallbackContext context) { }
    #endregion

    // Must include a Move Method
    protected abstract void Move(Vector2 motion, float deltaTime);
}

