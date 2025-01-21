using ColorMak3r.Utility;
using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public enum AnimationMode
{
    Primary,
    Secondary,
    Alternative
}

public class PlayerController : NetworkBehaviour, DefaultInputActions.IGameplayActions, IControllable
{
    private static Vector3 LEFT_DIRECTION = new Vector3(-1, 1, 1);
    private static Vector3 RIGHT_DIRECTION = new Vector3(1, 1, 1);
    private static Vector3 VECTOR_ONE_FLIP_XY = new Vector3(-1, -1, 1);

    [Header("Settings")]
    [SerializeField]
    private Transform graphicTransform;
    [SerializeField]
    private Transform muzzleTransform;
    [SerializeField]
    private bool spriteFacingRight;

    [Header("Graphic Settings")]
    [SerializeField]
    private SpriteRenderer itemRenderer;
    [SerializeField]
    private GameObject arm;
    [SerializeField]
    private SpriteRenderer itemRotationRenderer;
    [SerializeField]
    private GameObject armRotation;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;

    private bool isControllable = true;
    private Vector2 lookPosition;
    private Vector2 playerPosition_cached = Vector2.one;
    private Vector2 mousePosition;

    private float nextPrimary;
    private float nextSecondary;

    private EntityMovement movement;
    private PlayerInventory inventory;
    private PlayerInteraction interaction;

    private Previewer previewer;

    private Animator animator;
    private NetworkAnimator networkAnimator;
    private PlayerAnimationController animationController;

    private bool isOwner;
    private bool isInitialized;

    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    public static Vector2 LookPosition { get; private set; }

    public static Transform MuzzleTransform;

    private bool rotateArm = false;

    private Item currentItem;

    private void Awake()
    {
        movement = GetComponent<EntityMovement>();
        inventory = GetComponent<PlayerInventory>();
        interaction = GetComponent<PlayerInteraction>();
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        animationController = GetComponentInChildren<PlayerAnimationController>();
    }

    private void OnEnable()
    {
        inventory.OnCurrentItemChanged += HandleCurrentItemPropertyChanged;
    }

    private void OnDisable()
    {
        inventory.OnCurrentItemChanged -= HandleCurrentItemPropertyChanged;

        if (isOwner)
        {
            Camera.main.transform.parent = null;
            InputManager.Main.InputActions.Gameplay.SetCallbacks(null);
        }
    }

    private void HandleCurrentItemPropertyChanged(Item item)
    {
        currentItem = item;

        if (isOwner) Preview(lookPosition);

        rotateArm = item is RangedWeapon;
        if (rotateArm)
        {
            armRotation.SetActive(true);
            arm.SetActive(false);
            itemRotationRenderer.sprite = item.PropertyValue.Sprite;
        }
        else
        {
            armRotation.SetActive(false);
            arm.SetActive(true);
            itemRenderer.sprite = item.PropertyValue.Sprite;
        }
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
            graphicTransform.localScale = spriteFacingRight ? RIGHT_DIRECTION : LEFT_DIRECTION;
        else
            graphicTransform.localScale = spriteFacingRight ? LEFT_DIRECTION : RIGHT_DIRECTION;
    }

    private void Update()
    {
        // Run Client-Side only
        if (!IsOwner || !isInitialized) return;

        if (!GameManager.Main.IsInitialized) return;

        if (playerPosition_cached != (Vector2)transform.position)
        {
            playerPosition_cached = transform.position;
            StartCoroutine(WorldGenerator.Main.GenerateTerrainCoroutine(transform.position));
        }

        lookPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        LookPosition = lookPosition;
        IsFacingRight.Value = (lookPosition - (Vector2)transform.position).x > 0;

        if (rotateArm) RotateArm(lookPosition);

        Preview(lookPosition);
    }

    private void Initialize()
    {
        if (!IsOwner) return;
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        // Set camera
        Camera.main.transform.parent = transform;
        Camera.main.transform.localPosition = Camera.main.transform.position;

        yield return new WaitUntil(() => GameManager.Main.IsInitialized);

        // Set control
        InputManager.Main.InputActions.Gameplay.SetCallbacks(this);
        InputManager.Main.SwitchMap(InputMap.Gameplay);

        // Set previewer
        previewer = Previewer.Main;

        // Set muzzle transform
        MuzzleTransform = muzzleTransform;

        isOwner = true;

        isInitialized = true;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        var direction = context.ReadValue<Vector2>().normalized;
        movement.SetDirection(direction);
        animator.SetBool("IsMoving", direction != Vector2.zero);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (Camera.main == null) return;

        mousePosition = context.ReadValue<Vector2>();
    }

    private void RotateArm(Vector2 lookPosition)
    {
        var direction = (lookPosition - (Vector2)armRotation.transform.position).normalized;
        armRotation.transform.rotation = Quaternion.LookRotation(Vector3.forward, Quaternion.Euler(0, 0, 90) * direction);
        armRotation.transform.localScale = IsFacingRight.Value ? Vector3.one : VECTOR_ONE_FLIP_XY;
    }

    private void Preview(Vector2 position)
    {
        if (currentItem != null)
            currentItem.OnPreview(position, previewer);
        else
            previewer.Show(false);
    }

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            inventory.DropItem(inventory.CurrentHotbarIndex);
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            interaction.Interact();
        }
    }

    public void OnMap(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            MapUI.Main.ToggleShow();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        // Pause bypass controllable
        // if (!isControllable) return;

        if (context.performed)
        {
            if (ShopUI.Main.IsShowing) ShopUI.Main.CloseShop();
        }
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            InventoryUI.Main.ToggleInventory();
        }
    }

    public void OnHand(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            ChangeHotbarIndex(0);
        }
    }


    public void OnOpenConsole(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Prevent player drifting when opening console
            // movement.SetDirection(Vector2.zero);
            ConsoleUI.Main.OpenConsole();
        }
    }

    #region Player Action

    private Vector2? primaryPosition;
    private Vector2? secondaryPosition;

    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            if (currentItem != null && Time.time > nextPrimary)
            {
                var itemProperty = currentItem.PropertyValue;
                nextPrimary = Time.time + itemProperty.PrimaryCdr;

                if (currentItem is RangedWeapon)
                {
                    networkAnimator.SetTrigger("Shoot");
                    currentItem.OnPrimaryAction(lookPosition);
                    SyncArmRotationRpc(lookPosition);
                }
                else
                {
                    if (currentItem.CanPrimaryAction(lookPosition))
                    {
                        primaryPosition = lookPosition;
                        animationController.ChopAnimationMode = AnimationMode.Primary;
                        networkAnimator.SetTrigger("Chop");
                    }
                    else
                    {
                        AudioManager.Main.PlaySoundEffect(SoundEffect.UIError);
                    }
                }
            }
        }
    }

    [Rpc(SendTo.NotMe)]
    private void SyncArmRotationRpc(Vector2 lookPosition)
    {
        RotateArm(lookPosition);
    }

    public void Chop(AnimationMode mode)
    {
        if (!IsOwner) return;

        var itemProperty = currentItem.PropertyValue;
        switch (mode)
        {
            case AnimationMode.Primary:
                if (!primaryPosition.HasValue)
                {
                    Debug.LogError("Primary position is null");
                    break;
                }
                if (itemProperty.IsConsummable)
                {
                    if (inventory.CanConsumeItemOnClient(inventory.CurrentHotbarIndex))
                    {
                        currentItem.OnPrimaryAction(primaryPosition.Value);
                        inventory.ConsumeItemOnClient(inventory.CurrentHotbarIndex);
                    }
                    else
                    {
                        Debug.Log("Cannot consume item");
                    }
                }
                else
                {
                    currentItem.OnPrimaryAction(primaryPosition.Value);
                }

                Preview(lookPosition);
                break;
            case AnimationMode.Secondary:
                currentItem.OnSecondaryAction(lookPosition);
                break;
            case AnimationMode.Alternative:
                currentItem.OnAlternativeAction(lookPosition);
                break;
        }

    }

    public void OnSecondary(InputAction.CallbackContext context)
    {
        if (!isControllable) return;
        if (context.performed)
        {
            if (currentItem != null)
            {
                currentItem.OnSecondaryAction(lookPosition);
            }
        }
    }

    public void OnAlternative(InputAction.CallbackContext context)
    {
        if (!isControllable) return;
        if (context.performed)
        {
            if (currentItem != null)
            {
                currentItem.OnAlternativeAction(lookPosition);
            }
        }
    }

    #endregion

    #region Hotbar

    public void OnHotbar(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            var value = int.Parse(context.control.name);
            ChangeHotbarIndex(value);
        }
    }

    public void OnHotbarScroll(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            var value = inventory.CurrentHotbarIndex - (int)Mathf.Sign(context.ReadValue<float>());
            ChangeHotbarIndex(value);
        }
    }

    private void ChangeHotbarIndex(int value)
    {
        AudioManager.Main.PlaySoundEffect(SoundEffect.UIHover);

        // Clamp and loop the hotbar item keys from 0-9
        if (value > 9)
            value = 0;
        else if (value < 0)
            value = 9;

        // Hotbar index change locally, stored in PlayerInventory
        inventory.ChangeHotBarIndex(value);
    }

    #endregion

    public void SetControllable(bool value)
    {
        isControllable = value;

        if (!isControllable)
        {
            movement.SetDirection(Vector2.zero);
            animator.SetBool("IsMoving", false);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(PlayerController.LookPosition.SnapToGrid(), Vector3.one);
    }
}