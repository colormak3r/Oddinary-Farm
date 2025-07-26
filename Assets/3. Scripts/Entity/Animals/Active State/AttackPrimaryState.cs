using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPrimaryState : BehaviourState
{
    private float nextAction;
    public AttackPrimaryState(Animal animal) : base(animal)
    {

    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Time.time > nextAction && Animal.TargetDetector.CurrentTarget != null && Animal.NetworkObject.IsSpawned)
        {
            Animal.CurrentItem.OnPrimaryAction(Animal.TargetDetector.CurrentTarget.position);
            nextAction = Time.time + Animal.CurrentItem.BaseProperty.PrimaryCdr;
            Animal.NetworkAnimator.SetTrigger(Animal.ANIMATOR_PRIMARY_ACTION);
        }
    }
}