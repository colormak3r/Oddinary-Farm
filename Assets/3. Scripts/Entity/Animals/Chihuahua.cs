using UnityEngine;
using UnityEngine.UIElements;

public class Chihuahua : Animal
{
    [Header("Chihuahua Settings")]
    [SerializeField]
    private MeleeWeaponProperty weaponProperty;

    private BehaviourState thinkingState;
    private BehaviourState sittingState;
    private BehaviourState followingState;

    private BehaviourState chasingState;
    private BehaviourState attackPrimaryState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            CurrentItem.Initialize(weaponProperty);

            thinkingState = new ThinkingState(this);
            sittingState = new SittingState(this);
            followingState = new FollowingState(this);

            chasingState = new ChasingState(this);
            attackPrimaryState = new AttackPrimaryState(this);
        }
    }

    protected override void HandleTransitions()
    {
        if (TargetDetector.CurrentTarget == null)
        {
            if (FollowStimulus.TargetRBody != null)
            {
                if (FollowStimulus.IsOutsideFollowDistance(transform.position))
                {
                    if (currentState != followingState) ChangeState(followingState);
                }
                else
                {
                    if (currentState != sittingState) ChangeState(sittingState);
                }
            }
            else
            {
                if (currentState != sittingState) ChangeState(sittingState);
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