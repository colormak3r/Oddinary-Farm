using UnityEngine;

public class MoveTowardState : BehaviourState
{
    public MoveTowardState(Animal animal) : base(animal)
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

        Vector2 directionToTarget = animal.MoveTowardStimulus.TargetPosition - (Vector2)animal.transform.position;
        animal.MoveDirection(directionToTarget.normalized);
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        animal.StopMovement();
    }
}
