using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurrowingState : BehaviourState
{
    public BurrowingState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        AnimalBase.Animator.SetBool("IsBurrowing", true);
        AnimalBase.Movement.SetCanBeKnockback(false);
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool("IsBurrowing", false);
        AnimalBase.Movement.SetCanBeKnockback(true);
    }
}