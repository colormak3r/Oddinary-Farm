using UnityEngine;

public class TunnelingSpider : Animal
{
    [Header("Tunneling Spider Settings")]
    [SerializeField]
    private WeaponProperty weaponProperty;
    [SerializeField]
    private MinMaxFloat idleStateChangeCdr = new MinMaxFloat { min = 3, max = 5 };
    private float nextIdleStateChange;

    private BehaviourState thinkingState;
    private BehaviourState burrowingState;
    private BehaviourState roamingState;
    private BehaviourState moveTowardState;
    private BehaviourState guardState;

    private BehaviourState chasingState;
    private BehaviourState attackPrimaryState;

    private BehaviourState[] idleStates;
    private BehaviourState[] activeStates;

    private TunnelingController tunnelingController;
    private float nextCanTunnel;

    protected override void Awake()
    {
        base.Awake();

        tunnelingController = GetComponent<TunnelingController>();
        if (tunnelingController == null)
        {
            Debug.LogError("TunnelingController component is missing on TunnelingSpider.");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            CurrentItem.Initialize(weaponProperty);

            thinkingState = new ThinkingState(this);
            burrowingState = new BurrowingState(this);
            roamingState = new RoamingState(this);
            moveTowardState = new MoveTowardState(this);
            guardState = new GuardState(this);

            chasingState = new ChasingState(this);
            attackPrimaryState = new AttackPrimaryState(this);

            idleStates = new BehaviourState[] { thinkingState, burrowingState, roamingState };
            activeStates = new BehaviourState[] { chasingState, attackPrimaryState };
        }
    }

    protected override void HandleTransitions()
    {
        if (Time.time > nextCanTunnel && tunnelingController.IsTunnelingValue == false)
        {
            tunnelingController.SetTunneling(true);
        }

        if (TargetDetector.CurrentTarget == null)
        {
            // Idle States
            if (Time.time < nextIdleStateChange) return;
            nextIdleStateChange = Time.time + Random.Range(idleStateChangeCdr.min, idleStateChangeCdr.max);

            if (currentState == chasingState || currentState == attackPrimaryState || currentState == null)
            {
                ChangeState(thinkingState);
            }
            else
            {
                // Since the spider is tunnel, it can be active during the day
                // Remove the check for TimeManager.Main.IsDay
                if (MoveTowardStimulus.IsGuardMode)
                {
                    if (currentState != guardState)
                        ChangeState(guardState);
                }
                else
                {
                    if (!MoveTowardStimulus.ReachedTarget)
                    {
                        if (currentState != moveTowardState)
                            ChangeState(moveTowardState);
                    }
                    else
                    {
                        if (currentState != roamingState)
                            ChangeState(roamingState);
                    }
                }
            }
        }
        else
        {
            nextIdleStateChange = 0;

            if (TargetDetector.DistanceToTarget > weaponProperty.Range)
            {
                if (currentState != chasingState) ChangeState(chasingState);
            }
            else
            {
                if (currentState != attackPrimaryState) ChangeState(attackPrimaryState);
                if (tunnelingController.IsTunnelingValue)
                {
                    tunnelingController.SetTunneling(false);
                    nextCanTunnel = Time.time + 5f; // Cooldown before tunneling again
                }
            }
        }
    }
}