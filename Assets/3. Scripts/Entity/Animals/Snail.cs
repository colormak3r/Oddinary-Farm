/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;
using ColorMak3r.Utility;

public class Snail : Animal
{
    [Header(" Snail Settings")]
    [SerializeField]
    private float harvestRange = 1f;
    private BehaviourState thinkingState;
    private BehaviourState followingState;
    private BehaviourState chasingState;
    private BehaviourState harvestState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            thinkingState = new ThinkingState(this);
            followingState = new FollowingState(this);
            chasingState = new ChasingState(this);
            harvestState = new HarvestState(this);
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
                        ChangeState(followingState);
                    }
                }
                else
                {
                    if (currentState != thinkingState) ChangeState(thinkingState);
                }
            }
            else
            {
                // No target, do nothing
                // Code should not reach here
                if (currentState != thinkingState) ChangeState(thinkingState);
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
                if (currentState != harvestState) ChangeState(harvestState);
            }
        }
    }
}


