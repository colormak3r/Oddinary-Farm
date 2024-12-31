using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spider : Animal
{
    [Header("Spider Settings")]
    [SerializeField]
    private AxeProperty axeProperty;
    [SerializeField]
    private MinMaxFloat idleStateChangeCdr = new MinMaxFloat { min = 3, max = 5 };
    private float nextIdleStateChange;

    private BehaviourState thinkingState;
    private BehaviourState burrowingState;
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
            Item.PropertyValue = axeProperty;

            thinkingState = new ThinkingState(this);
            burrowingState = new BurrowingState(this);
            roamingState = new RoamingState(this);

            chasingState = new ChasingState(this);
            attackPrimaryState = new AttackPrimaryState(this);

            idleStates = new BehaviourState[] { thinkingState, burrowingState, roamingState };
            activeStates = new BehaviourState[] { chasingState, attackPrimaryState };
        }        
    }

    protected override void HandleTransitions()
    {
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
                if (TimeManager.Main.IsDay)
                {
                    ChangeState(burrowingState);
                }
                else
                {
                    ChangeState(roamingState);
                }
            }
        }
        else
        {
            nextIdleStateChange = 0;

            if (TargetDetector.DistanceToTarget > axeProperty.Range)
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
