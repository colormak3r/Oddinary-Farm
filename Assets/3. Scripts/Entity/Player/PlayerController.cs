using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour, DefaultInputActions.IPlayerActions
{
    private static Vector3 LEFT_DIRECTION = new Vector3(-1, 1, 1);
    private static Vector3 RIGHT_DIRECTION = new Vector3(1, 1, 1);

    [Header("Settings")]
    [SerializeField]
    private bool spriteFacingRight;

    private Vector2 lookPosition;
    private Vector2 playerPosition_cached = Vector2.one;

    private float nextPrimary;
    private float nextSecondary;

    private EntityMovement movement;
    private PlayerInventory inventory;
    private PlayerInteraction interaction;

    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        movement = GetComponent<EntityMovement>();
        inventory = GetComponent<PlayerInventory>();
        interaction = GetComponent<PlayerInteraction>();
        //playerInventory.OnCurrentItemPropertyChanged.AddListener(HandleCurrentItemPropertyChanged);
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
            transform.localScale = spriteFacingRight ? RIGHT_DIRECTION : LEFT_DIRECTION;
        else
            transform.localScale = spriteFacingRight ? LEFT_DIRECTION : RIGHT_DIRECTION;
    }

    private void Update()
    {
        // Run Client-Side only
        if (!IsOwner) return;

        if (!GameManager.Main.IsInitialized) return;

        if (playerPosition_cached != (Vector2)transform.position)
        {
            playerPosition_cached = transform.position;
            StartCoroutine(WorldGenerator.Main.GenerateTerrainCoroutine(transform.position));
        }
    }

    private void Initialize()
    {
        if (!IsOwner) return;
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Main.IsInitialized);

        // Set control
        InputManager.Main.InputActions.Player.SetCallbacks(this);

        // Set camera
        Camera.main.transform.parent = transform;
        Camera.main.transform.localPosition = Camera.main.transform.position;
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

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //playerInventory.DropItem(playerInventory.CurrentHotbarIndex, lookPosition);
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            interaction.Interact();
        }
    }

    private void HandleCurrentItemPropertyChanged()
    {
        // Todo: Cache item for more efficient memory use
    }

    #region Player Action

    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var currentItem = inventory.CurrentItemValue;
            if (currentItem != null && Time.time > nextPrimary)
            {
                var itemProperty = currentItem.PropertyValue;
                nextPrimary = Time.time + itemProperty.PrimaryCdr;

                if (currentItem.CanPrimaryAction(lookPosition))
                {
                    if (itemProperty.IsConsummable)
                    {
                        if (inventory.ConsumeItemOnClient(inventory.CurrentHotbarIndex))
                            currentItem.OnPrimaryAction(lookPosition);
                        else
                            Debug.Log("Failed to consume item");
                    }
                    else
                    {
                        currentItem.OnPrimaryAction(lookPosition);
                    }
                }
            }
        }
    }

    public void OnSecondary(InputAction.CallbackContext context)
    {
        var currentItem = inventory.CurrentItemValue;
        if (currentItem != null)
        {
            currentItem.OnSecondaryAction(lookPosition);
        }
    }

    public void OnAlternative(InputAction.CallbackContext context)
    {

    }

    #endregion

    #region Hotbar

    public void OnHotbar(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var value = int.Parse(context.control.name);
            ChangeHotbarIndex(value);
        }
    }

    public void OnHotbarScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var value = inventory.CurrentHotbarIndex - (int)Mathf.Sign(context.ReadValue<float>());
            ChangeHotbarIndex(value);
        }
    }

    private void ChangeHotbarIndex(int value)
    {
        // Clamp and loop the hotbar item keys from 0-9
        if (value > 9)
            value = 0;
        else if (value < 0)
            value = 9;

        // Hotbar index change locally, stored in PlayerInventory
        inventory.ChangeHotBarIndex(value);
    }

    #endregion

    #region Tools

    #endregion
}
