using Unity.Netcode;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour, DefaultInputActions.IPlayerActions
{
    private static Vector3 LEFT_DIRECTION = new Vector3(-1, 1, 1);

    [SerializeField]
    private Vector2 lookPosition;
    private Vector2 playerPosition_cached = Vector2.one;

    private EntityMovement movement;

    [SerializeField]
    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>();

    private void Awake()
    {
        movement = GetComponent<EntityMovement>();        
    }

    private void Start()
    {
        if (!IsOwner) return;

        InputManager.Main.InputActions.Player.SetCallbacks(this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsFacingRight.OnValueChanged += HandleOnIsFacingRightChanged;

        Initialize();
        HandleOnIsFacingRightChanged(false, IsFacingRight.Value);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        IsFacingRight.OnValueChanged -= HandleOnIsFacingRightChanged;
    }

    private void HandleOnIsFacingRightChanged(bool previous, bool current)
    {
        var isFacingRight = current;
        if (isFacingRight)
            transform.localScale = Vector3.one;
        else
            transform.localScale = LEFT_DIRECTION;
    }

    private void Initialize()
    {
        if (!IsOwner) return;

        // Set control
        InputManager.Main.InputActions.Player.SetCallbacks(this);

        // Set camera
        Camera.main.transform.parent = transform;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        var direction = context.ReadValue<Vector2>().normalized;
        movement.SetDirection(direction);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookPosition = Camera.main.ScreenToWorldPoint(context.ReadValue<Vector2>());
        IsFacingRight.Value = (lookPosition - (Vector2)transform.position).x > 0;
    }
}
