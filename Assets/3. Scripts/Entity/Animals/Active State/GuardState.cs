using UnityEngine;

public class GuardState : BehaviourState
{
    private Vector2 guardPosition;

    public GuardState(Animal animal) : base(animal)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
        guardPosition = animal.MoveTowardStimulus.GetRandomGuardLocation();
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        Vector2 originalPosition = animal.transform.position;
        Vector2 directionToNewPosition = guardPosition - (Vector2)animal.transform.position;
        animal.MoveDirection(directionToNewPosition.normalized);

        if (directionToNewPosition.sqrMagnitude < 0.1f)
        {
            guardPosition = animal.MoveTowardStimulus.GetRandomGuardLocation();
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        animal.StopMovement();
    }
}