/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

public enum ActiveTime
{
    Neutral,
    Day,
    Night
}

public abstract class Animal : NetworkBehaviour
{
    private static Vector3 LEFT_DIRECTION = new Vector3(-1, 1, 1);
    private static Vector3 RIGHT_DIRECTION = new Vector3(1, 1, 1);
    public static string ANIMATOR_IS_MOVING = "IsMoving";
    public static string ANIMATOR_IS_NIBBLING = "IsNibbling";
    public static string ANIMATOR_PRIMARY_ACTION = "PrimaryAction";
    public static string ANIMATOR_IS_SITTING = "IsSitting";

    [Header("Animal Settings")]
    [SerializeField]
    private Transform graphicTransform;
    [SerializeField]
    private bool spriteFacingRight;
    [SerializeField]
    private float roamRadius = 5f;
    [SerializeField]
    private ItemProperty itemProperty;
    public ItemProperty ItemProperty => itemProperty;

    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    private Animator animator;
    public Animator Animator => animator;

    private NetworkAnimator networkAnimator;
    public NetworkAnimator NetworkAnimator => networkAnimator;

    private EntityMovement movement;
    public EntityMovement Movement => movement;

    private EntityStatus status;
    public EntityStatus Status => status;

    private Rigidbody2D rbody;
    public Rigidbody2D Rbody => rbody;

    private TargetDetector targetDetector;
    public TargetDetector TargetDetector => targetDetector;

    private ThreatDetector threatDetector;
    public ThreatDetector ThreatDetector => threatDetector;

    private HungerStimulus hungerStimulus;
    public HungerStimulus HungerStimulus => hungerStimulus;

    private FollowStimulus followStimulus;
    public FollowStimulus FollowStimulus => followStimulus;

    private MoveTowardStimulus moveTowardStimulus;
    public MoveTowardStimulus MoveTowardStimulus => moveTowardStimulus;

    private Item currentItem;
    public Item CurrentItem => currentItem;

    public float RemainingDistance { get; private set; }

    [Header("Debugs")]
    [SerializeField]
    private bool showDebug;
    public bool ShowDebug => showDebug;
    [SerializeField]
    private Vector2 destination;
    [HideInInspector]
    public UnityEvent OnDestinationReached;
    [SerializeField]
    protected string currentStateName;
    protected BehaviourState currentState;


    private Coroutine moveCoroutine;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        movement = GetComponent<EntityMovement>();
        status = GetComponent<EntityStatus>();
        rbody = GetComponent<Rigidbody2D>();
        targetDetector = GetComponent<TargetDetector>();
        threatDetector = GetComponent<ThreatDetector>();
        hungerStimulus = GetComponent<HungerStimulus>();
        followStimulus = GetComponent<FollowStimulus>();
        moveTowardStimulus = GetComponent<MoveTowardStimulus>();
        if (itemProperty != null)
        {
            var weaponObj = Instantiate(itemProperty.Prefab, transform);
            currentItem = weaponObj.GetComponent<Item>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsFacingRight.OnValueChanged += HandleOnIsFacingRightChanged;

        HandleOnIsFacingRightChanged(false, IsFacingRight.Value);

        if (currentItem != null) currentItem.Initialize(itemProperty);
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

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned) return;

        HandleTransitions();
        currentState?.ExecuteState();
    }

    protected abstract void HandleTransitions();

    public void ChangeState(BehaviourState newState)
    {
        if (showDebug) Debug.Log($"Change state from {currentStateName} to {newState.GetType().Name}");

        var oldState = currentState;
        currentState?.ExitState();
        currentState = newState;
        currentStateName = newState.GetType().Name;
        currentState.EnterState();

        OnStateChanged(oldState, newState);
    }

    protected virtual void OnStateChanged(BehaviourState oldState, BehaviourState newState)
    {

    }

    #region Movement

    public void MoveTo(Vector2 position, float precision = 0.1f)
    {
        if (moveCoroutine != null) StopMovement();
        destination = position;
        moveCoroutine = StartCoroutine(MoveCoroutine(position, precision));
    }

    public void MoveDirection(Vector2 direction)
    {
        if (direction.x > 0)
            IsFacingRight.Value = true;
        else if (direction.x < 0)
            IsFacingRight.Value = false;

        movement.SetDirection(direction);
    }

    public void StopMovement()
    {
        destination = Vector2.zero;
        movement.SetDirection(Vector2.zero);
        RemainingDistance = float.PositiveInfinity;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
    }

    private IEnumerator MoveCoroutine(Vector2 position, float precision)
    {
        RemainingDistance = float.PositiveInfinity;
        while (RemainingDistance > precision)
        {
            var direction = position - (Vector2)transform.position;
            RemainingDistance = direction.magnitude;

            if (direction.x > 0)
                IsFacingRight.Value = true;
            else if (direction.x < 0)
                IsFacingRight.Value = false;

            movement.SetDirection(direction);

            yield return new WaitForFixedUpdate();
        }

        // Must have this condition to prevent stopping movement prematurally if OnDestinationReached set a new destination
        if (OnDestinationReached != null)
        {
            OnDestinationReached.Invoke();
        }
        else
        {
            StopMovement();
        }
    }

    public Vector2 GetRandomPointInRange()
    {
        const float minDistance = 1f;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Mathf.Sqrt(Random.Range(minDistance * minDistance, roamRadius * roamRadius));

        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        return (Vector2)transform.position + offset;
    }

    public void SetFacing(bool isFacingRight)
    {
        IsFacingRight.Value = isFacingRight;
    }

    #endregion

    #region Utility

    private void OnDrawGizmos()
    {
        if (!TargetDetector) return;

        if (TargetDetector.CurrentTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, TargetDetector.CurrentTarget.transform.position);
        }
        else
        {
            if (destination != Vector2.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, destination);
            }
        }
    }

    #endregion
}
