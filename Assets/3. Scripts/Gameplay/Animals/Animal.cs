using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

public abstract class Animal : NetworkBehaviour
{
    private static Vector3 LEFT_DIRECTION = new Vector3(-1, 1, 1);
    private static Vector3 RIGHT_DIRECTION = new Vector3(1, 1, 1);

    [Header("Animal Settings")]
    [SerializeField]
    private Transform graphicTransform;
    [SerializeField]
    private bool spriteFacingRight;
    [SerializeField]
    private float roamRadius = 5f;

    private NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    private Animator animator;
    public Animator Animator => animator;

    private NetworkAnimator networkAnimator;
    public NetworkAnimator NetworkAnimator => networkAnimator;

    private EntityMovement movement;
    public EntityMovement Movement => movement;
    private EntityStatus status;
    public EntityStatus Status => status;

    private PreyDetector preyDetector;
    public PreyDetector PreyDetector => preyDetector;

    private Item item;
    public Item Item => item;

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsFacingRight.OnValueChanged += HandleOnIsFacingRightChanged;

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

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        movement = GetComponent<EntityMovement>();
        preyDetector = GetComponent<PreyDetector>();
        item = GetComponentInChildren<Item>();
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned) return;

        currentState?.ExecuteState();
        HandleTransitions();
    }

    protected abstract void HandleTransitions();

    public void ChangeState(BehaviourState newState)
    {
        if (showDebug) Debug.Log($"Change state from {currentStateName} to {newState.GetType().Name}");
        currentState?.ExitState();
        currentState = newState;
        currentStateName = newState.GetType().Name;
        currentState.EnterState();
    }

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
        return (Vector2)transform.position + Random.insideUnitCircle * roamRadius;
    }

    private void OnDrawGizmos()
    {
        if (!PreyDetector) return;

        if (PreyDetector.CurrentPrey)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, PreyDetector.CurrentPrey.transform.position);
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
}
