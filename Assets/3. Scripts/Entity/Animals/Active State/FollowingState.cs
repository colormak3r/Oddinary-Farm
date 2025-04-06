using UnityEngine;

public class FollowingState : BehaviourState
{
    public FollowingState(Animal animal) : base(animal)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        var followStimulus = animal.FollowStimulus;
        if (followStimulus.TargetRBody == null) return;

        Vector2 petPosition = animal.transform.position;
        Vector2 directionToTarget = followStimulus.AheadPosition - petPosition;

        // Move if pet is significantly away from the target position
        if (followStimulus.IsNotAtAheadPosition(petPosition))
        {
            animal.MoveDirection(directionToTarget.normalized);
        }
        else
        {
            animal.StopMovement();
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        animal.StopMovement();
    }
}
