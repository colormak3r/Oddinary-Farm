/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   05/16/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Controller Settings")]
    [SerializeField]
    private GameObject cursor;
    [SerializeField]
    private float sensitivity = 10f;
    [SerializeField]
    private float smoothing = 10f;

    [Header("Graphic Settings")]
    [SerializeField]
    private RenderTexture renderTexture;
    [SerializeField]
    private SpriteRenderer rightItemRenderer;
    [SerializeField]
    private SpriteRenderer leftItemRenderer;

    [Header("Rotation Settings")]
    [SerializeField]
    private SpriteRenderer itemRotationRenderer;
    [SerializeField]
    private GameObject arm;
    [SerializeField]
    private GameObject armRotation;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private bool isController;
    [SerializeField]
    private Vector2 lookPosition;
    public static Vector2 LookPosition { get; private set; }

    [SerializeField]
    private Vector2 screenMousePos;
    [SerializeField]
    private Vector2 controllerDirection;

    private EntityMovement movement;
    private PlayerInventory inventory;
    private PlayerInteraction interaction;
    private Previewer previewer;
    private Rigidbody2D rbody;
    private Animator animator;
    private PlayerAnimationController animationController;
    private LassoController lassoController;

    private bool isOwner;
    private bool isInitialized;

    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    public static Transform MuzzleTransform;

    private bool rotateArm = false;
    private bool isPointerOverUI;

    private Item currentItem;
    private Camera mainCamera;
    private GameplayRenderer gameplayRenderer;

    private void Awake()
    {
        movement = GetComponent<EntityMovement>();
        inventory = GetComponent<PlayerInventory>();
        interaction = GetComponent<PlayerInteraction>();
        animator = GetComponentInChildren<Animator>();
        rbody = GetComponent<Rigidbody2D>();
        animationController = GetComponentInChildren<PlayerAnimationController>();
        lassoController = GetComponentInChildren<LassoController>();

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

    private void Update()
    {
        // Run Client-Side only
        if (!IsOwner || !isInitialized) return;

        if (!GameManager.Main.IsInitialized) return;

        // WorldGenerator.BuildWorld has been moved to WorldRenderer
        // This is so the spectator can also make use of the world generator

        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        // Set cursor position
        if (!isController)
        {
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
            var renderTexPos = new Vector3(
                normalizedX * renderTexture.width,
                normalizedY * renderTexture.height,
                mainCamera.nearClipPlane);

            lookPosition = mainCamera.ScreenToWorldPoint(renderTexPos);
        }
        else
        {
            float camHeight = 2f * mainCamera.orthographicSize;
            float camWidth = camHeight * mainCamera.aspect;

            Vector3 camCenter = mainCamera.transform.position;

            float left = camCenter.x - camWidth / 2f;
            float right = camCenter.x + camWidth / 2f;
            float bottom = camCenter.y - camHeight / 2f;
            float top = camCenter.y + camHeight / 2f;

            // Move cursor
            Vector3 targetPos = cursor.transform.position + (Vector3)(controllerDirection.normalized * sensitivity);
            cursor.transform.position = Vector3.Lerp(cursor.transform.position, targetPos, smoothing * Time.deltaTime);

            // Clamp position
            Vector2 clampedPos = cursor.transform.position;
            clampedPos.x = Mathf.Clamp(clampedPos.x, left, right);
            clampedPos.y = Mathf.Clamp(clampedPos.y, bottom, top);
            cursor.transform.position = (Vector3)clampedPos;

            lookPosition = cursor.transform.position;
        }

        LookPosition = lookPosition;

        IsFacingRight.Value = (lookPosition - (Vector2)transform.position).x > 0;

        if (rotateArm) RotateArm(lookPosition);

        Preview(lookPosition);
    }

    #region NetworkVariable Callback

    private void HandleCurrentItemChanged(Item item) => currentItem = item;

    private void HandleCurrentItemPropertyChanged(ItemProperty itemProperty)
    {
        if (isOwner) Preview(lookPosition);

        if (itemProperty == null)
        {
            armRotation.SetActive(false);
            arm.SetActive(true);
            rightItemRenderer.sprite = null;
            return;
        }

        rotateArm = itemProperty is RangedWeaponProperty || itemProperty is LaserWeaponProperty;
        if (rotateArm)
        {
            // The player is holding a ranged weapon
            armRotation.SetActive(true);
            arm.SetActive(false);
            itemRotationRenderer.sprite = itemProperty.ObjectSprite;
        }
        else
        {
            // The player is holding anything else
            armRotation.SetActive(false);
            arm.SetActive(true);
            rightItemRenderer.sprite = itemProperty.ObjectSprite;

            if (itemProperty is LassoProperty)
            {
                rightItemRenderer.sprite = null;
                lassoController.SetLassoState(LassoState.Visible);
            }
            else
            {
                lassoController.SetLassoState(LassoState.Hidden);
            }
        }
    }

    private void HandleOnIsFacingRightChanged(bool previous, bool current)
    {
        var isFacingRight = current;
        if (isFacingRight)
            graphicTransform.localScale = spriteFacingRight ? RIGHT_DIRECTION : LEFT_DIRECTION;
        else
            graphicTransform.localScale = spriteFacingRight ? LEFT_DIRECTION : RIGHT_DIRECTION;
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        if (!IsOwner) return;
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Main.IsInitialized);

        // Set camera
        Spectator.Main.SetCamera(transform);

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

    #endregion

    #region Input Callbacks

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
        isController = false;
        cursor.SetActive(false);
        Cursor.visible = true;

        screenMousePos = context.ReadValue<Vector2>();
    }

    public void OnLookDirection(InputAction.CallbackContext context)
    {
        if (!isControllable) return;

        if (Camera.main == null) return;
        isController = true;
        cursor.SetActive(true);
        Cursor.visible = false;

        controllerDirection = context.ReadValue<Vector2>();
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
            // This should be called to open the options menu only
            if (InventoryUI.Main.TabInventoryUIBehaviour.IsShowing)
            {
                InventoryUI.Main.CloseTabInventory();
            }
            else if (!OptionsUI.Main.IsShowing)
            {
                PauseButtonUI.Main.PauseButtonClicked();
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

    #endregion

    #region Player Action

    private Coroutine primaryCoroutine;
    private bool isPrimaryCoroutineRunning = false;
    private bool firstPrimaryTriggered = false; // Use for charge weapons to prevent multiple triggers at once
    private float nextPrimary = 0f;
    private float primaryStarted;
    private Vector2 invalidPosition_cached;

    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (!isControllable || currentItem == null) return;

        if (isPointerOverUI)
        {
            // When the gun is charged, the player can still shoot if UI is in the way
            if (currentItem is not LaserWeapon laserWeapon || !laserWeapon.IsCharging) return;
        }

        if (context.started)
        {
            OnPrimaryStarted();

            // Update Stats
            if (currentItem.BaseProperty != null)
            {
                StatisticsManager.Main.UpdateStat(StatisticType.ItemsUsed, currentItem.BaseProperty);
            }
        }
        else if (context.canceled)
        {
            OnPrimaryCanceled();
        }
    }

    private void OnPrimaryStarted()
    {
        if (isPrimaryCoroutineRunning) return;

        primaryStarted = Time.time;
        isPrimaryCoroutineRunning = true;
        primaryCoroutine = StartCoroutine(PrimaryCoroutine());
    }

    private void OnPrimaryCanceled()
    {
        if (primaryCoroutine == null) return;

        if (currentItem is LaserWeapon laserWeapon)
        {
            if (Time.time - primaryStarted > laserWeapon.BaseProperty.PrimaryCdr)
            {
                SetTriggerRpc("Shoot");
                SyncArmRotationRpc(lookPosition);
            }
        }

        isPrimaryCoroutineRunning = false;
        firstPrimaryTriggered = false; // Reset for next charge action
        StopCoroutine(primaryCoroutine);
        currentItem.OnPrimaryCancel(lookPosition);
    }

    private IEnumerator PrimaryCoroutine()
    {
        while (isPrimaryCoroutineRunning)
        {
            if (Time.time >= nextPrimary)
            {
                var itemProperty = currentItem.BaseProperty;
                nextPrimary = Time.time + itemProperty.PrimaryCdr;

                if (currentItem is RangedWeapon)
                {
                    SetTriggerRpc("Shoot");
                    currentItem.OnPrimaryAction(lookPosition);
                    SyncArmRotationRpc(lookPosition);
                }
                else if (currentItem is LaserWeapon laserWeapon)
                {
                    // Prevent multiple triggers at once
                    // Need to be one if level down to prevent fallthrough to default case
                    if (!firstPrimaryTriggered)
                    {
                        firstPrimaryTriggered = true;

                        // SetTriggerRpc("Shoot"); Start Charge animation
                        currentItem.OnPrimaryAction(lookPosition);
                        SyncArmRotationRpc(lookPosition);
                    }
                }
                else
                {
                    // Default case
                    if (currentItem.CanPrimaryAction(lookPosition))
                    {
                        // Animation
                        SetTriggerRpc("Chop");

                        // Action
                        currentItem.OnPrimaryAction(lookPosition);

                        // Inventory
                        if (itemProperty.IsConsummable)
                        {
                            inventory.ConsumeItemOnClient(inventory.CurrentHotbarIndex);
                        }
                    }
                    else
                    {
                        // If the position is invalid, play error sound only once
                        // Temporarily disabled for feedback purposes
                        /*if (invalidPosition_cached != lookPosition)
                        {
                            invalidPosition_cached = lookPosition;
                            AudioManager.Main.PlaySoundEffect(SoundEffect.UIError);
                        }*/
                    }
                }
            }

            yield return null;
        }
    }

    public void OnSecondary(InputAction.CallbackContext context)
    {
        if (!isControllable || isPointerOverUI || currentItem == null) return;

        if (context.performed) currentItem.OnSecondaryAction(lookPosition);
    }

    public void OnAlternative(InputAction.CallbackContext context)
    {
        if (!isControllable || isPointerOverUI || currentItem == null) return;

        if (context.performed) currentItem.OnAlternativeAction(lookPosition);
    }

    [Rpc(SendTo.NotMe)]
    private void SyncArmRotationRpc(Vector2 lookPosition)
    {
        RotateArm(lookPosition);
    }

    [Rpc(SendTo.Everyone)]
    private void SetTriggerRpc(FixedString32Bytes animation)
    {
        animator.SetTrigger(animation.ToString());
    }

    /*private Vector2? primaryPosition;
    private Vector2? secondaryPosition;

    private Coroutine primaryCoroutine;
    private bool isPrimaryCoroutineRunning;
    private bool firstCallIgnored;
    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (!isControllable || isPointerOverUI) return;

        if (context.performed)
        {
            if (!isPrimaryCoroutineRunning)
            {
                isPrimaryCoroutineRunning = true;
                firstCallIgnored = false;
                primaryCoroutine = StartCoroutine(PrimaryActionCoroutine());
            }
        }
        else if (context.canceled)
        {
            OnPrimaryCanceled();
        }
    }

    private void OnPrimaryCanceled()
    {
        if (primaryCoroutine != null)
        {
            laserChargeTime = 0;
            currentItem.OnPrimaryCancel();
            isPrimaryCoroutineRunning = false;
            StopCoroutine(primaryCoroutine);
        }
    }

    private float laserChargeTime = 0;

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
                    SetTriggerRpc("Shoot");
                    currentItem.OnPrimaryAction(lookPosition);
                    SyncArmRotationRpc(lookPosition);
                }
                if (currentItem is LaserWeapon laserWeapon)
                {
                    laserChargeTime += Time.deltaTime;
                    if (laserChargeTime > 0f)
                    {

                    }


                    SyncArmRotationRpc(lookPosition);
                }
                else
                {
                    if (currentItem.CanPrimaryAction(lookPosition))
                    {
                        primaryPosition = lookPosition;
                        animationController.ChopAnimationMode = AnimationMode.Primary;
                        SetTriggerRpc("Chop");
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

    [Rpc(SendTo.Everyone)]
    private void SetTriggerRpc(FixedString32Bytes animation)
    {
        animator.SetTrigger(animation.ToString());
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
    */

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
        // Prevent changing hotbar index when lasso is thrown
        if (currentItem is Lasso)
        {
            if (lassoController.CurrentStateValue == LassoState.Thrown || lassoController.CurrentStateValue == LassoState.Capturing)
                return;
            else
                lassoController.SetLassoState(LassoState.Hidden);
        }

        // Prevent changing hotbar index when laser weapon is charging
        if (currentItem is LaserWeapon laserWeapon && laserWeapon.IsCharging) return;

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

    #region Utility
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

    private bool isControllable = true;
    public void SetControllable(bool value)
    {
        isControllable = value;

        if (!isControllable)
        {
            movement.SetDirection(Vector2.zero);
            animator.SetBool("IsMoving", false);
            rbody.linearVelocity = Vector2.zero;
            OnPrimaryCanceled();
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
            OnPrimaryCanceled();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(PlayerController.LookPosition.SnapToGrid(), Vector3.one);
    }
    #endregion
}