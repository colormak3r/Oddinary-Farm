using UnityEngine;

public class FollowingState : BehaviourState
{
    public FollowingState(Animal animal) : base(animal)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        var followStimulus = Animal.FollowStimulus;
        if (followStimulus.TargetRBody == null) return;

        Vector2 petPosition = Animal.transform.position;
        Vector2 directionToTarget = followStimulus.AheadPosition - petPosition;

        Animal.MoveDirection(directionToTarget.normalized);
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        Animal.StopMovement();
    }
}
