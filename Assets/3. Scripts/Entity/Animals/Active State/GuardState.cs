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
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
        guardPosition = AnimalBase.MoveTowardStimulus.GetRandomGuardLocation();
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        Vector2 originalPosition = AnimalBase.transform.position;
        Vector2 directionToNewPosition = guardPosition - (Vector2)AnimalBase.transform.position;
        AnimalBase.MoveDirection(directionToNewPosition.normalized);

        if (directionToNewPosition.sqrMagnitude < 0.1f)
        {
            guardPosition = AnimalBase.MoveTowardStimulus.GetRandomGuardLocation();
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        AnimalBase.StopMovement();
    }
}