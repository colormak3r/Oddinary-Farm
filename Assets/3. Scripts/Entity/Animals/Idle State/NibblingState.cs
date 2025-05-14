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
        animal.Animator.SetBool(Animal.ANIMATOR_IS_NIBBLING, true);
        animal.SetFacing(Random.value > 0.5f);
    }

    private float nextNibbleTime;
    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Time.time > nextNibbleTime)
        {
            nextNibbleTime = Time.time + Random.Range(1, 3f);
            animal.Animator.SetBool(Animal.ANIMATOR_IS_NIBBLING, Random.value > 0.5f);
            animal.SetFacing(Random.value > 0.5f);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_NIBBLING, false);
    }
}