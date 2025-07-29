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
        if (Time.time > nextAction && AnimalBase.TargetDetector.CurrentTarget != null && AnimalBase.NetworkObject.IsSpawned)
        {
            AnimalBase.CurrentItem.OnPrimaryAction(AnimalBase.TargetDetector.CurrentTarget.position);
            nextAction = Time.time + AnimalBase.CurrentItem.BaseProperty.PrimaryCdr;
            AnimalBase.NetworkAnimator.SetTrigger(Animal.ANIMATOR_PRIMARY_ACTION);
        }
    }
}