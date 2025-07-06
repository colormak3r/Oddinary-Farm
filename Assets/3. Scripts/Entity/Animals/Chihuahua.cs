/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;
using UnityEngine.UIElements;

public class Chihuahua : Animal
{
    [Header("Chihuahua Settings")]
    private BehaviourState thinkingState;
    private BehaviourState sittingState;
    private BehaviourState followingState;

    private BehaviourState chasingState;
    private BehaviourState attackPrimaryState;
    [SerializeField]
    private float nextSittingTime;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
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
                if (FollowStimulus.IsNotAtAheadPosition(transform.position))
                {
                    if (currentState != followingState)
                    {
                        nextSittingTime = 0f;
                        ChangeState(followingState);
                    }
                }
                else
                {
                    if (nextSittingTime == 0f)
                    {
                        nextSittingTime = Time.time + Random.Range(1f, 3f);
                        if (currentState != thinkingState) ChangeState(thinkingState);
                    }
                    else if (Time.time >= nextSittingTime)
                    {
                        if (currentState != sittingState) ChangeState(sittingState);
                    }
                }
            }
            else
            {
                if (currentState != sittingState) ChangeState(sittingState);
            }
        }
        else
        {
            if (TargetDetector.DistanceToTarget > ItemProperty.Range)
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