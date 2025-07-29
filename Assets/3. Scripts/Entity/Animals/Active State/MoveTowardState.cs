using UnityEngine;

public class MoveTowardState : BehaviourState
{
    public MoveTowardState(Animal animal) : base(animal)
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

        Vector2 directionToTarget = AnimalBase.MoveTowardStimulus.TargetPosition - (Vector2)AnimalBase.transform.position;
        AnimalBase.MoveDirection(directionToTarget.normalized);
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        AnimalBase.StopMovement();
    }
}
