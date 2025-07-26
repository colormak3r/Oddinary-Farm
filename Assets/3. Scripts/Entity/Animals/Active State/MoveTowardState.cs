using UnityEngine;

public class MoveTowardState : BehaviourState
{
    public MoveTowardState(Animal animal) : base(animal)
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

        Vector2 directionToTarget = Animal.MoveTowardStimulus.TargetPosition - (Vector2)Animal.transform.position;
        Animal.MoveDirection(directionToTarget.normalized);
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        Animal.StopMovement();
    }
}
