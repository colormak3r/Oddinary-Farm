using ColorMak3r.Utility;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Settings")]
    [SerializeField]
    private Transform graphicTransform;
    [SerializeField]
    private bool spriteFacingRight;

    [Header("Previewer")]
    [SerializeField]
    private Color validColor;
    [SerializeField]
    private Color invalidColor;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;

    private bool isControllable = true;

    private Vector2 lookPosition;
    private Vector2 playerPosition_cached = Vector2.one;

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

    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    public static Vector2 LookPosition;
    private Vector2 lookPosition_snapped_cached;

    private void Awake()
    {
        movement = GetComponent<EntityMovement>();
        inventory = GetComponent<PlayerInventory>();
        interaction = GetComponent<PlayerInteraction>();
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        animationController = GetComponentInChildren<PlayerAnimationController>();
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
            graphicTransform.localScale = spriteFacingRight ? RIGHT_DIRECTION : LEFT_DIRECTION;
        else
            graphicTransform.localScale = spriteFacingRight ? LEFT_DIRECTION : RIGHT_DIRECTION;
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
        // Set camera
        Camera.main.transform.parent = transform;
        Camera.main.transform.localPosition = Camera.main.transform.position;

        yield return new WaitUntil(() => GameManager.Main.IsInitialized);

        // Set control
        InputManager.Main.InputActions.Gameplay.SetCallbacks(this);
        InputManager.Main.SwitchMap(InputMap.Gameplay);

        // Set previewer
        previewer = Previewer.Main;

        isOwner = true;
    }

    private void OnDisable()
    {
        if (isOwner)
        {
            Camera.main.transform.parent = null;
            InputManager.Main.InputActions.Gameplay.SetCallbacks(null);
        }
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

        lookPosition = Camera.main.ScreenToWorldPoint(context.ReadValue<Vector2>());
        LookPosition = lookPosition;
        IsFacingRight.Value = (lookPosition - (Vector2)transform.position).x > 0;

        var lookPosition_snapped = lookPosition.SnapToGrid();
        if (lookPosition_snapped != lookPosition_snapped_cached)
        {
            lookPosition_snapped_cached = lookPosition_snapped;
            Preview();
        }
    }

    private void Preview()
    {
        previewer.MoveTo(lookPosition_snapped_cached);
        /*if((position - (Vector2)transform.position).magnitude > spawnerProperty.Range) 
    position = (Vector2)transform.position + (position - (Vector2)transform.position).normalized * spawnerProperty.Range;*/
        var item = inventory.CurrentItemOnLocal;
        var tool = item != null && item is Tool ? (Tool)item : null;
        var spawner = item != null && item is Spawner ? (Spawner)item : null;
        if (tool != null || spawner != null)
        {
            previewer.Show(true);
            previewer.SetIcon(tool ? tool.ToolProperty.IconSprite : spawner.SpawnerProperty.IconSprite);
            previewer.SetSize(tool ? tool.ToolProperty.Size : spawner.SpawnerProperty.Size);
            if (item.CanPrimaryAction(lookPosition_snapped_cached))
            {
                previewer.SetColor(validColor);
            }
            else
            {
                previewer.SetColor(invalidColor);
            }
        }
        else
        {
            previewer.Show(false);
        }

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
            var currentItem = inventory.CurrentItemOnLocal;
            if (currentItem != null && Time.time > nextPrimary)
            {
                var itemProperty = currentItem.PropertyValue;
                nextPrimary = Time.time + itemProperty.PrimaryCdr;

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

    public void Chop(AnimationMode mode)
    {
        if (!IsOwner) return;

        var currentItem = inventory.CurrentItemOnLocal;
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
                    if (inventory.ConsumeItemOnClient(inventory.CurrentHotbarIndex))
                    {
                        currentItem.OnPrimaryAction(primaryPosition.Value);
                    }
                }
                else
                {
                    currentItem.OnPrimaryAction(primaryPosition.Value);
                }

                Preview();
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
            var currentItem = inventory.CurrentItemOnLocal;
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
            var currentItem = inventory.CurrentItemOnLocal;
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

        Preview();
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
