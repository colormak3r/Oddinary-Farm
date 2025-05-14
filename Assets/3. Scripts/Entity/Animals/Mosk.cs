using UnityEngine;

public class Mosk : Animal
{
    [Header("Spider Settings")]
    [SerializeField]
    private WeaponProperty weaponProperty;

    private BehaviourState thinkingState;
    private BehaviourState roamingState;
    private BehaviourState moveTowardState;
    private BehaviourState guardState;

    private BehaviourState chasingState;
    private BehaviourState attackPrimaryState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            CurrentItem.Initialize(weaponProperty);

            thinkingState = new ThinkingState(this);
            roamingState = new RoamingState(this);
            moveTowardState = new MoveTowardState(this);
            guardState = new GuardState(this);

            chasingState = new ChasingState(this);
            attackPrimaryState = new AttackPrimaryState(this);
        }
    }

    protected override void HandleTransitions()
    {
        if (TargetDetector.CurrentTarget == null)
        {
            if (currentState == chasingState || currentState == attackPrimaryState || currentState == null)
            {
                ChangeState(thinkingState);
            }
            else
            {

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
            if (TargetDetector.DistanceToTarget > weaponProperty.Range)
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
