using UnityEngine;

public class FollowingState : BehaviourState
{
    public FollowingState(Animal animal) : base(animal)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        var followStimulus = AnimalBase.FollowStimulus;
        if (followStimulus.TargetRBody == null) return;

        Vector2 petPosition = AnimalBase.transform.position;
        Vector2 directionToTarget = followStimulus.AheadPosition - petPosition;

        AnimalBase.MoveDirection(directionToTarget.normalized);
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        AnimalBase.StopMovement();
    }
}
