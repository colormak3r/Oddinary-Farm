using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RoamingState : BehaviourState
{
    public RoamingState(Animal animal) : base(animal)
    {
        
    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool("IsMoving", true);
        animal.MoveTo(animal.GetRandomPointInRange());
        animal.OnDestinationReached.AddListener(HandleOnDestinationReached);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();         
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool("IsMoving", false);
        animal.StopMovement();
        animal.OnDestinationReached.RemoveListener(HandleOnDestinationReached);
    }

    private void HandleOnDestinationReached()
    {
        animal.MoveTo(animal.GetRandomPointInRange());
    }
}

