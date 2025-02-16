using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NibblingState : BehaviourState
{
    public NibblingState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool("IsNibbling", true);
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool("IsNibbling", false);
    }
}