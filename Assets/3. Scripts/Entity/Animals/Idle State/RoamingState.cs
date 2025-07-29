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
        AnimalBase.Animator.SetBool("IsMoving", true);
        AnimalBase.MoveTo(AnimalBase.GetRandomPointInRange());
        AnimalBase.OnDestinationReached.AddListener(HandleOnDestinationReached);
    }

    const float StallDelay = 0.1f;
    private float stallStart = -1f;
    public override void ExecuteState()
    {
        var velocity = AnimalBase.Rbody.linearVelocity;

        if ((velocity.x == 0 || velocity.y == 0) && stallStart < 0f)
            stallStart = Time.time;
        else if (velocity.x != 0 && velocity.y != 0 && stallStart >= 0f)
            stallStart = -1f;

        if (stallStart >= 0f && Time.time - stallStart > StallDelay)
        {
            AnimalBase.MoveTo(AnimalBase.GetRandomPointInRange());
            stallStart = -1f;
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool("IsMoving", false);
        AnimalBase.StopMovement();
        AnimalBase.OnDestinationReached.RemoveListener(HandleOnDestinationReached);
    }

    private void HandleOnDestinationReached()
    {
        AnimalBase.MoveTo(AnimalBase.GetRandomPointInRange());
    }
}

