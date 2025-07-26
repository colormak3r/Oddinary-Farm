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
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
        guardPosition = Animal.MoveTowardStimulus.GetRandomGuardLocation();
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        Vector2 originalPosition = Animal.transform.position;
        Vector2 directionToNewPosition = guardPosition - (Vector2)Animal.transform.position;
        Animal.MoveDirection(directionToNewPosition.normalized);

        if (directionToNewPosition.sqrMagnitude < 0.1f)
        {
            guardPosition = Animal.MoveTowardStimulus.GetRandomGuardLocation();
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        Animal.StopMovement();
    }
}