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
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_NIBBLING, true);
        AnimalBase.SetFacing(Random.value > 0.5f);
    }

    private float nextNibbleTime;
    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Time.time > nextNibbleTime)
        {
            nextNibbleTime = Time.time + Random.Range(1, 3f);
            AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_NIBBLING, Random.value > 0.5f);
            AnimalBase.SetFacing(Random.value > 0.5f);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_NIBBLING, false);
    }
}