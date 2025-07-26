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
        Animal.Animator.SetBool("IsBurrowing", true);
        Animal.Movement.SetCanBeKnockback(false);
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool("IsBurrowing", false);
        Animal.Movement.SetCanBeKnockback(true);
    }
}