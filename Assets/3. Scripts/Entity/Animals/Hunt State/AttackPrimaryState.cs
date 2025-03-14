using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPrimaryState : BehaviourState
{
    private float nextAction;
    public AttackPrimaryState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();

    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Time.time > nextAction && animal.TargetDetector.CurrentTarget != null)
        {
            animal.CurrentItem.OnPrimaryAction(animal.TargetDetector.CurrentTarget.position);
            nextAction = Time.time + animal.CurrentItem.BaseProperty.PrimaryCdr;
            animal.NetworkAnimator.SetTrigger("PrimaryAction");
        }
    }
}