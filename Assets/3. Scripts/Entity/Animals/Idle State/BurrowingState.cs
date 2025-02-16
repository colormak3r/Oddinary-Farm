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
        animal.Animator.SetBool("IsBurrowing", true);
        animal.Movement.SetCanBeKnockback(false);
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool("IsBurrowing", false);
        animal.Movement.SetCanBeKnockback(true);
    }
}