using UnityEngine;

public class Mosk : Animal
{
    [Header("Spider Settings")]
    [SerializeField]
    private WeaponProperty weaponProperty;

    private BehaviourState thinkingState;
    private BehaviourState roamingState;
    private BehaviourState moveTowardState;

    private BehaviourState chasingState;
    private BehaviourState attackPrimaryState;

    private bool reachedOrigin = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            CurrentItem.Initialize(weaponProperty);

            thinkingState = new ThinkingState(this);
            roamingState = new RoamingState(this);
            moveTowardState = new MoveTowardState(this);

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
                if (!reachedOrigin)
                {
                    ChangeState(moveTowardState);
                    if (((Vector2)transform.position - MoveTowardStimulus.TargetPosition).SqrMagnitude() < 25f)
                    {
                        reachedOrigin = true;
                    }
                }
                else
                {
                    ChangeState(roamingState);
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
