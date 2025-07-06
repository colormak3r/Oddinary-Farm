/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

public class Mosk : Animal
{
    [Header("Spider Settings")]
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
