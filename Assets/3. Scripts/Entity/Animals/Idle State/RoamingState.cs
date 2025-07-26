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
        Animal.Animator.SetBool("IsMoving", true);
        Animal.MoveTo(Animal.GetRandomPointInRange());
        Animal.OnDestinationReached.AddListener(HandleOnDestinationReached);
    }

    const float StallDelay = 0.1f;
    private float stallStart = -1f;
    public override void ExecuteState()
    {
        var velocity = Animal.Rbody.linearVelocity;

        if ((velocity.x == 0 || velocity.y == 0) && stallStart < 0f)
            stallStart = Time.time;
        else if (velocity.x != 0 && velocity.y != 0 && stallStart >= 0f)
            stallStart = -1f;

        if (stallStart >= 0f && Time.time - stallStart > StallDelay)
        {
            Animal.MoveTo(Animal.GetRandomPointInRange());
            stallStart = -1f;
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool("IsMoving", false);
        Animal.StopMovement();
        Animal.OnDestinationReached.RemoveListener(HandleOnDestinationReached);
    }

    private void HandleOnDestinationReached()
    {
        Animal.MoveTo(Animal.GetRandomPointInRange());
    }
}

