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

    const float StallDelay = 0.1f;
    private float stallStart = -1f;
    public override void ExecuteState()
    {
        var velocity = animal.Rbody.linearVelocity;

        if ((velocity.x == 0 || velocity.y == 0) && stallStart < 0f)
            stallStart = Time.time;
        else if (velocity.x != 0 && velocity.y != 0 && stallStart >= 0f)
            stallStart = -1f;

        if (stallStart >= 0f && Time.time - stallStart > StallDelay)
        {
            animal.MoveTo(animal.GetRandomPointInRange());
            stallStart = -1f;
        }
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

