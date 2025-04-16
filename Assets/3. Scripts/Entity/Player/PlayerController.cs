using ColorMak3r.Utility;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public enum AnimationMode
{
    Primary,
    Secondary,
    Alternative
}

public class PlayerController : NetworkBehaviour, DefaultInputActions.IGameplayActions, IControllable, IMoveable
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
    [SerializeField]
    private RenderTexture renderTexture;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;

    [SerializeField]
    private Vector2 lookPosition;
    public static Vector2 LookPosition { get; private set; }
    private Vector2 playerPosition_cached = Vector2.one;
    [SerializeField]
    private Vector2 screenMousePos;

    private float nextPrimary;
    private float nextSecondary;

    private EntityMovement movement;
    private PlayerInventory inventory;
    private PlayerInteraction interaction;
    private Previewer previewer;
    private Rigidbody2D rbody;
    private Animator animator;
    private NetworkAnimator networkAnimator;
    private PlayerAnimationController animationController;

    private bool isOwner;
    private bool isInitialized;

    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    public static Transform MuzzleTransform;

    private bool rotateArm = false;

    private Item currentItem;
    private Camera mainCamera;
    private GameplayRenderer gameplayRenderer;

    private void Awake()
    {
        movement = GetComponent<EntityMovement>();
        inventory = GetComponent<PlayerInventory>();
        interaction = GetComponent<PlayerInteraction>();
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        rbody = GetComponent<Rigidbody2D>();
        animationController = GetComponentInChildren<PlayerAnimationController>();
        mainCamera = Camera.main;
        gameplayRenderer = GameplayRenderer.Main;
    }

    private void OnEnable()
    {
        inventory.OnCurrentItemPropertyChanged += HandleCurrentItemPropertyChanged;
        inventory.OnCurrentItemChanged += HandleCurrentItemChanged;
    }

    private void OnDisable()
    {
        inventory.OnCurrentItemPropertyChanged -= HandleCurrentItemPropertyChanged;
        inventory.OnCurrentItemChanged -= HandleCurrentItemChanged;


        if (isOwner)
        {
            InputManager.Main.InputActions.Gameplay.SetCallbacks(null);
        }
    }

    private void HandleCurrentItemChanged(Item item) => currentItem = item;

    private void HandleCurrentItemPropertyChanged(ItemProperty itemProperty)
    {
        if (isOwner) Preview(lookPosition);

        if (itemProperty == null)
        {
            armRotation.SetActive(false);
            arm.SetActive(true);
            itemRenderer.sprite = null;
            return;
        }

        rotateArm = itemProperty is RangedWeaponProperty;
        if (rotateArm)
        {
            armRotation.SetActive(true);
            arm.SetActive(false);
            itemRotationRenderer.sprite = itemProperty.ObjectSprite;
        }
        else
        {
            armRotation.SetActive(false);
            arm.SetActive(true);
            itemRenderer.sprite = itemProperty.ObjectSprite;
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
            StartCoroutine(WorldGenerator.Main.BuildWorld(transform.position));
        }

        var rawImageRectTransform = gameplayRenderer.RawImage.rectTransform;
        // Convert mouse position to local position within RawImage
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImageRectTransform,
            screenMousePos,
            gameplayRenderer.UICamera,
            out var localMousePos);

        // Normalize local coordinates (0-1)
        Rect rect = rawImageRectTransform.rect;
        float normalizedX = (localMousePos.x - rect.x) / rect.width;
        float normalizedY = (localMousePos.y - rect.y) / rect.height;

        // Convert normalized coordinates to RenderTexture coordinates
        renderTexPos = new Vector3(
            normalizedX * renderTexture.width,
            normalizedY * renderTexture.height,
            mainCamera.nearClipPlane);

        lookPosition = mainCamera.ScreenToWorldPoint(renderTexPos);
        LookPosition = lookPosition;
        IsFacingRight.Value = (lookPosition - (Vector2)transform.position).x > 0;

        if (rotateArm) RotateArm(lookPosition);

        Preview(lookPosition);
    }
    [SerializeField]
    Vector3 renderTexPos;

    private void Initialize()
    {
        if (!IsOwner) return;
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Main.IsInitialized);

        // Set camera
        Spectator.Main.SetCamera(OwnerClientId);

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
        if (!isControllable || !isMoveable) return;

        var direction = context.ReadValue<Vector2>().normalized;
        movement.SetDirection(direction);
        animator.SetBool("IsMoving", direction != Vector2.zero);
    }

    public void OnLookPosition(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (Camera.main == null) return;

        screenMousePos = context.ReadValue<Vector2>();
    }

    public void OnLookDirection(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (Camera.main == null) return;

        CursorUI.Main.MoveCursor(context.ReadValue<Vector2>().normalized);
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
            if (ShopUI.Main.IsShowing)
            {
                ShopUI.Main.CloseShop();
            }
            else
            {
                if (OptionsUI.Main.IsShowing)
                {
                    // TODO: Use UI Map instead
                    OptionsUI.Main.Hide();
                }
                else
                {
                    InventoryUI.Main.CloseInventory();
                    OptionsUI.Main.Show();
                }
            }
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

    public void OnToggleUI(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            UIManager.Main.ToggleUI();
        }
    }

    #region Player Action

    private Vector2? primaryPosition;
    private Vector2? secondaryPosition;

    private Coroutine primaryCoroutine;
    private bool isPrimaryCoroutineRunning;
    private bool firstCallIgnored;
    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            isPrimaryCoroutineRunning = true;
            firstCallIgnored = false;
            primaryCoroutine = StartCoroutine(PrimaryActionCoroutine());
        }
        else if (context.canceled)
        {
            OnPrimaryCancelled();
        }
    }

    private void OnPrimaryCancelled()
    {
        if (primaryCoroutine != null)
        {
            isPrimaryCoroutineRunning = false;
            StopCoroutine(primaryCoroutine);
        }
    }

    private IEnumerator PrimaryActionCoroutine()
    {
        while (true)
        {
            if (currentItem != null && Time.time > nextPrimary)
            {
                var itemProperty = currentItem.BaseProperty;
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
            yield return new WaitUntil(() => Time.time > nextPrimary);
            firstCallIgnored = true;
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

        if (!isPrimaryCoroutineRunning && firstCallIgnored) return;

        var itemProperty = currentItem.BaseProperty;
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

    public void OnHotbarUp(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            var value = inventory.CurrentHotbarIndex + 1;
            ChangeHotbarIndex(value);
        }
    }

    public void OnHotbarDown(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (context.performed)
        {
            var value = inventory.CurrentHotbarIndex - 1;
            ChangeHotbarIndex(value);
        }
    }

    private void ChangeHotbarIndex(int value)
    {
        // Play sound effect
        AudioManager.Main.PlaySoundEffect(SoundEffect.UIHover);

        // Stop primary action coroutine
        if (primaryCoroutine != null)
        {
            isPrimaryCoroutineRunning = false;
            StopCoroutine(primaryCoroutine);
        }

        // Clamp and loop the hotbar item keys from 0-9
        if (value > 9)
            value = 0;
        else if (value < 0)
            value = 9;

        // Hotbar index change locally, stored in PlayerInventory
        inventory.ChangeHotBarIndex(value);
    }

    #endregion

    private bool isControllable = true;
    public void SetControllable(bool value)
    {
        isControllable = value;

        if (!isControllable)
        {
            movement.SetDirection(Vector2.zero);
            animator.SetBool("IsMoving", false);
            rbody.linearVelocity = Vector2.zero;
            OnPrimaryCancelled();
        }
    }

    private bool isMoveable = true;
    public void SetMoveable(bool value)
    {
        isMoveable = value;
        if (!isMoveable)
        {
            movement.SetDirection(Vector2.zero);
            animator.SetBool("IsMoving", false);
            rbody.linearVelocity = Vector2.zero;
            OnPrimaryCancelled();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(PlayerController.LookPosition.SnapToGrid(), Vector3.one);
    }
}