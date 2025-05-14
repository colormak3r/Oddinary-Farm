using UnityEngine;
using ColorMak3r.Utility;

public class Snail : Animal
{
    [Header("Snail Settings")]
    [SerializeField]
    private HandProperty handProperty;
    [SerializeField]
    private MinMaxFloat idleStateChangeCdr = new MinMaxFloat { min = 3, max = 5 };
    private float nextIdleStateChange;

    private BehaviourState thinkingState;
    private BehaviourState nibblingState;
    private BehaviourState roamingState;

    private BehaviourState chasingState;
    private BehaviourState attackPrimaryState;

    private BehaviourState[] idleStates;
    private BehaviourState[] activeStates;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            CurrentItem.Initialize(handProperty);

            thinkingState = new ThinkingState(this);
            nibblingState = new NibblingState(this);
            roamingState = new RoamingState(this);

            chasingState = new ChasingState(this);
            attackPrimaryState = new AttackPrimaryState(this);

            idleStates = new BehaviourState[] { thinkingState, nibblingState, roamingState };
            activeStates = new BehaviourState[] { chasingState, attackPrimaryState };
        }
    }

    protected override void HandleTransitions()
    {
        if (TargetDetector.CurrentTarget == null)
        {
            // Idle States
            if (Time.time > nextIdleStateChange)
            {
                nextIdleStateChange = Time.time + Random.Range(idleStateChangeCdr.min, idleStateChangeCdr.max);

                var newState = idleStates[Random.Range(0, idleStates.Length)];
                ChangeState(newState);
            }
        }
        else
        {
            // Active States
            nextIdleStateChange = 0;
            if (TargetDetector.DistanceToTarget > handProperty.Range)
            {
                if (currentState != chasingState) ChangeState(chasingState);
            }
            else
            {
                if (currentState != attackPrimaryState) ChangeState(attackPrimaryState);
            }
        }
    }
}


