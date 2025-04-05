using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public class TargetDetector : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool raycastTarget = true;
    [SerializeField]
    private float detectRange = 5f;
    [SerializeField]
    private float escapeRange = 7f;
    [Tooltip("Detector get penaly range outsize of active time")]
    [SerializeField]
    private float penaltyRange = 2f;
    [SerializeField]
    private ActiveTime activeTime = ActiveTime.Neutral;
    [SerializeField]
    private LayerMask targetMask;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private Transform currentTarget;
    [SerializeField]
    private float distanceToTarget;

    [HideInInspector]
    public UnityEvent<Transform> OnTargetDetected;
    [HideInInspector]
    public UnityEvent<Transform> OnTargetEscaped;

    private DistanceComparer distanceComparer;
    private EntityStatus entityStatus;

    public Transform CurrentTarget => currentTarget;
    public float DistanceToTarget
    {
        get
        {
            distanceToTarget = currentTarget ? (transform.position - currentTarget.position).magnitude : float.PositiveInfinity;
            return distanceToTarget;
        }
    }

    private void Start()
    {
        distanceComparer = new DistanceComparer(transform, Vector2.zero);
        entityStatus = GetComponent<EntityStatus>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            TimeManager.Main.OnDayStart.AddListener(HandleOnDayStart);
            TimeManager.Main.OnNightStart.AddListener(HandleOnNightStart);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            TimeManager.Main.OnDayStart.RemoveListener(HandleOnDayStart);
            TimeManager.Main.OnNightStart.RemoveListener(HandleOnNightStart);
        }
    }

    private void HandleOnNightStart()
    {
        if (activeTime == ActiveTime.Day) currentTarget = null;
    }

    private void HandleOnDayStart()
    {
        if (activeTime == ActiveTime.Night) currentTarget = null;
    }

    private void Update()
    {
        if (!IsServer) return;

        TrackTarget();
    }

    private void TrackTarget()
    {
        if (currentTarget == null)
        {
            currentTarget = ScanForTarget(transform.position, out var targetStatus);
            if (currentTarget != null)
            {
                targetStatus.OnDeathOnServer.AddListener(HandleOnTargetDie);
                OnPostTargetDetected(targetStatus);

                if (showDebugs) Debug.Log("New target detected: " + currentTarget);
                OnTargetDetected?.Invoke(currentTarget);
            }
        }
        else
        {
            if (Vector3.Distance(currentTarget.position, transform.position) > escapeRange)
            {
                OnTargetEscaped?.Invoke(currentTarget);
                currentTarget = null;
            }
        }
    }

    private Transform ScanForTarget(Vector3 position, out EntityStatus targetStatus)
    {
        targetStatus = null;
        var range = detectRange;

        if (activeTime == ActiveTime.Day && TimeManager.Main.IsNight || activeTime == ActiveTime.Night && TimeManager.Main.IsDay)
            range = penaltyRange;

        var hits = Physics2D.OverlapCircleAll(position, range, targetMask);
        if (hits.Length > 0)
        {
            Transform candidate = null;
            for (int i = 0; i < hits.Length; i++)
            {
                candidate = hits[i].transform;
                if (ValidateValidTarget(candidate, out targetStatus))
                {
                    // Raycast to check for more desireable prey in line of sight
                    if (raycastTarget)
                    {
                        var raycastHits = Physics2D.RaycastAll(transform.position, candidate.position - transform.position, detectRange, targetMask);
                        foreach (var raycastHit in raycastHits)
                        {
                            if (raycastHit.transform == transform) continue;
                            if (raycastHit.transform == candidate)
                            {
                                return candidate;
                            }
                            if (raycastHit.transform != null && ValidateValidTarget(raycastHit.transform, out targetStatus))
                            {
                                return raycastHit.transform;
                            }
                        }
                    }
                    else
                    {
                        return candidate;
                    }
                }
            }
            return null;
        }
        else
        {
            return null;
        }
    }

    protected virtual bool ValidateValidTarget(Transform target, out EntityStatus targetStatus)
    {
        if (target == transform)
        {
            targetStatus = null;
            return false;
        }

        targetStatus = target.GetComponent<EntityStatus>();
        return targetStatus != null && entityStatus.Hostility != targetStatus.Hostility;
    }

    protected virtual void OnPostTargetDetected(EntityStatus targetStatus)
    {

    }

    private void HandleOnTargetDie()
    {
        if (currentTarget != null)
            DeselectTarget($"{currentTarget} died");
    }

    protected void DeselectTarget(string reason)
    {
        if (currentTarget != null)
        {
            var status = currentTarget.GetComponent<EntityStatus>();
            if (status != null)
                status.OnDeathOnServer.RemoveListener(HandleOnTargetDie);
        }

        if (showDebugs) Debug.Log($"Deselecting target: {reason}");
        currentTarget = null;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (TimeManager.Main == null || !TimeManager.Main.IsInitialized) return;

        var detectRange = this.detectRange;

        if (activeTime == ActiveTime.Day && TimeManager.Main.IsNight || activeTime == ActiveTime.Night && TimeManager.Main.IsDay)
            detectRange = 2;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        if (currentTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, escapeRange);
    }
}