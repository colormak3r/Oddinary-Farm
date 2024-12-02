using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurrowingState : AnimalState
{
    public BurrowingState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Burrowing true");
        animal.Animator.SetBool("IsBurrowing", true);
    }

    public override void ExitState()
    {
        base.ExitState();
        Debug.Log("Burrowing fase");
        animal.Animator.SetBool("IsBurrowing", false);
    }
}